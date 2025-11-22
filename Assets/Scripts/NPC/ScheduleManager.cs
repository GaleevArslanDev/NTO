using System.Linq;
using UnityEngine;

public class ScheduleManager : MonoBehaviour
{
    public WorldTime worldTime;
    
    [System.Serializable]
    public class ScheduleData // Переименовали чтобы избежать конфликта
    {
        public ScheduleEntry[] dailySchedule;
    }
    
    public Activity GetCurrentActivity(NPCData npc) // Убрали DataStructure.
    {
        if (npc.schedule == null || npc.schedule.dailySchedule == null)
        {
            return new Activity(ActivityType.Leisure, npc.homeLocation, "", 60f);
        }
        
        var currentTimeOfDay = worldTime.GetTimeOfDay();
        
        var entry = npc.schedule.dailySchedule.FirstOrDefault(e => e.time == currentTimeOfDay);
        
        if (entry != null)
        {
            return new Activity
            {
                type = entry.activity,
                location = entry.location,
                targetNPC = entry.specificNPC,
                duration = 60f
            };
        }
        
        return new Activity(ActivityType.Leisure, npc.homeLocation, "", 60f);
    }
}