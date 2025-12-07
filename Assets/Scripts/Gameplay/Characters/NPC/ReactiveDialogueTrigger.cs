using System.Collections;
using Gameplay.Characters.Player;
using Gameplay.Systems;
using TMPro;
using UI;
using UnityEngine;

namespace Gameplay.Characters.NPC
{
    public class ReactiveDialogueTrigger : MonoBehaviour
    {
        [Header("Call Settings")] [SerializeField]
        private float callRadius = 10f;

        [SerializeField] private float minCallCooldown = 120f;
        [SerializeField] private float maxCallCooldown = 300f;

        [Header("Behavior Settings")] [SerializeField]
        private bool getsOffendedWhenIgnored = true;

        [SerializeField] private int relationshipPenalty = -5;
        [SerializeField] private float playerSpeedThreshold = 2f;

        [Header("Visual & Audio")] [SerializeField]
        private GameObject callIcon;

        [SerializeField] private AudioClip callSound;
        [SerializeField] private string[] callPhrases;
        [SerializeField] private TMP_Text callText;

        [Header("Dialogue Sequence")] [SerializeField]
        private string[] reactiveDialogueSequence;

        [SerializeField] private bool loopSequence = true;

        // Состояния
        private NpcInteraction _npcInteraction;
        private Transform _player;
        private AudioSource _audioSource;
        private bool _isCalling;
        private bool _canCall = true;
        private int _currentDialogueIndex = 0;
        private float _lastCallTime;
        private Coroutine _callRoutine;
        private GameObject _currentCallIcon;

        // Новые поля для отслеживания игрока в зоне
        private bool _playerInCallZone = false;
        private Collider _callZoneCollider;
        
        [Header("Animation References")]
        [SerializeField] private NpcAnimator npcAnimator;
        
        // События
        public System.Action<string> OnStartCalling;
        public System.Action OnStopCalling;
        public System.Action OnPlayerResponded;
        public System.Action OnPlayerIgnored;

        private void Start()
        {
            if (npcAnimator == null)
            {
                npcAnimator = GetComponent<NpcAnimator>();
                if (npcAnimator == null)
                {
                    Debug.LogWarning($"NpcAnimator not found on {gameObject.name}");
                }
            }
            
            _npcInteraction = GetComponent<NpcInteraction>();
            _audioSource = GetComponent<AudioSource>();
            _player = GameObject.FindGameObjectWithTag("Player")?.transform;

            SetupCallZoneCollider();

            if (_player == null)
            {
                Debug.LogWarning($"ReactiveDialogueCaller: Player not found on {gameObject.name}");
                enabled = false;
            }

            if (callIcon != null)
                callIcon.SetActive(false);

            // Запускаем корутину проверки вызовов если можем звать
            if (_canCall)
            {
                StartCoroutine(CallCooldownRoutine());
            }
        }

        private void SetupCallZoneCollider()
        {
            // Создаем отдельный дочерний объект для зоны вызова
            GameObject callZone = new GameObject("CallZone");
            callZone.transform.SetParent(transform);
            callZone.transform.localPosition = Vector3.zero;
            callZone.layer = LayerMask.NameToLayer("NPC_CallZone");
    
            _callZoneCollider = callZone.AddComponent<SphereCollider>();
            ((SphereCollider)_callZoneCollider).radius = callRadius;
            _callZoneCollider.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player")) return;

            _playerInCallZone = true;

            // Если NPC уже зовет, обновляем состояние
            if (_isCalling)
            {
                UpdateCallState();
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag("Player")) return;

            _playerInCallZone = false;

            // Если NPC зовет и игрок вышел из зоны - обижаемся
            if (_isCalling)
            {
                OnPlayerIgnored?.Invoke();
                HandleIgnoredCall();
                EndCalling();
            }
        }

        private void Update()
        {
            if (!_canCall || _isCalling || _player == null) return;

            CheckForPlayerPassingBy();
        }

        private void CheckForPlayerPassingBy()
        {
            if (!_playerInCallZone) return;

            var distanceToPlayer = Vector3.Distance(transform.position, _player.position);

            // Игрок в радиусе и движется с достаточной скоростью
            if (distanceToPlayer <= callRadius && IsPlayerMovingFastEnough())
            {
                // Проверяем, что игрок действительно пробегает мимо, а не стоит
                if (IsPlayerMovingAway() || IsPlayerMovingPast())
                {
                    StartCalling();
                }
            }
        }

