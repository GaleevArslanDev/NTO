using System;
using System.Collections;
using Core;
using Data.Game;
using Data.NPC;
using UnityEngine;
using UnityEngine.AI;

namespace Gameplay.Characters.NPC
{
    public class NpcBehaviour : MonoBehaviour
    {
        [Header("NPC Data")] public NpcDataConfig npcDataConfig;

        private NavMeshAgent _agent;
        private ScheduleManager _scheduleManager;
        private RelationshipManager _relationshipManager;
        private NpcInteraction _interaction;
        
        private bool _isInStoryMode = false;
        private Coroutine _optimizedBehaviorRoutine;
        
        private NpcAnimator _npcAnimator;

        // Состояния
        private NpcState _currentState = NpcState.Idle;
        private Activity _currentActivity;
        private Coroutine _currentActivityRoutine;

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            _scheduleManager = FindObjectOfType<ScheduleManager>();
            _relationshipManager = FindObjectOfType<RelationshipManager>();
            _interaction = GetComponent<NpcInteraction>();
            _npcAnimator = GetComponent<NpcAnimator>();

            // Настройка NPCInteraction из NPCDataConfig
            if (npcDataConfig == null || _interaction == null) return;
            // Создаем runtime данные для взаимодействия
            _interaction.npcData = CreateRuntimeNpcData();
            _interaction.dialogueTrees = npcDataConfig.dialogueTrees;
            _interaction.portrait = npcDataConfig.portrait;
            _interaction.defaultVoice = npcDataConfig.defaultVoice;

            if (_agent != null)
            {
                _agent.speed = npcDataConfig.movementSpeed;
            }
        }

        private void Start()
        {
            _agent = GetComponent<NavMeshAgent>();
            _scheduleManager = FindObjectOfType<ScheduleManager>();
            _relationshipManager = FindObjectOfType<RelationshipManager>();
            _interaction = GetComponent<NpcInteraction>();
            _npcAnimator = GetComponent<NpcAnimator>();

            // Настройка NPCInteraction из NPCDataConfig
            if (npcDataConfig == null || _interaction == null) return;
            _interaction.npcData = CreateRuntimeNpcData();
            _interaction.dialogueTrees = npcDataConfig.dialogueTrees;
            _interaction.portrait = npcDataConfig.portrait;
            _interaction.defaultVoice = npcDataConfig.defaultVoice;

            if (_agent != null)
            {
                _agent.speed = npcDataConfig.movementSpeed;
            }
    
            // Регистрация в менеджерах
            if (_relationshipManager != null && _interaction != null && _interaction.npcData != null)
            {
                _relationshipManager.RegisterNpc(_interaction.npcData);
            }
    
            // Запуск оптимизированной корутины поведения, если не в режиме сюжета
            if (!_isInStoryMode)
            {
                _optimizedBehaviorRoutine = StartCoroutine(OptimizedBehaviorRoutine());
            }
        }


        private IEnumerator OptimizedBehaviorRoutine()
        {
            var waitCheck = new WaitForSeconds(1f);

            while (true)
            {
                yield return waitCheck;

                // Если в режиме сюжета, не обновляем активность
                if (_isInStoryMode) continue;

                if (_scheduleManager == null || npcDataConfig == null) continue;

                var newActivity = _scheduleManager.GetCurrentActivity(_interaction.npcData);

                // Если активность изменилась, прерываем текущую и начинаем новую
                if (IsSameActivity(_currentActivity, newActivity)) continue;
                if (_currentActivityRoutine != null)
                {
                    StopCoroutine(_currentActivityRoutine);
                }

                _currentActivity = newActivity;
                _currentActivityRoutine = StartCoroutine(ExecuteActivity(newActivity));
            }
        }

        private static bool IsSameActivity(Activity a, Activity b)
        {
            return a.type == b.type &&
                   Vector3.Distance(a.location, b.location) < 1f &&
                   a.targetNpc == b.targetNpc;
        }
        
        public void SetStoryMode(bool storyMode)
        {
            _isInStoryMode = storyMode;
    
            if (storyMode)
            {
                // Останавливаем обычное поведение
                if (_optimizedBehaviorRoutine != null)
                {
                    StopCoroutine(_optimizedBehaviorRoutine);
                    _optimizedBehaviorRoutine = null;
                }
        
                if (_currentActivityRoutine != null)
                {
                    StopCoroutine(_currentActivityRoutine);
                    _currentActivityRoutine = null;
                }
        
                // Останавливаем агента
                if (_agent != null && _agent.enabled)
                {
                    _agent.isStopped = true;
                }
            }
            else
            {
                // Восстанавливаем обычное поведение
                if (_optimizedBehaviorRoutine == null)
                {
                    _optimizedBehaviorRoutine = StartCoroutine(OptimizedBehaviorRoutine());
                }
            }
        }

