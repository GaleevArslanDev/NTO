using System;
using System.Collections;
using System.Collections.Generic;
using Core;
using Gameplay.Systems;
using TMPro;
using UnityEngine;
using Random = UnityEngine.Random;

namespace UI
{
    public class AIAssistant : MonoBehaviour
    {
        public static AIAssistant Instance;
        private static readonly int IsMoving = Animator.StringToHash("IsMoving");
        private static readonly int Roll = Animator.StringToHash("Roll");
        private static readonly int Jump = Animator.StringToHash("Jump");
        private static readonly int Mood = Animator.StringToHash("Mood");
        private static readonly int IsSpeaking = Animator.StringToHash("IsSpeaking");

        [Header("UI References")]
        [SerializeField] private GameObject assistantPanel;
        [SerializeField] private TextMeshProUGUI speechText;
        [SerializeField] private CanvasGroup speechBubble;
        [SerializeField] private RectTransform assistantRectTransform;
    
        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = 100f;
        [SerializeField] private float bounceHeight = 10f;
        [SerializeField] private float bounceSpeed = 2f;
        [SerializeField] private Vector2 movementAreaSize = new Vector2(300f, 200f);
        [SerializeField] private Vector2 bottomRightOffset = new Vector2(-100f, 100f);
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
        
        [Header("Story Mode")]
        [SerializeField] private bool storyMode = true;
        [SerializeField] private List<string> storyCommentCategories = new List<string> { "StoryIntroduction", "StoryTutorial", "StoryGuide", "StoryVillage", "StoryFinal" };

        private AssistantMood _currentMood = AssistantMood.Neutral;
        private StoryManager _storyManager;
        
        [Header("Localization Settings")]
        [SerializeField] private string commentBaseKey = "assistant";
    
        [System.Serializable]
        public class CommentCategory
        {
            public string categoryName;
            public List<string> localizationKeys; // Ключи вместо готовых фраз
            [NonSerialized] public List<string> availableKeys; // Доступные ключи после фильтрации
        }
        
        private LocalizationManager.LocalizationManager _localizationManager;
        
        private Dictionary<string, Queue<string>> _recentlySpokenCategories = new Dictionary<string, Queue<string>>();
        private const int CATEGORY_MEMORY_SIZE = 7; 
    
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
        private Dictionary<string, CommentCategory> _commentsDict = new();
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
    
        // События для комментариев
        public Action<string> OnAssistantSpeak;

        // Границы для правого нижнего угла
        private Rect _movementBounds;
        private Vector2 _assistantSize;
        private Vector2 _defaultPosition;
        private Vector2 _currentPixelOffset;

        // Флаги инициализации
        private bool _isInitialized = false;
        private bool _isCalculatingBounds = false;

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
        
            FindPlayerController();
            _localizationManager = LocalizationManager.LocalizationManager.Instance;
        }

        private void Start()
        {
            speechBubble.alpha = 0;
    
            if (_mainCamera != null)
                _lastCameraForward = _mainCamera.transform.forward;
            
            _storyManager = FindObjectOfType<StoryManager>();
    
            // Запускаем полную инициализацию
            StartCoroutine(FullInitialization());
            InitializeLocalizedComments();
    
            // Инициализируем словарь категорий
            foreach (var category in commentCategories)
            {
                _commentsDict[category.categoryName] = category;
            }
    
            if (_localizationManager != null)
            {
                _localizationManager.OnLanguageChanged += OnLanguageChanged;
            }
        }
        
        private void OnLanguageChanged(string languageCode)
        {
            // Переинициализируем комментарии при смене языка
            InitializeLocalizedComments();
        }

        private void InitializeLocalizedComments()
        {
            _commentsDict.Clear(); // Очищаем словарь
    
            // Инициализируем систему вариативности для каждой категории
            foreach (var category in commentCategories)
            {
                category.availableKeys = new List<string>(category.localizationKeys);
                _recentlySpokenCategories[category.categoryName] = new Queue<string>();
                _commentsDict[category.categoryName] = category; // Добавляем в словарь
            }
        }

