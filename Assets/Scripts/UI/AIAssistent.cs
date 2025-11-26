using System;
using System.Collections;
using System.Collections.Generic;
using Core;
using TMPro;
using UnityEngine;
using Random = UnityEngine.Random;

namespace UI
{
    public class AIAssistant : MonoBehaviour
    {
        public static AIAssistant Instance;
        private static readonly int IsMoving = Animator.StringToHash("IsMoving"); // bool
        private static readonly int Roll = Animator.StringToHash("Roll"); // trigger
        private static readonly int Jump = Animator.StringToHash("Jump"); // trigger
        private static readonly int Mood = Animator.StringToHash("Mood"); // int
        private static readonly int IsSpeaking = Animator.StringToHash("IsSpeaking"); // bool

        [Header("UI References")]
        [SerializeField] private GameObject assistantPanel;
        [SerializeField] private TextMeshProUGUI speechText;
        [SerializeField] private CanvasGroup speechBubble;
        [SerializeField] private RectTransform assistantRectTransform;
    
        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = 100f;
        [SerializeField] private float bounceHeight = 10f;
        [SerializeField] private float bounceSpeed = 2f;
        [SerializeField] private float movementAreaWidth = 300f;
        [SerializeField] private float movementAreaHeight = 200f;
        [SerializeField] private float jumpHeight = 3f;
        [SerializeField] private float jumpDuration = 0.5f;
    
        [Header("Animation Settings")]
        [SerializeField] private float fadeDuration = 0.3f;
        [SerializeField] private float showDuration = 3f;
        [SerializeField] private float typeSpeed = 0.05f;
    
        [Header("Comment Settings")]
        [SerializeField] private float minCommentDelay = 10f;
        [SerializeField] private float maxCommentDelay = 30f;
    
        [Header("Chances")]
        [SerializeField] private float combatCommentChance = 0.3f;
        [SerializeField] private float resourceCommentChance = 0.5f;
        [SerializeField] private float jumpChance = 0.1f;
        [SerializeField] private float rollChance = 0.1f;
        [SerializeField] private float quickStopCommentChance = 0.1f;
        [SerializeField] private float quickTurnCommentChance = 0.1f;
        [SerializeField] private float quickMoveCommentChance = 0.1f;
        [SerializeField] private float npcShoutCommentChance = 0.5f;
        [SerializeField] private float npcIgnoredCommentChance = 0.25f;
    
        [Header("Inertia Settings")]
        [SerializeField] private float inertiaForce = 80f;
        [SerializeField] private float inertiaDecay = 0.93f;
        [SerializeField] private float maxInertiaSpeed = 200f;
        [SerializeField] private float cameraTurnThreshold = 45f;
        [SerializeField] private float playerAccelThreshold = 5f;
        [SerializeField] private float playerStopThreshold = 3f;

        private AssistantMood _currentMood = AssistantMood.Neutral;
    
        [Serializable]
        public class CommentCategory
        {
            public string categoryName;
            public List<string> comments;
        }
    
        [Header("Comment Database")]
        public List<CommentCategory> commentCategories;
    
        // Components
        private Animator _animator;
        private Canvas _parentCanvas;
        private Camera _mainCamera;
    
        // Movement
        private Vector2 _movementDirection;
        private float _currentBounceOffset;
        private bool _isSpeaking;
        private bool _shouldMove = true;

        // Speech
        private Dictionary<string, List<string>> _commentsDict = new();
        private List<string> _recentlySpoken = new();
        private const int MemorySize = 5;
        private Coroutine _currentSpeechCoroutine;
        private Coroutine _bounceCoroutine;
        private Coroutine _movementCoroutine;
    
        // Инерция
        private Vector2 _currentInertia;
        private Vector3 _lastCameraForward;
        private Vector3 _lastPlayerVelocity;
    
        // Player tracking
        private CharacterController _playerController;
        private Vector3 _lastPlayerWorldPosition;
    
        // События для комментариев
        public Action<string> OnAssistantSpeak;

        // Границы для правого нижнего угла
        private Rect _movementBounds;
        private Vector2 _assistantSize;
        private Vector2 _defaultPosition;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
                return;
            }
        
            _animator = GetComponent<Animator>();
            _parentCanvas = GetComponentInParent<Canvas>();
            _mainCamera = Camera.main;
        
            if (assistantRectTransform == null)
                assistantRectTransform = GetComponent<RectTransform>();
        