        private bool IsPlayerMovingFastEnough()
        {
            var playerController = _player.GetComponent<PlayerController>();
            if (playerController == null) return false;

            // Здесь можно добавить проверку реальной скорости через Rigidbody.velocity
            return true;
        }

        private bool IsPlayerMovingAway()
        {
            var playerDirection = (_player.position - transform.position).normalized;
            var playerForward = _player.forward;

            return Vector3.Dot(playerDirection, playerForward) < 0;
        }

        private bool IsPlayerMovingPast()
        {
            var toPlayer = _player.position - transform.position;
            var playerRight = _player.right;

            return Mathf.Abs(Vector3.Dot(toPlayer.normalized, playerRight)) > 0.7f;
        }

        public int GetCurrentDialogueIndex()
        {
            return _currentDialogueIndex;
        }

        public void SetCurrentDialogueIndex(int index)
        {
            _currentDialogueIndex = index;
        }

        public float GetLastCallTime()
        {
            return _lastCallTime;
        }

        public void SetCallCooldown(float lastCallTime)
        {
            _lastCallTime = lastCallTime;
        }

        public void SetCanCall(bool canCall)
        {
            _canCall = canCall;
        }

        private void StartCalling()
        {
            // ПРОВЕРКА: не в сюжетном режиме
            if (StoryManager.Instance != null && StoryManager.Instance.IsInStoryMode())
            {
                Debug.Log($"{_npcInteraction.GetNpcName()}: Не могу звать в сюжетном режиме");
                return;
            }
    
            if (!_canCall || _isCalling) return;

            if (AIAssistant.Instance != null)
                AIAssistant.Instance.OnNpcCalling(_npcInteraction.GetNpcName());

            _isCalling = true;
            _canCall = false;
            _lastCallTime = Time.time;

            _callRoutine = StartCoroutine(CallRoutine());
        }
        
        private void PlayCallingAnimation(bool isCalling)
        {
            if (npcAnimator != null)
            {
                npcAnimator.PlayCallingAnimation(isCalling);
            }
        }

        private IEnumerator CallRoutine()
        {
            // Активируем визуальные элементы
            ShowCallIndicator();
            
            // Проигрываем анимацию зова
            PlayCallingAnimation(true);

            // Воспроизводим звук
            PlayCallSound();

            // Показываем текстовую фразу
            ShowCallPhrase();

            // Уведомляем о начале подзыва
            OnStartCalling?.Invoke(_npcInteraction.npcData?.npcName ?? "NPC");

            // Ждем ответа игрока в течение callDuration секунд, только если игрок в зоне
            var timer = 0f;
            var playerResponded = false;

            while (_playerInCallZone && !playerResponded)
            {
                timer += Time.deltaTime;
                yield return null;
            }
        }

        public void TriggerDialogue()
        {
            if (!_isCalling) return;

            OnPlayerResponded?.Invoke();
            StartDialogue();
            EndCalling();
        }

        private void ShowCallIndicator()
        {
            if (callIcon != null)
            {
                callIcon.SetActive(true);
            }
        }

        private void PlayCallSound()
        {
            if (callSound != null && _audioSource != null)
            {
                _audioSource.PlayOneShot(callSound);
            }
        }

        private void ShowCallPhrase()
        {
            if (callText != null && callPhrases.Length > 0)
            {
                var randomPhrase = callPhrases[Random.Range(0, callPhrases.Length)];
                callText.text = randomPhrase;
                callText.gameObject.SetActive(true);

                StartCoroutine(HideCallTextAfterDelay(3f));
            }
        }

        private IEnumerator HideCallTextAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (callText != null)
                callText.gameObject.SetActive(false);
        }

        private void StartDialogue()
        {
            if (_npcInteraction == null) return;
            PlayCallingAnimation(false);

            var dialogueTree = GetCurrentDialogue();
            if (!string.IsNullOrEmpty(dialogueTree))
            {
                _npcInteraction.StartSpecificDialogue(dialogueTree);
                AdvanceToNextDialogue();
            }
        }