        private IEnumerator ExecuteActivity(Activity activity)
        {
            _currentState = GetNpcStateForActivity(activity.type);
            _npcAnimator?.SetState(_currentState, activity.type);

            // Двигаемся к цели
            yield return StartCoroutine(MoveToLocation(activity.location));

            // Выполняем активность
            var activityTimer = activity.duration;
            while (activityTimer > 0 && IsSameActivity(_currentActivity, activity))
            {
                activityTimer -= Time.deltaTime;

                switch (activity.type)
                {
                    case ActivityType.Work:
                    case ActivityType.Social:
                    case ActivityType.Leisure:
                    case ActivityType.Home:
                    case ActivityType.Special:
                    case ActivityType.Eating:
                    case ActivityType.Sleeping:
                    case ActivityType.Traveling:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                yield return null;
            }
        }

        private IEnumerator MoveToLocation(Vector3 destination)
        {
            if (_agent == null || !_agent.enabled) yield break;

            _currentState = NpcState.Walking;
            _npcAnimator?.SetState(_currentState);
            _npcAnimator?.SetMoving(true);
            _agent.SetDestination(destination);

            while (_agent.pathPending || _agent.remainingDistance > _agent.stoppingDistance)
            {
                yield return null;
            }
            _npcAnimator?.SetMoving(false);
        }

        private static NpcState GetNpcStateForActivity(ActivityType activityType)
        {
            return activityType switch
            {
                ActivityType.Work => NpcState.Working,
                ActivityType.Social => NpcState.Socializing,
                ActivityType.Leisure => NpcState.Leisure,
                ActivityType.Home => NpcState.Idle,
                ActivityType.Eating => NpcState.Eating,
                ActivityType.Sleeping => NpcState.Sleeping,
                _ => NpcState.Idle
            };
        }

        private NpcData CreateRuntimeNpcData()
        {
            var runtimeData = new NpcData(
                npcDataConfig.npcName,
                npcDataConfig.npcID,
                npcDataConfig.homeLocation
            )
            {
                personality = npcDataConfig.personality,
                schedule =
                {
                    // Инициализация расписания из NPCDataConfig
                    dailySchedule = new[]
                    {
                        new ScheduleEntry
                        {
                            time = TimeOfDay.Morning,
                            activity = ActivityType.Work,
                            location = npcDataConfig.workLocation
                        },
                        new ScheduleEntry
                        {
                            time = TimeOfDay.Noon,
                            activity = ActivityType.Eating,
                            location = npcDataConfig.eatingLocation
                        },
                        new ScheduleEntry
                        {
                            time = TimeOfDay.Afternoon,
                            activity = ActivityType.Work,
                            location = npcDataConfig.workLocation
                        },
                        new ScheduleEntry
                        {
                            time = TimeOfDay.Evening,
                            activity = ActivityType.Leisure,
                            location = npcDataConfig.leisureLocation
                        },
                        new ScheduleEntry
                        {
                            time = TimeOfDay.Night,
                            activity = ActivityType.Home,
                            location = npcDataConfig.homeLocation
                        }
                    }
                }
            };

            return runtimeData;
        }

        // Для отладки
        private void OnDrawGizmosSelected()
        {
            if (npcDataConfig == null) return;

            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(npcDataConfig.workLocation, 1f);

            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(npcDataConfig.eatingLocation, 1f);

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(npcDataConfig.leisureLocation, 1f);

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(npcDataConfig.homeLocation, 1f);
        }

        public NpcBehaviourSaveData GetSaveData()
        {
            return new NpcBehaviourSaveData
            {
                position = transform.position,
                rotation = transform.eulerAngles,
                currentState = _currentState,
                currentActivity = CreateActivitySaveData()
            };
        }

        public void ApplySaveData(NpcBehaviourSaveData saveData)
        {
            if (saveData == null) return;
            Debug.Log($"Applying save data for NPC {npcDataConfig.npcName}");
            Debug.Log($"Position: {saveData.position}");

            // Восстанавливаем позицию и поворот
            transform.position = saveData.position;
            transform.eulerAngles = saveData.rotation;

            // Восстанавливаем состояние
            _currentState = saveData.currentState;

            // Останавливаем текущие корутины
            if (_currentActivityRoutine != null)
            {
                StopCoroutine(_currentActivityRoutine);
                _currentActivityRoutine = null;
            }

            // Восстанавливаем NavMeshAgent позицию
            if (_agent != null && _agent.enabled)
            {
                _agent.Warp(saveData.position);
            }

            // Восстанавливаем активность если нужно
            if (saveData.currentActivity != null && _agent != null && _agent.enabled)
            {
                _currentActivity = ConvertToActivity(saveData.currentActivity);
                _currentActivityRoutine = StartCoroutine(ExecuteActivity(_currentActivity));
            }
        }

        private ActivitySaveData CreateActivitySaveData()
        {
            return new ActivitySaveData
            {
                type = _currentActivity.type,
                location = _currentActivity.location,
                targetNpc = _currentActivity.targetNpc,
                duration = _currentActivity.duration,
                remainingTime = GetRemainingActivityTime()
            };
        }

        private Activity ConvertToActivity(ActivitySaveData saveData)
        {
            return new Activity
            {
                type = saveData.type,
                location = saveData.location,
                targetNpc = saveData.targetNpc,
                duration = saveData.duration
            };
        }

        private float GetRemainingActivityTime()
        {
            // TODO: Реализовать логику получения оставшегося времени активности
            return 0f;
        }
        
        public void PlayThinkingAnimation()
        {
            _npcAnimator?.PlayThinkingAnimation();
        }

        public void PlayHappyAnimation()
        {
            _npcAnimator?.PlayHappyAnimation();
        }
        
        public void SetWaitingForResponse(bool waiting)
        {
            _npcAnimator?.SetWaitingForResponse(waiting);
        }

        public void PlaySadAnimation()
        {
            _npcAnimator?.PlaySadAnimation();
        }

        public void SetTalking(bool isTalking)
        {
            _npcAnimator?.SetTalking(isTalking);
        }

        public void SetDialogueState(bool inDialogue)
        {
            _npcAnimator?.SetDialogueState(inDialogue);
        }
    }

    [System.Serializable]
    public class NpcBehaviourSaveData
    {
        public Vector3 position;
        public Vector3 rotation;
        public NpcState currentState;
        public ActivitySaveData currentActivity;
    }
}