            InitializeComments();
            FindPlayerController();
            CalculateMovementBounds();
        }

        private void Start()
        {
            assistantPanel.SetActive(false);
            speechBubble.alpha = 0;
        
            if (_mainCamera != null)
                _lastCameraForward = _mainCamera.transform.forward;
        
            // Устанавливаем начальную позицию в правом нижнем углу
            SetToDefaultPosition();
        
            StartCoroutine(RandomCommentsRoutine());
            _movementCoroutine = StartCoroutine(MovementRoutine());
            StartCoroutine(IdleActionsRoutine());
            StartCoroutine(PlayerTrackingRoutine());
        }
    
        void Update()
        {
            HandleInertiaForces();
            HandleMovement();
            HandleBounceAnimation();
        }

        private void CalculateMovementBounds()
        {
            if (_parentCanvas == null) return;

            // Получаем размер ассистента
            _assistantSize = assistantRectTransform.rect.size;
        
            // Получаем размер канваса
            var canvasWidth = _parentCanvas.pixelRect.width;
            var canvasHeight = _parentCanvas.pixelRect.height;
        
            // Рассчитываем область движения в правом нижнем углу
            var rightEdge = canvasWidth/2 - _assistantSize.x * 0.5f;
            var bottomEdge = (-canvasHeight/2)+_assistantSize.y*0.5f;
            var leftEdge = rightEdge - movementAreaWidth;
            var topEdge = bottomEdge - movementAreaHeight;
        
            // Убеждаемся, что область движения не выходит за пределы экрана
            leftEdge = Mathf.Max(leftEdge, _assistantSize.x * 0.5f);
            topEdge = Mathf.Max(topEdge, _assistantSize.y * 0.5f);
        
            _movementBounds = new Rect(
                leftEdge,
                topEdge,
                Mathf.Max(movementAreaWidth, _assistantSize.x),
                Mathf.Max(movementAreaHeight, _assistantSize.y)
            );
        
            // Позиция по умолчанию - центр области движения
            _defaultPosition = new Vector2(
                leftEdge + _movementBounds.width * 0.5f,
                bottomEdge
            );
        }
    
        private void SetToDefaultPosition()
        {
            assistantRectTransform.anchoredPosition = _defaultPosition;
        }
    
        private void FindPlayerController()
        {
            _playerController = FindObjectOfType<CharacterController>();
            if (_playerController == null)
            {
                Debug.LogWarning("Player CharacterController not found! Inertia effects will be disabled.");
            }
            else
            {
                _lastPlayerVelocity = _playerController.velocity;
            }
        }
    
        private void InitializeComments()
        {
            foreach (var category in commentCategories)
            {
                _commentsDict[category.categoryName] = category.comments;
            }
        }
    
        private IEnumerator PlayerTrackingRoutine()
        {
            while (true)
            {
                if (_playerController != null)
                {
                    TrackPlayerMovement();
                }
                yield return new WaitForSeconds(0.1f);
            }
        }
    
        private void TrackPlayerMovement()
        {
            var currentVelocity = _playerController.velocity;
            var currentCameraForward = _mainCamera != null ? _mainCamera.transform.forward : Vector3.forward;
        
            // Отслеживаем поворот камеры
            var cameraTurnSpeed = CalculateCameraTurnSpeed(currentCameraForward);
            if (cameraTurnSpeed > cameraTurnThreshold)
            {
                OnCameraTurnedQuickly(currentCameraForward, cameraTurnSpeed);
            }
        
            // Отслеживаем ускорение игрока
            var playerAcceleration = CalculatePlayerAcceleration(currentVelocity);
            if (playerAcceleration.magnitude > playerAccelThreshold)
            {
                OnPlayerQuickMovement(playerAcceleration);
            }
        
            // Отслеживаем резкую остановку
            if (_lastPlayerVelocity.magnitude > playerStopThreshold && currentVelocity.magnitude < 1f)
            {
                OnPlayerStoppedQuickly(_lastPlayerVelocity);
            }
        
            _lastCameraForward = currentCameraForward;
            _lastPlayerVelocity = currentVelocity;
        }
    
        private float CalculateCameraTurnSpeed(Vector3 currentCameraForward)
        {
            var angle = Vector3.Angle(_lastCameraForward, currentCameraForward);
            return angle / Time.deltaTime;
        }
    
        private Vector3 CalculatePlayerAcceleration(Vector3 currentVelocity)
        {
            return (currentVelocity - _lastPlayerVelocity) / Time.deltaTime;
        }
        
        public void OnNpcCalling(string npcName)
        {
            if (_isSpeaking) return;
    
            if (Random.value < npcShoutCommentChance)
            {
                SpeakRandomFromCategory("NpcCalling");
            }
        }

        public void OnNpcIgnored(string npcName)
        {
            if (_isSpeaking) return;
    
            if (Random.value < npcIgnoredCommentChance)
            {
                SpeakRandomFromCategory("NpcIgnored");
            }
        }
    
        private IEnumerator MovementRoutine()
        {
            while (true)
            {
                if (!_isSpeaking && _shouldMove)
                {
                    _movementDirection = new Vector2(Random.Range(-1f, 1f), 0).normalized;
                }
            
                yield return new WaitForSeconds(1f);
            }
        }
    
        private IEnumerator IdleActionsRoutine()
        {
            while (true)
            {
                yield return new WaitForSeconds(Random.Range(5f, 15f));

                if (_isSpeaking || !_shouldMove) continue;
                if (Random.value < rollChance)
                {
                    _animator.SetTrigger(Roll);
                }
                else if (Random.value < jumpChance)
                {
                    _animator.SetTrigger(Jump);
                    StartCoroutine(JumpRoutine());
                }
            }
        }
    
        private void HandleMovement()
        {
            if (!_shouldMove || _isSpeaking) return;
        
            var totalMovement = _movementDirection * moveSpeed + _currentInertia;
            var newPosition = assistantRectTransform.anchoredPosition + totalMovement * Time.deltaTime;
        
            // Ограничиваем движение областью в правом нижнем углу
            newPosition = ClampToMovementBounds(newPosition);
        
            assistantRectTransform.anchoredPosition = newPosition;

            transform.localScale = totalMovement.x switch
            {
                // Поворот спрайта
                > 0.1f => Vector3.one,
                < -0.1f => new Vector3(-1, 1, 1),
                _ => transform.localScale
            };

            _animator.SetBool(IsMoving, totalMovement.magnitude > 10f);
        }

        private Vector2 ClampToMovementBounds(Vector2 position)
        {
            // Обновляем границы на случай изменения размера экрана
            CalculateMovementBounds();

            // Ограничиваем позицию областью движения
            position.x = Mathf.Clamp(position.x, _movementBounds.x, _movementBounds.x + _movementBounds.width);

            // Если достигли границы, меняем направление и уменьшаем инерцию
            if (!(position.x <= _movementBounds.x) && !(position.x >= _movementBounds.x + _movementBounds.width))
                return position;
            _movementDirection.x *= -1;
            _currentInertia.x *= 0.3f;

            return position;
        }
    
        private void HandleInertiaForces()
        {
            _currentInertia *= inertiaDecay;
            _currentInertia = Vector2.ClampMagnitude(_currentInertia, maxInertiaSpeed);
        }
    
        private void HandleBounceAnimation()
        {
            switch (_isSpeaking)
            {
                case true when _bounceCoroutine == null:
                    _bounceCoroutine = StartCoroutine(BounceRoutine());
                    break;
                case false when _bounceCoroutine != null:
                {
                    StopCoroutine(_bounceCoroutine);
                    _bounceCoroutine = null;
                    var currentPos = assistantRectTransform.anchoredPosition;
                    assistantRectTransform.anchoredPosition = new Vector2(currentPos.x, currentPos.y - _currentBounceOffset);
                    _currentBounceOffset = 0f;
                    break;
                }
            }
        }
    
        private IEnumerator BounceRoutine()
        {
            var startPos = assistantRectTransform.anchoredPosition;
            var time = 0f;
        
            while (_isSpeaking)
            {
                _currentBounceOffset = Mathf.Sin(time * bounceSpeed) * bounceHeight;
                assistantRectTransform.anchoredPosition = new Vector2(startPos.x, startPos.y + _currentBounceOffset);
                time += Time.deltaTime;
                yield return null;
            }
        }
    
        private IEnumerator JumpRoutine()
        {
            var startPos = assistantRectTransform.anchoredPosition;

            float elapsed = 0f;
            while (elapsed < jumpDuration / 2)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / (jumpDuration / 2);
                float yOffset = Mathf.Sin(progress * Mathf.PI) * jumpHeight;
                assistantRectTransform.anchoredPosition = new Vector2(startPos.x, startPos.y + yOffset);
                yield return null;
            }
        
            elapsed = 0f;
            while (elapsed < jumpDuration / 2)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / (jumpDuration / 2);
                float yOffset = Mathf.Sin((1 - progress) * Mathf.PI) * jumpHeight;
                assistantRectTransform.anchoredPosition = new Vector2(startPos.x, startPos.y + yOffset);
                yield return null;
            }
        
            assistantRectTransform.anchoredPosition = startPos;
        }
    
        // МЕТОДЫ ИНЕРЦИИ

        private void OnCameraTurnedQuickly(Vector3 currentCameraForward, float turnSpeed)
        {
            if (!(turnSpeed > cameraTurnThreshold) || _isSpeaking) return;
            var turnDirection = (currentCameraForward - _lastCameraForward).normalized;
            var inertia = new Vector2(turnDirection.x, 0f).normalized * (turnSpeed * 0.1f);
            _currentInertia += inertia;
            
            if (Random.value < quickTurnCommentChance)
            {
                SpeakRandomFromCategory("CameraQuickTurn");
            }
        }

        private void OnPlayerQuickMovement(Vector3 acceleration)
        {
            if (!(acceleration.magnitude > playerAccelThreshold) || _isSpeaking) return;
            var inertia = new Vector2(-acceleration.x, 0f).normalized * (acceleration.magnitude * 0.2f);
            _currentInertia += inertia;

            if (!(Random.value < quickMoveCommentChance)) return;
            if (!(acceleration.magnitude > playerAccelThreshold * 2f)) return;
            TriggerMoodChange(AssistantMood.Excited, 2f);
            SpeakRandomFromCategory("QuickMovement");
        }

        private void OnPlayerStoppedQuickly(Vector3 previousVelocity)
        {
            if (!(previousVelocity.magnitude > playerStopThreshold) || _isSpeaking) return;
            var stopInertia = new Vector2(previousVelocity.x, 0f).normalized * (previousVelocity.magnitude * 0.3f);
            _currentInertia += stopInertia;
            
            if (Random.value < quickStopCommentChance)
            {
                SpeakRandomFromCategory("QuickStop");
            }
        }
    
        public void AddInertia(Vector2 force)
        {
            _currentInertia += force;
        }
    
        public void AddScreenShakeInertia(float intensity = 1f)
        {
            var randomInertia = Random.insideUnitCircle * inertiaForce * intensity;
            _currentInertia += randomInertia;
        }
    
        // ОСТАЛЬНЫЕ МЕТОДЫ

        private void SetMood(AssistantMood newMood)
        {
            _currentMood = newMood;
            _animator.SetInteger(Mood, (int)newMood);
        }

        private void TriggerMoodChange(AssistantMood newMood, float duration = 0f)
        {
            SetMood(newMood);
        
            if (duration > 0)
            {
                StartCoroutine(RevertMoodAfterDelay(duration));
            }
        }
    
        private IEnumerator RevertMoodAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            SetMood(AssistantMood.Neutral);
        }
    
        private IEnumerator RandomCommentsRoutine()
        {
            while (true)
            {
                yield return new WaitForSeconds(Random.Range(minCommentDelay, maxCommentDelay));
            
                if (!_isSpeaking && !IsAnyImportantUIOpen())
                {
                    SpeakRandomFromCategory("Random");
                }
            }
        }
    
        private string GetUniqueComment(string category)
        {
            if (!_commentsDict.TryGetValue(category, out var value)) return "Category not found!";
        
            var availableComments = new List<string>(value);
    
            foreach (var spoken in _recentlySpoken)
            {
                availableComments.Remove(spoken);
            }
    
            if (availableComments.Count == 0)
            {
                _recentlySpoken.Clear();
                availableComments = new List<string>(_commentsDict[category]);
            }
    
            var selected = availableComments[Random.Range(0, availableComments.Count)];
    
            _recentlySpoken.Add(selected);
            if (_recentlySpoken.Count > MemorySize)
            {
                _recentlySpoken.RemoveAt(0);
            }
    
            return selected;
        }

        private void Speak(string text)
        {
            if (_isSpeaking) return;
        
            if (_currentSpeechCoroutine != null)
                StopCoroutine(_currentSpeechCoroutine);
            
            _currentSpeechCoroutine = StartCoroutine(SpeechRoutine(text));
        }

        private void SpeakWithPriority(string text, CommentPriority priority = CommentPriority.Normal)
        {
            if (_isSpeaking)
            {
                if (priority <= CommentPriority.Normal) return;
                StopSpeaking();
            }
        
            StartCoroutine(PrioritySpeechRoutine(text, priority));
        }
    
        private IEnumerator PrioritySpeechRoutine(string text, CommentPriority priority)
        {
            var duration = priority == CommentPriority.Critical ? 5f : showDuration;
            var speed = priority == CommentPriority.Critical ? 0.02f : typeSpeed;
        
            if (priority == CommentPriority.Critical)
            {
                TriggerMoodChange(AssistantMood.Worried, duration);
            }
        
            yield return StartCoroutine(SpeechRoutine(text, duration, speed));
        }
    
        private IEnumerator SpeechRoutine(string text, float duration = 0f, float speed = 0f)
        {
            if (duration == 0) duration = showDuration;
            if (speed == 0) speed = typeSpeed;
        
            _isSpeaking = true;
            _shouldMove = false;
        
            _animator.SetBool(IsMoving, false);
            _animator.SetBool(IsSpeaking, true);
        
            assistantPanel.SetActive(true);
            yield return StartCoroutine(FadeSpeechBubble(0f, 1f, fadeDuration));
        
            speechText.text = "";
            foreach (var c in text)
            {
                speechText.text += c;
                yield return new WaitForSeconds(speed);
            }
        
            yield return new WaitForSeconds(duration);
        
            yield return StartCoroutine(FadeSpeechBubble(1f, 0f, fadeDuration));
            assistantPanel.SetActive(false);
        
            _animator.SetBool(IsSpeaking, false);
            _isSpeaking = false;
            _shouldMove = true;
        
            OnAssistantSpeak?.Invoke(text);
        }
    
        private IEnumerator FadeSpeechBubble(float from, float to, float duration)
        {
            var elapsed = 0f;
            while (elapsed < duration)
            {
                speechBubble.alpha = Mathf.Lerp(from, to, elapsed / duration);
                elapsed += Time.deltaTime;
                yield return null;
            }
            speechBubble.alpha = to;
        }
    
        // Методы для вызова из других систем
        public void OnResourceCollected(ItemType itemType, int amount)
        {
            if (_isSpeaking) return;

            if (!(Random.value < resourceCommentChance)) return;
            switch (itemType)
            {
                case ItemType.CrystalRed:
                case ItemType.CrystalBlue:
                    SpeakRandomFromCategory("CrystalCollection");
                    break;
                case ItemType.Metal:
                    SpeakRandomFromCategory("MetalCollection");
                    break;
                case ItemType.Wood:
                    SpeakRandomFromCategory("WoodCollection");
                    break;
                case ItemType.Stone:
                    SpeakRandomFromCategory("StoneCollection");
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(itemType), itemType, null);
            }
        }
    
        public void OnEnemyKilled(string enemyType)
        {
            if (_isSpeaking) return;
        
            if (Random.value < combatCommentChance)
            {
                SpeakRandomFromCategory("Combat");
            }
        }
    
        public void OnTechUnlocked(string techName)
        {
            if (_isSpeaking) return;
        
            SpeakRandomFromCategory("Technology");
        }
    
        public void OnBuildingUpgraded(string buildingName)
        {
            if (_isSpeaking) return;
        
            SpeakRandomFromCategory("Construction");
        }
    
        public void OnPlayerLowHealth()
        {
            if (_isSpeaking) return;
        
            SpeakWithPriority(GetUniqueComment("LowHealth"), CommentPriority.High);
        }
    
        public void OnPlayerEnterNewArea(string areaName)
        {
            if (_isSpeaking) return;
            SpeakRandomFromCategory("NewArea");
        }

        private void SpeakRandomFromCategory(string category)
        {
            if (_commentsDict.ContainsKey(category) && _commentsDict[category].Count > 0)
            {
                Speak(GetUniqueComment(category));
            }
        }
    
        private static bool IsAnyImportantUIOpen()
        {
            return false;
        }

        public void StopSpeaking()
        {
            if (_currentSpeechCoroutine == null) return;
            StopCoroutine(_currentSpeechCoroutine);
            _isSpeaking = false;
            _shouldMove = true;
            assistantPanel.SetActive(false);
            _animator.SetBool(IsSpeaking, false);

            if (_bounceCoroutine == null) return;
            StopCoroutine(_bounceCoroutine);
            _bounceCoroutine = null;
        }
    
        public void SetMovementEnabled(bool value)
        {
            _shouldMove = value;
            if (!value)
            {
                _animator.SetBool(IsMoving, false);
            }
        }
    
        // Для сохранения/загрузки
        [Serializable]
        public class AssistantData
        {
            public AssistantMood currentMood;
            public List<string> spokenComments;
            public Vector2 position;
        }
    
        public AssistantData GetSaveData()
        {
            return new AssistantData
            {
                currentMood = _currentMood,
                spokenComments = _recentlySpoken,
                position = assistantRectTransform.anchoredPosition
            };
        }
    
        public void LoadData(AssistantData data)
        {
            if (data == null) return;
        
            SetMood(data.currentMood);
            _recentlySpoken = data.spokenComments ?? new List<string>();
            assistantRectTransform.anchoredPosition = data.position;
        }
    }
}