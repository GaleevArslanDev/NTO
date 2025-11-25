using System.Collections.Generic;
using Core;
using Data.Game;
using Gameplay.Characters.NPC;
using UnityEngine;

namespace Data.NPC
{
    [System.Serializable]
    public class NpcData
    {
        [Header("Basic Info")]
        public string npcName;
        public int npcID;
    
        [Header("Relationships")]
        public Dictionary<int, int> Relationships;
    
        [Header("Memories")]
        public List<Memory> memories;
    
        [Header("Schedule & Behavior")]
        public NpcSchedule schedule;
        public Personality personality;
        public NpcState currentState;
        public Vector3 homeLocation;

        public NpcData(string name, int id, Vector3 homePos)
        {
            npcName = name;
            npcID = id;
            Relationships = new Dictionary<int, int>();
            memories = new List<Memory>();
            schedule = new NpcSchedule();
            personality = new Personality();
            currentState = NpcState.Idle;
            homeLocation = homePos;
        }

        // Конструктор по умолчанию
        public NpcData()
        {
            Relationships = new Dictionary<int, int>();
            memories = new List<Memory>();
            schedule = new NpcSchedule();
            personality = new Personality();
            currentState = NpcState.Idle;
            homeLocation = Vector3.zero;
        }

        // Метод для установки или обновления отношения к другому NPC/игроку
        public void SetRelationship(int targetID, int value)
        {
            Relationships ??= new Dictionary<int, int>();

            Relationships[targetID] = Mathf.Clamp(value, -100, 100);
        }

        // Метод для получения отношения к другому NPC/игроку
        public int GetRelationship(int targetID)
        {
            if (Relationships != null && Relationships.TryGetValue(targetID, out var relationship))
                return relationship;
            return 0; // Нейтральное отношение по умолчанию
        }

        // Метод для изменения отношения (добавить/убавить)
        public void ModifyRelationship(int targetID, int change)
        {
            var current = GetRelationship(targetID);
            SetRelationship(targetID, current + change);
        }

        // Метод для добавления воспоминания
        public void AddMemory(string memoryText, int impact, string source, GameTimestamp timestamp)
        {
            memories ??= new List<Memory>();
            
            memories.Add(new Memory(memoryText, impact, source, timestamp));
        
            // Ограничиваем количество воспоминаний (удаляем самые старые)
            if (memories.Count > 20)
                memories.RemoveAt(0);
        }

        // Метод для поиска воспоминаний по ключевым словам
        public List<Memory> FindMemories(string keyword)
        {
            if (memories == null) return new List<Memory>();
        
            return memories.FindAll(memory => 
                memory.memoryText.ToLower().Contains(keyword.ToLower()));
        }

        // Метод для получения последних N воспоминаний
        public List<Memory> GetRecentMemories(int count = 5)
        {
            if (memories == null || memories.Count == 0) 
                return new List<Memory>();
            
            var startIndex = Mathf.Max(0, memories.Count - count);
            var actualCount = Mathf.Min(count, memories.Count - startIndex);
        
            return memories.GetRange(startIndex, actualCount);
        }

        // Метод для проверки, есть ли у NPC определенная черта личности
        public bool HasTrait(Trait trait)
        {
            return personality != null && personality.HasTrait(trait);
        }

        // Метод для получения текущей активности по расписанию
        public Activity GetCurrentActivity(ScheduleManager scheduleManager)
        {
            return scheduleManager == null ? new Activity(ActivityType.Home, homeLocation) : scheduleManager.GetCurrentActivity(this);
        }

        // Метод для сброса состояния (например, при загрузке игры)
        public void ResetState()
        {
            currentState = NpcState.Idle;
        }

        // Метод для получения статуса отношений в текстовом виде
        public string GetRelationshipStatus(int targetID)
        {
            var value = GetRelationship(targetID);

            return value switch
            {
                >= 80 => "Друзья",
                >= 60 => "Хорошие",
                >= 40 => "Нейтральные",
                >= 20 => "Напряженные",
                >= 0 => "Плохие",
                _ => "Враждебные"
            };
        }

        // Метод для клонирования NPCData (полезно для сохранений)
        public NpcData Clone()
        {
            var clone = new NpcData(npcName, npcID, homeLocation)
            {
                personality = new Personality()
                {
                    traits = (Trait[])personality.traits?.Clone(),
                    openness = personality.openness,
                    friendliness = personality.friendliness,
                    ambition = personality.ambition
                },
                currentState = currentState
            };

            // Клонируем отношения
            foreach (var relationship in Relationships)
            {
                clone.Relationships[relationship.Key] = relationship.Value;
            }

            // Клонируем воспоминания
            foreach (var memory in memories)
            {
                clone.memories.Add(new Memory(
                    memory.memoryText,
                    memory.relationshipImpact,
                    memory.sourceNpc,
                    memory.timestamp
                ));
            }

            // Клонируем расписание
            if (schedule?.dailySchedule == null) return clone;
            clone.schedule.dailySchedule = new ScheduleEntry[schedule.dailySchedule.Length];
            for (var i = 0; i < schedule.dailySchedule.Length; i++)
            {
                clone.schedule.dailySchedule[i] = new ScheduleEntry
                {
                    time = schedule.dailySchedule[i].time,
                    activity = schedule.dailySchedule[i].activity,
                    location = schedule.dailySchedule[i].location,
                    specificNpc = schedule.dailySchedule[i].specificNpc
                };
            }

            return clone;
        }

        // Метод для отладки
        public void DebugInfo()
        {
            Debug.Log($"=== NPC: {npcName} (ID: {npcID}) ===");
            Debug.Log($"Состояние: {currentState}");
            Debug.Log($"Дом: {homeLocation}");
            Debug.Log($"Черты личности: {personality.traits?.Length ?? 0}");
        
            Debug.Log("Отношения:");
            foreach (var rel in Relationships)
            {
                Debug.Log($"  -> ID {rel.Key}: {rel.Value}/100 ({GetRelationshipStatus(rel.Key)})");
            }
        
            Debug.Log($"Воспоминания: {memories?.Count ?? 0}");
            if (memories is not { Count: > 0 }) return;
            foreach (var memory in GetRecentMemories(3))
            {
                Debug.Log($"  - {memory.memoryText} ({memory.relationshipImpact})");
            }
        }
    }
}