        private IEnumerator FullInitialization()
        {
            // Ждем пока Canvas полностью инициализируется
            yield return new WaitForEndOfFrame();
            yield return new WaitForEndOfFrame(); // Двойное ожидание для надежности
            
            // Инициализируем границы и позицию
            CalculateMovementBounds();
            SetToDefaultPosition();
            
            _isInitialized = true;
            
            // Запускаем основные корутины
            StartCoroutine(RandomCommentsRoutine());
            _movementCoroutine = StartCoroutine(MovementRoutine());
            StartCoroutine(IdleActionsRoutine());
            StartCoroutine(PlayerTrackingRoutine());
        }
        
        private string GetUniqueLocalizedComment(string categoryName)
        {
            if (!_commentsDict.TryGetValue(categoryName, out var category))
                return GetFallbackText(categoryName);

            // Если нет доступных ключей, сбрасываем память
            if (category.availableKeys.Count == 0)
            {
                ResetCategoryMemory(categoryName);
            }

            // Выбираем случайный доступный ключ
            var randomIndex = Random.Range(0, category.availableKeys.Count);
            var selectedKey = category.availableKeys[randomIndex];
            
            // Убираем выбранный ключ из доступных
            category.availableKeys.RemoveAt(randomIndex);
            
            // Добавляем в историю произнесенных
            var spokenQueue = _recentlySpokenCategories[categoryName];
            spokenQueue.Enqueue(selectedKey);
            
            // Ограничиваем размер памяти
            if (spokenQueue.Count > CATEGORY_MEMORY_SIZE)
            {
                var oldestKey = spokenQueue.Dequeue();
                // Возвращаем старый ключ в доступные, если он еще существует
                if (category.localizationKeys.Contains(oldestKey) && !category.availableKeys.Contains(oldestKey))
                {
                    category.availableKeys.Add(oldestKey);
                }
            }

            // Получаем локализованный текст
            return GetLocalizedText(selectedKey);
        }
        
        private void ResetCategoryMemory(string categoryName)
        {
            if (_commentsDict.TryGetValue(categoryName, out var category))
            {
                category.availableKeys = new List<string>(category.localizationKeys);
                _recentlySpokenCategories[categoryName].Clear();
            }
        }
        
        private string GetLocalizedText(string key)
        {
            if (_localizationManager != null)
            {
                var localizedText = _localizationManager.GetString(key);
                if (!localizedText.StartsWith("[") && !localizedText.EndsWith("]"))
                {
                    return localizedText;
                }
            }

            return "Localized text not found";
        }
        
        private string GetFallbackText(string category)
        {
            return category switch
            {
                "Combat" => "Отличный бой!",
                "ResourceCollection" => "Ресурсы собраны!",
                "Technology" => "Технология исследована!",
                "Construction" => "Строительство завершено!",
                "LowHealth" => "Внимание! Низкое здоровье!",
                _ => "Интересно..."
            };
        }
        
        public void SpeakStoryLine(string category, params object[] args)
        {
            if (_localizationManager != null)
            {
                string key = $"assistant_story_{category}";
                string text = _localizationManager.GetString(key, args);
                Speak(text, true); // forceStoryMode = true
            }
        }

        private void OnRectTransformDimensionsChange()
        {
            if (!_isInitialized || _isCalculatingBounds) return;

            StartCoroutine(DelayedBoundsRecalculation());
        }
    
        private IEnumerator DelayedBoundsRecalculation()
        {
            yield return new WaitForEndOfFrame();
            CalculateMovementBounds();
            
            if (!_isSpeaking)
            {
                // Плавно перемещаем к текущей позиции с учетом новых границ
                var currentPos = assistantRectTransform.anchoredPosition;
                var clampedPos = ClampToMovementBounds(currentPos);
                
                if (Vector2.Distance(currentPos, clampedPos) > 10f)
                {
                    if (_movementCoroutine != null)
                        StopCoroutine(_movementCoroutine);
                    _movementCoroutine = StartCoroutine(SmoothReposition(currentPos, clampedPos));
                }
            }
        }
    