        private void HandleIgnoredCall()
        {
            if (!getsOffendedWhenIgnored || relationshipPenalty == 0) return;

            var npcData = _npcInteraction.npcData;
            if (npcData != null)
            {
                var playerData = Data.Game.PlayerData.Instance;
                if (playerData != null)
                {
                    playerData.ModifyRelationship(npcData.npcID, relationshipPenalty);
                    
                    if (RelationshipNotificationUI.Instance != null)
                    {
                        RelationshipNotificationUI.Instance.ShowRelationshipChange(
                            npcData.npcName,
                            relationshipPenalty
                        );
                    }

                    var relationshipManager = RelationshipManager.Instance;
                    if (relationshipManager != null)
                    {
                        var worldTime = WorldTime.Instance;
                        if (worldTime != null)
                        {
                            npcData.AddMemory(
                                $"Игрок проигнорировал мой зов",
                                relationshipPenalty,
                                "System",
                                worldTime.GetCurrentTimestamp()
                            );
                        }
                    }

                    Debug.Log($"{npcData.npcName} обиделся на игнор! Отношения: {relationshipPenalty}");
                }
            }
            PlayCallingAnimation(false);
            PlaySadAnimation();

            AdvanceToNextDialogue();
        }
        
        private void PlaySadAnimation()
        {
            if (npcAnimator != null)
            {
                npcAnimator.PlaySadAnimation();
            }
        }

        public void EndCalling()
        {
            _isCalling = false;
            _currentDialogueIndex++;

            if (callIcon != null)
                callIcon.SetActive(false);

            if (callText != null)
                callText.gameObject.SetActive(false);

            OnStopCalling?.Invoke();

            StartCoroutine(CallCooldownRoutine());
        }

        private IEnumerator CallCooldownRoutine()
        {
            // Вычисляем оставшееся время кулдауна
            if (_lastCallTime > 0)
            {
                var timeSinceLastCall = Time.time - _lastCallTime;
                var remainingCooldown = Mathf.Max(0, minCallCooldown - timeSinceLastCall);
        
                Debug.Log($"{GetComponent<NpcInteraction>()?.GetNpcName()} call cooldown: " +
                          $"{remainingCooldown:F1}s remaining (since last call: {timeSinceLastCall:F1}s)");

                if (remainingCooldown > 0)
                {
                    yield return new WaitForSeconds(remainingCooldown);
                }
            }
            else
            {
                // Случайный кулдаун при первом запуске
                var cooldown = Random.Range(minCallCooldown, maxCallCooldown);
                Debug.Log($"{GetComponent<NpcInteraction>()?.GetNpcName()} initial cooldown: {cooldown:F1}s");
                yield return new WaitForSeconds(cooldown);
            }

            _canCall = true;
            Debug.Log($"{GetComponent<NpcInteraction>()?.GetNpcName()} can now call players");
        }

        private void UpdateCallState()
        {
            Debug.Log("Call state updated");
        }

        private string GetCurrentDialogue()
        {
            if (reactiveDialogueSequence == null || reactiveDialogueSequence.Length == 0)
                return null;

            if (_currentDialogueIndex < reactiveDialogueSequence.Length)
                return reactiveDialogueSequence[_currentDialogueIndex];

            return loopSequence ? reactiveDialogueSequence[0] : null;
        }

        private void AdvanceToNextDialogue()
        {
            if (reactiveDialogueSequence == null || reactiveDialogueSequence.Length == 0)
                return;

            if (_currentDialogueIndex >= reactiveDialogueSequence.Length)
            {
                if (loopSequence)
                {
                    _currentDialogueIndex = 0;
                }
                else
                {
                    _currentDialogueIndex = reactiveDialogueSequence.Length - 1;
                    if (!loopSequence)
                    {
                        _canCall = false;
                        enabled = false;
                    }
                }
            }
        }

        // Публичные методы
        public void ForceCall(string specificDialogue = null)
        {
            if (_isCalling) return;

            if (!string.IsNullOrEmpty(specificDialogue))
            {
                reactiveDialogueSequence = new[] { specificDialogue };
                _currentDialogueIndex = 0;
            }

            StartCalling();
        }

