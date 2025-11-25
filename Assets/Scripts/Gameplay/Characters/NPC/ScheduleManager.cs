using System.Linq;
using Core;
using Data.NPC;
using Gameplay.Systems;
using UnityEngine;

namespace Gameplay.Characters.NPC
{
    public class ScheduleManager : MonoBehaviour
    {
        public WorldTime worldTime;
    
        [System.Serializable]
        public class ScheduleData // Переименовали чтобы избежать конфликта
        {
            public ScheduleEntry[] dailySchedule;
        }
    
        public Activity GetCurrentActivity(NpcData npc) // Убрали DataStructure.
        {
            if (npc.schedule?.dailySchedule == null)
            {
                return new Activity(ActivityType.Leisure, npc.homeLocation);
            }
        
            var currentTimeOfDay = worldTime.GetTimeOfDay();
        
            var entry = npc.schedule.dailySchedule.FirstOrDefault(e => e.time == currentTimeOfDay);
        
            if (entry != null)
            {
                return new Activity
                {
                    type = entry.activity,
                    location = entry.location,
                    targetNpc = entry.specificNpc,
                    duration = 60f
                };
            }
        
            return new Activity(ActivityType.Leisure, npc.homeLocation);
        }
    }
}