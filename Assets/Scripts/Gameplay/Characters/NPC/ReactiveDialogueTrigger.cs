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
        [Header("Call Settings")]
        [SerializeField] private float callRadius = 10f;
        [SerializeField] private float minCallCooldown = 120f;
        [SerializeField] private float maxCallCooldown = 300f;
        
        [Header("Behavior Settings")]
        [SerializeField] private bool getsOffendedWhenIgnored = true;
        [SerializeField] private int relationshipPenalty = -5;
        [SerializeField] private float playerSpeedThreshold = 2f;
        
        [Header("Visual & Audio")]
        [SerializeField] private GameObject callIcon;
        [SerializeField] private AudioClip callSound;
        [SerializeField] private string[] callPhrases;
        [SerializeField] private TMP_Text callText;
        
        [Header("Dialogue Sequence")]
        [SerializeField] private string[] reactiveDialogueSequence;
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
        
        // События
        public System.Action<string> OnStartCalling;
        public System.Action OnStopCalling;
        public System.Action OnPlayerResponded;
        public System.Action OnPlayerIgnored;

        private void Start()
        {
            _npcInteraction = GetComponent<NpcInteraction>();
            _audioSource = GetComponent<AudioSource>();
            _player = GameObject.FindGameObjectWithTag("Player")?.transform;
            
            // Создаем или находим коллайдер зоны вызова
            SetupCallZoneCollider();
            
            if (_player == null)
            {
                Debug.LogWarning($"ReactiveDialogueCaller: Player not found on {gameObject.name}");
                enabled = false;
            }
            
            if (callIcon != null)
                callIcon.SetActive(false);
        }

        private void SetupCallZoneCollider()
        {
            // Создаем коллайдер для зоны вызова если его нет
            _callZoneCollider = GetComponent<Collider>();
            if (_callZoneCollider == null)
            {
                _callZoneCollider = gameObject.AddComponent<SphereCollider>();
                ((SphereCollider)_callZoneCollider).radius = callRadius;
                _callZoneCollider.isTrigger = true;
            }
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

        private void StartCalling()
        {
            if (!_canCall || _isCalling) return;
            
            if (AIAssistant.Instance != null) 
                AIAssistant.Instance.OnNpcCalling(_npcInteraction.GetNpcName());
            
            _isCalling = true;
            _canCall = false;
            _lastCallTime = Time.time;
            
            _callRoutine = StartCoroutine(CallRoutine());
        }

        private IEnumerator CallRoutine()
        {
            // Активируем визуальные элементы
            ShowCallIndicator();
    
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
            
            AdvanceToNextDialogue();
        }

        private void EndCalling()
        {
            _isCalling = false;
            
            if (callIcon != null)
                callIcon.SetActive(false);
                
            if (callText != null)
                callText.gameObject.SetActive(false);
            
            OnStopCalling?.Invoke();
            
            StartCoroutine(CallCooldownRoutine());
        }

        private IEnumerator CallCooldownRoutine()
        {
            var cooldown = Random.Range(minCallCooldown, maxCallCooldown);
            yield return new WaitForSeconds(cooldown);
            _canCall = true;
        }

        private void UpdateCallState()
        {
            // Обновляем UI в зависимости от состояния
            if (_npcInteraction != null && _npcInteraction.interactionPrompt != null)
            {
                _npcInteraction.interactionPrompt.SetActive(_playerInCallZone);
            }
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
            
            _currentDialogueIndex++;
            
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

        public void SetCallParameters(float radius, float minCooldown, float maxCooldown, bool getsOffended, int penalty)
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
    }
}