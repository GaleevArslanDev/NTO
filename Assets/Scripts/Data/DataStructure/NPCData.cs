using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class NPCData
{
    [Header("Basic Info")]
    public string npcName;
    public int npcID;
    
    [Header("Relationships")]
    public Dictionary<int, int> relationships;
    
    [Header("Memories")]
    public List<Memory> memories;
    
    [Header("Schedule & Behavior")]
    public NPCSchedule schedule;
    public Personality personality;
    public NPCState currentState;
    public Vector3 homeLocation;

    public NPCData(string name, int id, Vector3 homePos)
    {
        npcName = name;
        npcID = id;
        relationships = new Dictionary<int, int>();
        memories = new List<Memory>();
        schedule = new NPCSchedule();
        personality = new Personality();
        currentState = NPCState.Idle;
        homeLocation = homePos;
    }

    // Конструктор по умолчанию
    public NPCData()
    {
        relationships = new Dictionary<int, int>();
        memories = new List<Memory>();
        schedule = new NPCSchedule();
        personality = new Personality();
        currentState = NPCState.Idle;
        homeLocation = Vector3.zero;
    }

    // Метод для установки или обновления отношения к другому NPC/игроку
    public void SetRelationship(int targetID, int value)
    {
        if (relationships == null)
            relationships = new Dictionary<int, int>();
            
        relationships[targetID] = Mathf.Clamp(value, -100, 100);
    }

    // Метод для получения отношения к другому NPC/игроку
    public int GetRelationship(int targetID)
    {
        if (relationships != null && relationships.ContainsKey(targetID))
            return relationships[targetID];
        return 0; // Нейтральное отношение по умолчанию
    }

    // Метод для изменения отношения (добавить/убавить)
    public void ModifyRelationship(int targetID, int change)
    {
        int current = GetRelationship(targetID);
        SetRelationship(targetID, current + change);
    }

    // Метод для добавления воспоминания
    public void AddMemory(string memoryText, int impact, string source, GameTimestamp timestamp)
    {
        if (memories == null)
            memories = new List<Memory>();
            
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
            
        int startIndex = Mathf.Max(0, memories.Count - count);
        int actualCount = Mathf.Min(count, memories.Count - startIndex);
        
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
        if (scheduleManager == null) 
            return new Activity(ActivityType.Home, homeLocation, "", 60f);
            
        return scheduleManager.GetCurrentActivity(this);
    }

    // Метод для сброса состояния (например, при загрузке игры)
    public void ResetState()
    {
        currentState = NPCState.Idle;
    }

    // Метод для получения статуса отношений в текстовом виде
    public string GetRelationshipStatus(int targetID)
    {
        int value = GetRelationship(targetID);
        
        if (value >= 80) return "Друзья";
        if (value >= 60) return "Хорошие";
        if (value >= 40) return "Нейтральные";
        if (value >= 20) return "Напряженные";
        if (value >= 0) return "Плохие";
        return "Враждебные";
    }

    // Метод для клонирования NPCData (полезно для сохранений)
    public NPCData Clone()
    {
        var clone = new NPCData(npcName, npcID, homeLocation)
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
        foreach (var relationship in relationships)
        {
            clone.relationships[relationship.Key] = relationship.Value;
        }

        // Клонируем воспоминания
        foreach (var memory in memories)
        {
            clone.memories.Add(new Memory(
                memory.memoryText,
                memory.relationshipImpact,
                memory.sourceNPC,
                memory.timestamp
            ));
        }

        // Клонируем расписание
        if (schedule != null && schedule.dailySchedule != null)
        {
            clone.schedule.dailySchedule = new ScheduleEntry[schedule.dailySchedule.Length];
            for (int i = 0; i < schedule.dailySchedule.Length; i++)
            {
                clone.schedule.dailySchedule[i] = new ScheduleEntry
                {
                    time = schedule.dailySchedule[i].time,
                    activity = schedule.dailySchedule[i].activity,
                    location = schedule.dailySchedule[i].location,
                    specificNPC = schedule.dailySchedule[i].specificNPC
                };
            }
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
        foreach (var rel in relationships)
        {
            Debug.Log($"  -> ID {rel.Key}: {rel.Value}/100 ({GetRelationshipStatus(rel.Key)})");
        }
        
        Debug.Log($"Воспоминания: {memories?.Count ?? 0}");
        if (memories != null && memories.Count > 0)
        {
            foreach (var memory in GetRecentMemories(3))
            {
                Debug.Log($"  - {memory.memoryText} ({memory.relationshipImpact})");
            }
        }
    }
}