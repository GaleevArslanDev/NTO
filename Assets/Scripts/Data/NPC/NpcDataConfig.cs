using Core;
using Data.Game;
using Gameplay.Dialogue;
using UnityEngine;

namespace Data.NPC
{
    [CreateAssetMenu(menuName = "NPC/NPC Data Config")]
    public class NpcDataConfig : ScriptableObject
    {
        [Header("Basic Info")]
        public string npcName;
        public int npcID;
        public Sprite portrait;
        public AudioClip defaultVoice;
    
        [Header("Personality & Relationships")]
        public Personality personality;
        public NpcConfig startingRelationships;
    
        [Header("Schedule Settings")]
        public Vector3 homeLocation;
        public Vector3 workLocation;
        public Vector3 eatingLocation;
        public Vector3 leisureLocation;
    
        [Header("Dialogue")]
        public DialogueTree[] dialogueTrees;
    
        [Header("Behavior Settings")]
        public float movementSpeed = 3.5f;
        public float interactionRange = 3f;
    
        [Header("Visual Settings (Optional)")]
        public GameObject customModel;
        public Material[] customMaterials;
    
        public NpcData CreateRuntimeData()
        {
            var runtimeData = new NpcData(npcName, npcID, homeLocation)
            {
                personality = personality ?? new Personality()
            };
        
            if (startingRelationships != null)
            {
                foreach (var rel in startingRelationships.startingRelationships)
                {
                    runtimeData.SetRelationship(rel.targetNpcId, rel.relationshipValue);
                }
            
                foreach (var memory in startingRelationships.startingMemories)
                {
                    runtimeData.AddMemory(
                        memory.memoryText, 
                        memory.impact, 
                        memory.source, 
                        new GameTimestamp(1, 0, 0)
                    );
                }
            }
        
            runtimeData.schedule.dailySchedule = new[]
            {
                new ScheduleEntry { 
                    time = TimeOfDay.Morning, 
                    activity = ActivityType.Work, 
                    location = workLocation 
                },
                new ScheduleEntry { 
                    time = TimeOfDay.Noon, 
                    activity = ActivityType.Eating, 
                    location = eatingLocation 
                },
                new ScheduleEntry { 
                    time = TimeOfDay.Afternoon, 
                    activity = ActivityType.Work, 
                    location = workLocation 
                },
                new ScheduleEntry { 
                    time = TimeOfDay.Evening, 
                    activity = ActivityType.Leisure, 
                    location = leisureLocation 
                },
                new ScheduleEntry { 
                    time = TimeOfDay.Night, 
                    activity = ActivityType.Home, 
                    location = homeLocation 
                }
            };
        
            return runtimeData;
        }
    
        public bool IsValid()
        {
            return !string.IsNullOrEmpty(npcName) && npcID != 0;
        }
    }
}