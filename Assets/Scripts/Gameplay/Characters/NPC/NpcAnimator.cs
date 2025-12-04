using System.Collections;
using System.Collections.Generic;
using Core;
using UnityEngine;
using UnityEngine.AI;

namespace Gameplay.Characters.NPC
{
    public class NpcAnimator : MonoBehaviour
    {
        [Header("Animator References")]
        [SerializeField] private Animator animator;
        [SerializeField] private NavMeshAgent navMeshAgent;
        
        [Header("Animation Parameters")]
        [SerializeField] private string isMovingParam = "IsMoving";
        [SerializeField] private string isWorkingParam = "IsWorking";
        [SerializeField] private string isTalkingParam = "IsTalking";
        [SerializeField] private string isWaitingParam = "IsWaiting"; // Новый параметр
        [SerializeField] private string thinkTrigger = "Think";
        [SerializeField] private string happyTrigger = "Happy";
        [SerializeField] private string sadTrigger = "Sad";
        [SerializeField] private string impatientTrigger = "Impatient"; // Новый триггер
        [SerializeField] private string lookAroundTrigger = "LookAround"; // Новый триггер
        
        [Header("Animation Settings")]
        [SerializeField] private float locomotionSmoothTime = 0.1f;
        [SerializeField] private float minMoveSpeed = 0.1f;
        
        [Header("Waiting Animation Settings")]
        [SerializeField] private float minWaitingAnimationInterval = 8f;
        [SerializeField] private float maxWaitingAnimationInterval = 15f;
        [SerializeField] private List<string> waitingAnimationTriggers = new List<string>() { "Impatient", "LookAround", "Think" };
        
        // Состояния
        private NpcState _currentState = NpcState.Idle;
        private ActivityType _currentActivity = ActivityType.Home;
        private bool _isInDialogue = false;
        private bool _isWaitingForResponse = false;
        private float _currentSpeed = 0f;
        
        // Кэшированные хэши параметров
        private int _isMovingHash;
        private int _isWorkingHash;
        private int _isTalkingHash;
        private int _isWaitingHash; // Новый хэш
        private int _thinkHash;
        private int _happyHash;
        private int _sadHash;
        private int _impatientHash;
        private int _lookAroundHash;
        
        // Корутины
        private Coroutine _waitingAnimationCoroutine;
        
        private void Awake()
        {
            if (animator == null) animator = GetComponent<Animator>();
            if (navMeshAgent == null) navMeshAgent = GetComponent<NavMeshAgent>();
            
            _isMovingHash = Animator.StringToHash(isMovingParam);
            _isWorkingHash = Animator.StringToHash(isWorkingParam);
            _isTalkingHash = Animator.StringToHash(isTalkingParam);
            _isWaitingHash = Animator.StringToHash(isWaitingParam);
            _thinkHash = Animator.StringToHash(thinkTrigger);
            _happyHash = Animator.StringToHash(happyTrigger);
            _sadHash = Animator.StringToHash(sadTrigger);
            _impatientHash = Animator.StringToHash(impatientTrigger);
            _lookAroundHash = Animator.StringToHash(lookAroundTrigger);
        }
        
        public void SetMoving(bool isMoving)
        {
            animator.SetBool(_isMovingHash, isMoving);
        }
        
        public void SetState(NpcState state, ActivityType activity = ActivityType.Home)
        {
            _currentState = state;
            _currentActivity = activity;
            UpdateAnimatorState();
        }
        
        private void UpdateAnimatorState()
        {
            if (animator == null) return;
            
            animator.SetBool(_isWorkingHash, false);
            animator.SetBool(_isTalkingHash, false);
            
            switch (_currentActivity)
            {
                case ActivityType.Work:
                    animator.SetBool(_isWorkingHash, true);
                    break;
            }
        }
        
        // Новая система ожидания ответа
        public void SetWaitingForResponse(bool waiting)
        {
            if (_isWaitingForResponse == waiting) return;
            
            _isWaitingForResponse = waiting;
            
            if (animator != null)
            {
                animator.SetBool(_isWaitingHash, waiting);
                
                if (waiting)
                {
                    // Запускаем корутину для периодических анимаций ожидания
                    if (_waitingAnimationCoroutine != null)
                    {
                        StopCoroutine(_waitingAnimationCoroutine);
                    }
                    _waitingAnimationCoroutine = StartCoroutine(WaitingAnimationRoutine());
                }
                else
                {
                    // Останавливаем корутину
                    if (_waitingAnimationCoroutine != null)
                    {
                        StopCoroutine(_waitingAnimationCoroutine);
                        _waitingAnimationCoroutine = null;
                    }
                }
            }
        }
        
        private IEnumerator WaitingAnimationRoutine()
        {
            while (_isWaitingForResponse)
            {
                // Ждем случайный интервал перед следующей анимацией
                float waitTime = Random.Range(minWaitingAnimationInterval, maxWaitingAnimationInterval);
                yield return new WaitForSeconds(waitTime);
                
                // Если все еще ждем ответа, проигрываем случайную анимацию
                if (_isWaitingForResponse && CanPlayWaitingAnimation())
                {
                    PlayRandomWaitingAnimation();
                }
            }
        }
        
        private void PlayRandomWaitingAnimation()
        {
            if (animator == null || waitingAnimationTriggers.Count == 0) return;
            
            // Выбираем случайную анимацию из списка
            int randomIndex = Random.Range(0, waitingAnimationTriggers.Count);
            string triggerName = waitingAnimationTriggers[randomIndex];
            
            // Проигрываем соответствующую анимацию
            switch (triggerName)
            {
                case "Think":
                    animator.SetTrigger(_thinkHash);
                    break;
                case "Impatient":
                    animator.SetTrigger(_impatientHash);
                    break;
                case "LookAround":
                    animator.SetTrigger(_lookAroundHash);
                    break;
            }
        }
        
        private bool CanPlayWaitingAnimation()
        {
            if (_isInDialogue || !_isWaitingForResponse) return false;
            
            // Проверяем, не проигрывается ли уже другая анимация
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            return !stateInfo.IsTag("Talking") && 
                   !stateInfo.IsTag("Emotional") &&
                   !stateInfo.IsTag("Working");
        }
        
        // Остальные методы остаются без изменений
        public void PlayThinkingAnimation()
        {
            if (animator != null && CanPlayWaitingAnimation())
            {
                animator.SetTrigger(_thinkHash);
            }
        }
        
        public void PlayHappyAnimation()
        {
            if (animator != null)
            {
                animator.SetTrigger(_happyHash);
            }
        }
        
        public void PlaySadAnimation()
        {
            if (animator != null)
            {
                animator.SetTrigger(_sadHash);
            }
        }
        
        public void SetTalking(bool isTalking)
        {
            if (animator != null)
            {
                animator.SetBool(_isTalkingHash, isTalking);
                
                // Если начинаем говорить, выключаем ожидание
                if (isTalking)
                {
                    SetWaitingForResponse(false);
                }
            }
        }
        
        public void SetDialogueState(bool inDialogue)
        {
            _isInDialogue = inDialogue;
            SetTalking(inDialogue);
            
            // При выходе из диалога сбрасываем ожидание
            if (!inDialogue)
            {
                SetWaitingForResponse(false);
            }
        }
        
        public bool CanPlayAnimation()
        {
            if (_isInDialogue) return false;
            
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            return !stateInfo.IsName("Think") && 
                   !stateInfo.IsName("Happy") && 
                   !stateInfo.IsName("Sad");
        }
    }
}