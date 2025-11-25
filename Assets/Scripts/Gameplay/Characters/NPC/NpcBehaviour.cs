using System;
using System.Collections;
using Core;
using Data.NPC;
using UnityEngine;
using UnityEngine.AI;

namespace Gameplay.Characters.NPC
{
    public class NpcBehaviour : MonoBehaviour
    {
        [Header("NPC Data")]
        public NpcDataConfig npcDataConfig;
    
        private NavMeshAgent _agent;
        private ScheduleManager _scheduleManager;
        private RelationshipManager _relationshipManager;
        private NpcInteraction _interaction;
    
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
            // Регистрация в менеджерах
            if (_relationshipManager != null && _interaction != null && _interaction.npcData != null)
            {
                _relationshipManager.RegisterNpc(_interaction.npcData);
            }
        
            // Запуск оптимизированной корутины поведения
            StartCoroutine(OptimizedBehaviorRoutine());
        }
    
        private IEnumerator OptimizedBehaviorRoutine()
        {
            var waitCheck = new WaitForSeconds(1f);
        
            while (true)
            {
                yield return waitCheck;
            
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
    
        private IEnumerator ExecuteActivity(Activity activity)
        {
            _currentState = GetNpcStateForActivity(activity.type);
        
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
            _agent.SetDestination(destination);
        
            const float timeout = 10f;
            var timer = 0f;
        
            while (timer < timeout && 
                   (_agent.pathPending || _agent.remainingDistance > _agent.stoppingDistance))
            {
                timer += Time.deltaTime;
                yield return null;
            }
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
                        new ScheduleEntry { 
                            time = TimeOfDay.Morning, 
                            activity = ActivityType.Work, 
                            location = npcDataConfig.workLocation 
                        },
                        new ScheduleEntry { 
                            time = TimeOfDay.Noon, 
                            activity = ActivityType.Eating, 
                            location = npcDataConfig.eatingLocation 
                        },
                        new ScheduleEntry { 
                            time = TimeOfDay.Afternoon, 
                            activity = ActivityType.Work, 
                            location = npcDataConfig.workLocation 
                        },
                        new ScheduleEntry { 
                            time = TimeOfDay.Evening, 
                            activity = ActivityType.Leisure, 
                            location = npcDataConfig.leisureLocation 
                        },
                        new ScheduleEntry { 
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
    }
}