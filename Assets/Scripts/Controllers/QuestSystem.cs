using System;
using System.Collections.Generic;
using UnityEngine;

public class QuestSystem : MonoBehaviour
{
    public static QuestSystem Instance;
    
    [System.Serializable]
    public class Quest
    {
        public string questId;
        public string title;
        public string description;
        public QuestType type;
        public List<ResourceCost> requirements;
        public List<ResourceCost> rewards;
        public bool isCompleted;
        public bool isActive;
        
        // Для квестов на убийство
        public string enemyType;
        public int requiredKills;
        public int currentKills;
    }
    
    public enum QuestType
    {
        GatherResources,
        KillEnemies,
        UpgradeBuilding,
        UnlockTech
    }
    
    [Header("Quest Settings")]
    public int maxActiveQuests = 3;
    public float questRefreshTime = 300f; // 5 минут
    
    private List<Quest> availableQuests = new List<Quest>();
    private List<Quest> activeQuests = new List<Quest>();
    private List<Quest> completedQuests = new List<Quest>();
    private float questRefreshTimer;
    
    public event Action<Quest> OnQuestAccepted;
    public event Action<Quest> OnQuestCompleted;
    public event Action OnQuestsUpdated;
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        GenerateInitialQuests();
        questRefreshTimer = questRefreshTime;
    }
    
    void Update()
    {
        // Авто-обновление квестов
        questRefreshTimer -= Time.deltaTime;
        if (questRefreshTimer <= 0)
        {
            RefreshQuests();
            questRefreshTimer = questRefreshTime;
        }
        
        // Проверка выполнения квестов
        CheckQuestProgress();
    }
    
    private void GenerateInitialQuests()
    {
        // Примеры квестов
        availableQuests.Add(new Quest
        {
            questId = "gather_wood_1",
            title = "Заготовка древесины",
            description = "Соберите 20 единиц дерева",
            type = QuestType.GatherResources,
            requirements = new List<ResourceCost> { new ResourceCost { Type = ItemType.Wood, Amount = 20 } },
            rewards = new List<ResourceCost> { new ResourceCost { Type = ItemType.Metal, Amount = 5 } }
        });
        
        availableQuests.Add(new Quest
        {
            questId = "kill_beetles_1",
            title = "Очистка территории",
            description = "Уничтожьте 3 жуков",
            type = QuestType.KillEnemies,
            enemyType = "Beetle",
            requiredKills = 3,
            rewards = new List<ResourceCost> { 
                new ResourceCost { Type = ItemType.Crystal_Red, Amount = 2 },
                new ResourceCost { Type = ItemType.Metal, Amount = 10 }
            }
        });
    }
    
    public void AcceptQuest(string questId)
    {
        Quest quest = availableQuests.Find(q => q.questId == questId);
        if (quest != null && activeQuests.Count < maxActiveQuests)
        {
            quest.isActive = true;
            availableQuests.Remove(quest);
            activeQuests.Add(quest);
            
            OnQuestAccepted?.Invoke(quest);
            OnQuestsUpdated?.Invoke();
        }
    }
    
    public void CompleteQuest(string questId)
    {
        Quest quest = activeQuests.Find(q => q.questId == questId);
        if (quest != null && IsQuestComplete(quest))
        {
            quest.isCompleted = true;
            
            // Выдача наград
            foreach (var reward in quest.rewards)
            {
                Inventory.Instance.AddItem(reward.Type, reward.Amount);
            }
            
            activeQuests.Remove(quest);
            completedQuests.Add(quest);
            
            OnQuestCompleted?.Invoke(quest);
            OnQuestsUpdated?.Invoke();
        }
    }
    
    private bool IsQuestComplete(Quest quest)
    {
        switch (quest.type)
        {
            case QuestType.GatherResources:
                foreach (var requirement in quest.requirements)
                {
                    if (Inventory.Instance.GetItemCount(requirement.Type) < requirement.Amount)
                        return false;
                }
                return true;
                
            case QuestType.KillEnemies:
                return quest.currentKills >= quest.requiredKills;
                
            default:
                return false;
        }
    }
    
    private void CheckQuestProgress()
    {
        foreach (var quest in activeQuests)
        {
            if (quest.type == QuestType.GatherResources && IsQuestComplete(quest))
            {
                CompleteQuest(quest.questId);
            }
        }
    }
    
    public void ReportEnemyKill(string enemyType)
    {
        foreach (var quest in activeQuests)
        {
            if (quest.type == QuestType.KillEnemies && quest.enemyType == enemyType)
            {
                quest.currentKills++;
                OnQuestsUpdated?.Invoke();
                
                if (quest.currentKills >= quest.requiredKills)
                {
                    CompleteQuest(quest.questId);
                }
            }
        }
    }
    
    private void RefreshQuests()
    {
        // Удаляем старые неактивные квесты
        availableQuests.Clear();
        
        // Генерируем новые случайные квесты
        GenerateRandomQuests();
        
        OnQuestsUpdated?.Invoke();
    }
    
    private void GenerateRandomQuests()
    {
        // Здесь можно добавить логику генерации случайных квестов
        // на основе прогресса игрока, доступных ресурсов и т.д.
        GenerateInitialQuests();
    }
    
    public List<Quest> GetAvailableQuests() => new List<Quest>(availableQuests);
    public List<Quest> GetActiveQuests() => new List<Quest>(activeQuests);
    public List<Quest> GetCompletedQuests() => new List<Quest>(completedQuests);
}