        private IEnumerator SmoothReposition(Vector2 from, Vector2 to)
        {
            var duration = 0.5f;
            var elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var t = elapsed / duration;
                assistantRectTransform.anchoredPosition = Vector2.Lerp(from, to, t);
                yield return null;
            }
            
            assistantRectTransform.anchoredPosition = to;
            _movementCoroutine = StartCoroutine(MovementRoutine());
        }
    
        private void CalculateMovementBounds()
        {
            if (_parentCanvas == null) 
            {
                Debug.LogWarning("Parent canvas not found!");
                return;
            }

            _isCalculatingBounds = true;

            try
            {
                // Получаем размер ассистента
                if (assistantRectTransform == null)
                    assistantRectTransform = GetComponent<RectTransform>();
                    
                _assistantSize = assistantRectTransform.rect.size;
            
                // Рассчитываем область движения в правом нижнем углу с учетом смещения
                var rightEdge = bottomRightOffset.x;
                var bottomEdge = bottomRightOffset.y;
            
                var leftEdge = rightEdge - movementAreaSize.x;
                var topEdge = bottomEdge - movementAreaSize.y;

                // Гарантируем, что область движения имеет минимальный размер
                var boundsWidth = Mathf.Max(movementAreaSize.x, _assistantSize.x);
                var boundsHeight = Mathf.Max(movementAreaSize.y, _assistantSize.y);

                _movementBounds = new Rect(
                    leftEdge,
                    topEdge,
                    boundsWidth,
                    boundsHeight
                );

                // Позиция по умолчанию - центр нижней части области движения
                _defaultPosition = new Vector2(
                    leftEdge + _movementBounds.width * 0.5f,
                    bottomEdge
                );
            }
            finally
            {
                _isCalculatingBounds = false;
            }
        }
    
        private void SetToDefaultPosition()
        {
            if (assistantRectTransform != null)
            {
                assistantRectTransform.anchoredPosition = _defaultPosition;
                _currentPixelOffset = Vector2.zero;
            }
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
                SpeakLocalized("NpcCalling");
            }
        }

        public void OnNpcIgnored(string npcName)
        {
            if (_isSpeaking) return;
    
            if (Random.value < npcIgnoredCommentChance)
            {
                SpeakLocalized("NpcIgnored");
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
            
                yield return new WaitForSeconds(Random.Range(1f, 3f));
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

        private void Update()
        {
            if (!_isInitialized) return;
    
            // В режиме сюжета ограничиваем движение и комментарии
            if (_storyManager != null && _storyManager.IsInStoryMode())
            {
                _shouldMove = false;
                return;
            }
    
            HandleInertiaForces();
            HandleMovement();
            HandleBounceAnimation();
        }
    
        private void HandleMovement()
        {
            if (!_shouldMove || _isSpeaking) return;
        
            var totalMovement = _movementDirection * moveSpeed + _currentInertia;
            var newPosition = assistantRectTransform.anchoredPosition + totalMovement * Time.deltaTime;
        
            // Ограничиваем движение областью в правом нижнем углу
            newPosition = ClampToMovementBounds(newPosition);
        
            assistantRectTransform.anchoredPosition = newPosition;
            
            // Сохраняем смещение от позиции по умолчанию
            _currentPixelOffset = newPosition - _defaultPosition;

            assistantPanel.transform.localScale = totalMovement.x switch
            {
                // Поворот спрайта
                > 0.1f => Vector3.one,
                < -0.1f => new Vector3(-1, 1, 1),
                _ => assistantPanel.transform.localScale
            };

            _animator.SetBool(IsMoving, totalMovement.magnitude > 10f);
        }

        private Vector2 ClampToMovementBounds(Vector2 position)
        {
            position.x = Mathf.Clamp(position.x, _movementBounds.x, _movementBounds.x + _movementBounds.width);
            position.y = Mathf.Clamp(position.y, _movementBounds.y, _movementBounds.y + _movementBounds.height);

            // Если достигли границы, меняем направление и уменьшаем инерцию
            if (position.x <= _movementBounds.x || position.x >= _movementBounds.x + _movementBounds.width)
            {
                _movementDirection.x *= -1;
                _currentInertia.x *= 0.3f;
            }

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
    
        private void OnCameraTurnedQuickly(Vector3 currentCameraForward, float turnSpeed)
        {
            if (!(turnSpeed > cameraTurnThreshold) || _isSpeaking) return;
            var turnDirection = (currentCameraForward - _lastCameraForward).normalized;
            var inertia = new Vector2(turnDirection.x, 0f).normalized * (turnSpeed * 0.1f);
            _currentInertia += inertia;
            
            if (Random.value < quickTurnCommentChance)
            {
                SpeakLocalized("CameraQuickTurn");
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
            SpeakLocalized("QuickMovement");
        }

        private void OnPlayerStoppedQuickly(Vector3 previousVelocity)
        {
            if (!(previousVelocity.magnitude > playerStopThreshold) || _isSpeaking) return;
            var stopInertia = new Vector2(previousVelocity.x, 0f).normalized * (previousVelocity.magnitude * 0.3f);
            _currentInertia += stopInertia;
            
            if (Random.value < quickStopCommentChance)
            {
                SpeakLocalized("QuickStop");
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
                    SpeakLocalized("Random");
                }
            }
        }
        
        private void SpeakLocalized(string category, CommentPriority priority = CommentPriority.Normal)
        {
            var text = GetUniqueLocalizedComment(category);
            
            if (priority == CommentPriority.Normal)
            {
                Speak(text);
            }
            else
            {
                SpeakWithPriority(text, priority);
            }
        }

        public void Speak(string text, bool forceStoryMode = false)
        {
            if (_storyManager != null && _storyManager.IsInStoryMode() && !forceStoryMode)
            {
                // В режиме сюжета говорим только сюжетные реплики
                return;
            }
    
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
        
            yield return StartCoroutine(FadeSpeechBubble(0f, 1f, fadeDuration));
        
            speechText.text = "";
            foreach (var c in text)
            {
                speechText.text += c;
                yield return new WaitForSeconds(speed);
            }
        
            yield return new WaitForSeconds(duration);
        
            yield return StartCoroutine(FadeSpeechBubble(1f, 0f, fadeDuration));
        
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
    
        public void OnResourceCollected(ItemType itemType, int amount)
        {
            if (_isSpeaking) return;

            if (!(Random.value < resourceCommentChance)) return;
            switch (itemType)
            {
                case ItemType.CrystalRed:
                case ItemType.CrystalBlue:
                    SpeakLocalized("CrystalCollection");
                    break;
                case ItemType.Metal:
                    SpeakLocalized("MetalCollection");
                    break;
                case ItemType.Wood:
                    SpeakLocalized("WoodCollection");
                    break;
                case ItemType.Stone:
                    SpeakLocalized("StoneCollection");
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
                SpeakLocalized("Combat");
            }
        }
    
        public void OnTechUnlocked(string techName)
        {
            if (_isSpeaking) return;
        
            SpeakLocalized("Technology");
        }
    
        public void OnBuildingUpgraded(string buildingName)
        {
            if (_isSpeaking) return;
        
            SpeakLocalized("Construction");
        }
    
        public void OnPlayerLowHealth()
        {
            if (_isSpeaking) return;
        
            SpeakLocalized("LowHealth", CommentPriority.High);
        }
    
        public void OnPlayerEnterNewArea(string areaName)
        {
            if (_isSpeaking) return;
            SpeakLocalized("NewArea");
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
    
        [Serializable]
        public class AssistantData
        {
            public AssistantMood currentMood;
            public List<string> spokenComments;
            public Vector2 position;
        }
        
        private void OnDestroy()
        {
            if (_localizationManager != null)
            {
                _localizationManager.OnLanguageChanged -= OnLanguageChanged;
            }
        }
    }
}