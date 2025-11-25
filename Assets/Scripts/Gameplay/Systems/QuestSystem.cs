using System;
using System.Collections.Generic;
using System.Linq;
using Core;
using Data.Game;
using Gameplay.Items;
using UnityEngine;

namespace Gameplay.Systems
{
    public sealed class QuestSystem : MonoBehaviour
    {
        public static QuestSystem Instance;
    
        [Serializable]
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
    
        [Header("Quest Settings")]
        public int maxActiveQuests = 3;
        public float questRefreshTime = 300f; // 5 минут
    
        private List<Quest> _availableQuests = new();
        private List<Quest> _activeQuests = new();
        private List<Quest> _completedQuests = new();
        private float _questRefreshTimer;
    
        public event Action<Quest> OnQuestAccepted;
        public event Action<Quest> OnQuestCompleted;
        public event Action OnQuestsUpdated;

        private void Awake()
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

        private void Start()
        {
            GenerateInitialQuests();
            _questRefreshTimer = questRefreshTime;
        }

        public void Update()
        {
            // Авто-обновление квестов
            _questRefreshTimer -= Time.deltaTime;
            if (_questRefreshTimer <= 0)
            {
                RefreshQuests();
                _questRefreshTimer = questRefreshTime;
            }
        
            // Проверка выполнения квестов
            CheckQuestProgress();
        }
    
        private void GenerateInitialQuests()
        {
            // Примеры квестов
            _availableQuests.Add(new Quest
            {
                questId = "gather_wood_1",
                title = "Заготовка древесины",
                description = "Соберите 20 единиц дерева",
                type = QuestType.GatherResources,
                requirements = new List<ResourceCost> { new ResourceCost { type = ItemType.Wood, amount = 20 } },
                rewards = new List<ResourceCost> { new ResourceCost { type = ItemType.Metal, amount = 5 } }
            });
        
            _availableQuests.Add(new Quest
            {
                questId = "kill_beetles_1",
                title = "Очистка территории",
                description = "Уничтожьте 3 жуков",
                type = QuestType.KillEnemies,
                enemyType = "Beetle",
                requiredKills = 3,
                rewards = new List<ResourceCost> { 
                    new ResourceCost { type = ItemType.CrystalRed, amount = 2 },
                    new ResourceCost { type = ItemType.Metal, amount = 10 }
                }
            });
        }
    
        public void AcceptQuest(string questId)
        {
            var quest = _availableQuests.Find(q => q.questId == questId);
            if (quest == null || _activeQuests.Count >= maxActiveQuests) return;
            quest.isActive = true;
            _availableQuests.Remove(quest);
            _activeQuests.Add(quest);
            
            OnQuestAccepted?.Invoke(quest);
            OnQuestsUpdated?.Invoke();
        }
    
        public void CompleteQuest(string questId)
        {
            var quest = _activeQuests.Find(q => q.questId == questId);
            if (quest == null || !IsQuestComplete(quest)) return;
            quest.isCompleted = true;
            
            // Выдача наград
            foreach (var reward in quest.rewards)
            {
                Inventory.Instance.AddItem(reward.type, reward.amount);
            }
            
            _activeQuests.Remove(quest);
            _completedQuests.Add(quest);
            
            OnQuestCompleted?.Invoke(quest);
            OnQuestsUpdated?.Invoke();
        }

        private static bool IsQuestComplete(Quest quest)
        {
            switch (quest.type)
            {
                case QuestType.GatherResources:
                {
                    return quest.requirements.All(requirement => Inventory.Instance.GetItemCount(requirement.type) >= requirement.amount);
                }
                case QuestType.KillEnemies:
                    return quest.currentKills >= quest.requiredKills;
                default:
                    return false;
            }
        }
    
        private void CheckQuestProgress()
        {
            foreach (var quest in _activeQuests.Where(quest => quest.type == QuestType.GatherResources && IsQuestComplete(quest)))
            {
                CompleteQuest(quest.questId);
            }
        }
    
        public void ReportEnemyKill(string enemyType)
        {
            foreach (var quest in _activeQuests.Where(quest => quest.type == QuestType.KillEnemies && quest.enemyType == enemyType))
            {
                quest.currentKills++;
                OnQuestsUpdated?.Invoke();
                
                if (quest.currentKills >= quest.requiredKills)
                {
                    CompleteQuest(quest.questId);
                }
            }
        }
    
        private void RefreshQuests()
        {
            // Удаляем старые неактивные квесты
            _availableQuests.Clear();
        
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
    
        public List<Quest> GetAvailableQuests() => new List<Quest>(_availableQuests);
        public List<Quest> GetActiveQuests() => new List<Quest>(_activeQuests);
        public List<Quest> GetCompletedQuests() => new List<Quest>(_completedQuests);
    }
}