        public void SetCallParameters(float radius, float minCooldown, float maxCooldown, bool getsOffended,
            int penalty)
        {
            callRadius = radius;
            minCallCooldown = minCooldown;
            maxCallCooldown = maxCooldown;
            getsOffendedWhenIgnored = getsOffended;
            relationshipPenalty = penalty;

            // Обновляем коллайдер
            if (_callZoneCollider is SphereCollider sphereCollider)
            {
                sphereCollider.radius = callRadius;
            }
        }

        public void ResetDialogueSequence()
        {
            _currentDialogueIndex = 0;
            _canCall = true;
            enabled = true;
        }

        public bool IsCalling => _isCalling;
        public bool CanCall => _canCall;
        public string CurrentDialogue => GetCurrentDialogue();

        private void OnDrawGizmosSelected()
        {
            // Визуализация радиуса подзыва
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, callRadius);
        }

        private void OnDestroy()
        {
            if (_callRoutine != null)
            {
                StopCoroutine(_callRoutine);
            }
        }

        public ReactiveDialogueSaveData GetSaveData()
        {
            Debug.Log("Getting save data for ReactiveDialogueTrigger, canCall: " + _canCall + ", current index: " + _currentDialogueIndex + ", last call time: " + _lastCallTime);
            return new ReactiveDialogueSaveData
            {
                currentDialogueIndex = _currentDialogueIndex,
                canCall = _canCall,
                lastCallTime = _lastCallTime
            };
        }

        public void ApplySaveData(ReactiveDialogueSaveData saveData)
        {
            if (saveData == null) return;
            
            Debug.Log("Applying save data for ReactiveDialogueTrigger");

            _currentDialogueIndex = saveData.currentDialogueIndex;
            _canCall = saveData.canCall;
            _lastCallTime = saveData.lastCallTime;

            if (reactiveDialogueSequence != null && reactiveDialogueSequence.Length > 0)
            {
                if (_currentDialogueIndex >= reactiveDialogueSequence.Length)
                {
                    if (loopSequence)
                    {
                        _currentDialogueIndex = 0;
                        _canCall = true; // Гарантируем, что NPC может звать при loopSequence
                    }
                    else
                    {
                        _canCall = false;
                    }
                }
                else
                {
                    _canCall = true;
                }
            }
            else
            {
                Debug.LogWarning("ReactiveDialogueTrigger has no dialogue sequences");
                // Если нет диалогов, отключаем вызовы
                _canCall = false;
                enabled = false;
            }

            // Сбрасываем состояние вызова
            if (_callRoutine != null)
            {
                StopCoroutine(_callRoutine);
                _callRoutine = null;
            }

            _isCalling = false;
            
            Debug.Log($"Loaded dialogue index: {_canCall}");

            // Перезапускаем корутины если NPC может звать
            if (_canCall)
            {
                Debug.Log("Restarting call cooldown");
                // Пересчитываем время кулдауна с учетом времени загрузки
                float timeSinceLastCall = Time.time - _lastCallTime;
                if (timeSinceLastCall >= minCallCooldown)
                {
                    Debug.Log("Resetting call cooldown");
                    // Кулдаун прошел, можно звать сразу
                    StartCoroutine(CallCooldownRoutine());
                }
                else
                {
                    Debug.Log("Delaying call cooldown");
                    // Ждем оставшееся время кулдауна
                    float remainingCooldown = minCallCooldown - timeSinceLastCall;
                    StartCoroutine(DelayedCallCooldownRoutine(remainingCooldown));
                }
            }

            if (callIcon != null)
                callIcon.SetActive(false);

            Debug.Log($"Reactive dialogue loaded for {GetComponent<NpcInteraction>()?.GetNpcName()}: " +
                      $"index={_currentDialogueIndex}, canCall={_canCall}, lastCall={_lastCallTime}");
        }

        private IEnumerator DelayedCallCooldownRoutine(float delay)
        {
            yield return new WaitForSeconds(delay);
            StartCoroutine(CallCooldownRoutine());
        }
    }

    [System.Serializable]
    public class ReactiveDialogueSaveData
    {
        public int currentDialogueIndex;
        public bool canCall;
        public float lastCallTime;
    }
}