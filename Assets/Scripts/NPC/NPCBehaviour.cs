using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class NPCBehaviour : MonoBehaviour
{
    [Header("NPC Data")]
    public NPCDataConfig npcDataConfig;
    
    private NavMeshAgent agent;
    private ScheduleManager scheduleManager;
    private RelationshipManager relationshipManager;
    private NPCInteraction interaction;
    
    // Состояния
    private NPCState currentState = NPCState.Idle;
    private Activity currentActivity;
    private Coroutine currentActivityRoutine;
    
    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        scheduleManager = FindObjectOfType<ScheduleManager>();
        relationshipManager = FindObjectOfType<RelationshipManager>();
        interaction = GetComponent<NPCInteraction>();
        
        // Настройка NPCInteraction из NPCDataConfig
        if (npcDataConfig != null && interaction != null)
        {
            // Создаем runtime данные для взаимодействия
            interaction.NPCData = CreateRuntimeNPCData();
            interaction.dialogueTrees = npcDataConfig.dialogueTrees;
            interaction.portrait = npcDataConfig.portrait;
            interaction.defaultVoice = npcDataConfig.defaultVoice;
            
            if (agent != null)
            {
                agent.speed = npcDataConfig.movementSpeed;
            }
        }
    }
    
    private void Start()
    {
        // Регистрация в менеджерах
        if (relationshipManager != null && interaction != null && interaction.NPCData != null)
        {
            relationshipManager.RegisterNPC(interaction.NPCData);
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
            
            if (scheduleManager == null || npcDataConfig == null) continue;
            
            var newActivity = scheduleManager.GetCurrentActivity(interaction.NPCData);
            
            // Если активность изменилась, прерываем текущую и начинаем новую
            if (!IsSameActivity(currentActivity, newActivity))
            {
                if (currentActivityRoutine != null)
                {
                    StopCoroutine(currentActivityRoutine);
                }
                
                currentActivity = newActivity;
                currentActivityRoutine = StartCoroutine(ExecuteActivity(newActivity));
            }
        }
    }
    
    private bool IsSameActivity(Activity a, Activity b)
    {
        return a.type == b.type && 
               Vector3.Distance(a.location, b.location) < 1f &&
               a.targetNPC == b.targetNPC;
    }
    
    private IEnumerator ExecuteActivity(Activity activity)
    {
        currentState = GetNPCStateForActivity(activity.type);
        
        // Двигаемся к цели
        yield return StartCoroutine(MoveToLocation(activity.location));
        
        // Выполняем активность
        float activityTimer = activity.duration;
        while (activityTimer > 0 && IsSameActivity(currentActivity, activity))
        {
            activityTimer -= Time.deltaTime;
            
            switch (activity.type)
            {
                case ActivityType.Work:
                    // Анимация работы и т.д.
                    break;
                case ActivityType.Social:
                    // Поиск NPC для общения
                    break;
            }
            
            yield return null;
        }
    }
    
    private IEnumerator MoveToLocation(Vector3 destination)
    {
        if (agent == null || !agent.enabled) yield break;
        
        currentState = NPCState.Walking;
        agent.SetDestination(destination);
        
        float timeout = 10f;
        float timer = 0f;
        
        while (timer < timeout && 
               (agent.pathPending || agent.remainingDistance > agent.stoppingDistance))
        {
            timer += Time.deltaTime;
            yield return null;
        }
    }
    
    private NPCState GetNPCStateForActivity(ActivityType activityType)
    {
        switch (activityType)
        {
            case ActivityType.Work: return NPCState.Working;
            case ActivityType.Social: return NPCState.Socializing;
            case ActivityType.Leisure: return NPCState.Leisure;
            case ActivityType.Home: return NPCState.Idle;
            case ActivityType.Eating: return NPCState.Eating;
            case ActivityType.Sleeping: return NPCState.Sleeping;
            default: return NPCState.Idle;
        }
    }
    
    private NPCData CreateRuntimeNPCData()
    {
        var runtimeData = new NPCData(
            npcDataConfig.npcName, 
            npcDataConfig.npcID, 
            npcDataConfig.homeLocation
        )
        {
            personality = npcDataConfig.personality
        };
        
        // Инициализация расписания из NPCDataConfig
        runtimeData.schedule.dailySchedule = new ScheduleEntry[]
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