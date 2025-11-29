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

            // Для квестов на сбор ресурсов (дополнительные поля)
            public Dictionary<string, int> requiredResources; // Тип ресурса -> количество
            public Dictionary<string, int> currentResources; // Текущий прогресс

            // Время создания/принятия квеста
            public string acceptedTime;
            public string completedTime;

            public Quest()
            {
                requirements = new List<ResourceCost>();
                rewards = new List<ResourceCost>();
                requiredResources = new Dictionary<string, int>();
                currentResources = new Dictionary<string, int>();
                currentKills = 0;
            }

            // Метод для проверки выполнения квеста
            public bool CheckCompletion()
            {
                switch (type)
                {
                    case QuestType.GatherResources:
                        return CheckResourceCompletion();
                    case QuestType.KillEnemies:
                        return currentKills >= requiredKills;
                    case QuestType.UpgradeBuilding:
                        // Логика для проверки улучшений зданий
                        return false;
                    case QuestType.UnlockTech:
                        // Логика для проверки разблокированных технологий
                        return false;
                    default:
                        return false;
                }
            }

            private bool CheckResourceCompletion()
            {
                if (requirements == null || requirements.Count == 0) return false;

                var inventory = Inventory.Instance;
                if (inventory == null) return false;

                return requirements.All(req => inventory.GetItemCount(req.type) >= req.amount);
            }
        }

        [Header("Quest Settings")] public int maxActiveQuests = 3;
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
            // Очищаем существующие квесты
            _availableQuests.Clear();

            // Создаем базовые квесты только если их нет
            if (_availableQuests.All(q => q.questId != "gather_wood_1"))
            {
                _availableQuests.Add(new Quest
                {
                    questId = "gather_wood_1",
                    title = "Заготовка древесины",
                    description = "Соберите 20 единиц дерева",
                    type = QuestType.GatherResources,
                    requirements = new List<ResourceCost> { new ResourceCost { type = ItemType.Wood, amount = 20 } },
                    rewards = new List<ResourceCost> { new ResourceCost { type = ItemType.Metal, amount = 5 } },
                    isCompleted = false,
                    isActive = false
                });
            }

            if (_availableQuests.All(q => q.questId != "kill_beetles_1"))
            {
                _availableQuests.Add(new Quest
                {
                    questId = "kill_beetles_1",
                    title = "Очистка территории",
                    description = "Уничтожьте 3 жуков",
                    type = QuestType.KillEnemies,
                    enemyType = "Beetle",
                    requiredKills = 3,
                    currentKills = 0,
                    rewards = new List<ResourceCost>
                    {
                        new ResourceCost { type = ItemType.CrystalRed, amount = 2 },
                        new ResourceCost { type = ItemType.Metal, amount = 10 }
                    },
                    isCompleted = false,
                    isActive = false
                });
            }
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
                    return quest.requirements.All(requirement =>
                        Inventory.Instance.GetItemCount(requirement.type) >= requirement.amount);
                }
                case QuestType.KillEnemies:
                    return quest.currentKills >= quest.requiredKills;
                default:
                    return false;
            }
        }

        private void CheckQuestProgress()
        {
            // Создаем копию списка для безопасного перечисления
            var activeQuestsCopy = new List<Quest>(_activeQuests);
    
            foreach (var quest in activeQuestsCopy)
            {
                if (quest.type == QuestType.GatherResources && IsQuestComplete(quest))
                {
                    CompleteQuest(quest.questId);
                }
            }
        }

        public void ReportEnemyKill(string enemyType)
        {
            foreach (var quest in _activeQuests.Where(quest =>
                         quest.type == QuestType.KillEnemies && quest.enemyType == enemyType))
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

        public void ApplySaveData(QuestSaveData saveData)
{
    if (saveData == null) return;

    try
    {
        // Очищаем текущие состояния квестов
        _activeQuests.Clear();
        _completedQuests.Clear();
        _availableQuests.Clear();

        // Генерируем базовые квесты
        GenerateInitialQuests();

        // Восстанавливаем завершенные квесты
        foreach (var questId in saveData.completedQuests)
        {
            var quest = FindQuestById(questId);
            if (quest != null)
            {
                quest.isCompleted = true;
                quest.isActive = false;
                _completedQuests.Add(quest);
            }
        }

        // Восстанавливаем активные квесты
        foreach (var questId in saveData.activeQuests)
        {
            var quest = FindQuestById(questId);
            if (quest != null)
            {
                quest.isActive = true;
                quest.isCompleted = false;

                // Восстанавливаем прогресс квеста
                if (saveData.questProgress != null && 
                    saveData.questProgress.TryGetValue(questId, out var progressData))
                {
                    quest.currentKills = progressData.currentKills;

                    // Восстанавливаем прогресс сбора ресурсов
                    if (progressData.gatheredResources != null)
                    {
                        quest.currentResources = progressData.gatheredResources.ToDictionary();
                    }
                }

                _activeQuests.Add(quest);
                
                // Удаляем из доступных если он там есть
                _availableQuests.RemoveAll(q => q.questId == questId);
            }
        }

        // Обновляем UI
        OnQuestsUpdated?.Invoke();

        Debug.Log($"Quests loaded: {_activeQuests.Count} active, {_completedQuests.Count} completed, {_availableQuests.Count} available");
    }
    catch (Exception e)
    {
        Debug.LogError($"Error applying quest save data: {e.Message}");
        // В случае ошибки генерируем начальные квесты
        ResetQuests();
    }
}

// Вспомогательные методы для ApplySaveData

        private Quest FindQuestById(string questId)
        {
            // Ищем в активных квестах
            var quest = _activeQuests.Find(q => q.questId == questId);
            if (quest != null) return quest;

            // Ищем в завершенных квестах
            quest = _completedQuests.Find(q => q.questId == questId);
            if (quest != null) return quest;

            // Ищем в доступных квестах
            quest = _availableQuests.Find(q => q.questId == questId);
            if (quest != null) return quest;

            // Пытаемся создать квест по ID
            return CreateQuestById(questId);
        }

        private Quest CreateQuestById(string questId)
        {
            // Здесь можно добавить логику создания квестов по их ID
            // Например, из базы данных квестов или по шаблонам

            // Временно создаем базовый квест
            var quest = new Quest
            {
                questId = questId,
                title = $"Quest {questId}",
                description = "Restored from save",
                type = QuestType.GatherResources, // По умолчанию
                requirements = new List<ResourceCost>(),
                rewards = new List<ResourceCost>(),
                isCompleted = false,
                isActive = false
            };

            return quest;
        }

        private List<string> GetAllQuestIds()
        {
            var allIds = new List<string>();

            // Собираем ID из всех списков
            allIds.AddRange(_activeQuests.Select(q => q.questId));
            allIds.AddRange(_completedQuests.Select(q => q.questId));
            allIds.AddRange(_availableQuests.Select(q => q.questId));

            // Добавляем ID из сохраненных данных чтобы не потерять квесты
            if (_availableQuests.Count == 0 && _activeQuests.Count == 0 && _completedQuests.Count == 0)
            {
                // Если все списки пустые, возвращаем стандартный набор
                return new List<string> { "gather_wood_1", "kill_beetles_1" };
            }

            return allIds.Distinct().ToList();
        }

        private void ApplyQuestResourceProgress(Quest quest, Dictionary<string, int> gatheredResources)
        {
            if (quest.type != QuestType.GatherResources || gatheredResources == null) return;

            // Восстанавливаем прогресс сбора ресурсов для квестов на сбор
            // Здесь можно добавить логику для восстановления конкретного прогресса
            // Например, если у нас есть отдельная система отслеживания собранных ресурсов для квестов
            Debug.Log($"Restored resource progress for quest {quest.questId}");
        }

        public QuestProgressSaveData GetQuestProgressData(string questId)
        {
            var quest = FindQuestById(questId);
            if (quest == null) return null;

            var progressData = new QuestProgressSaveData
            {
                currentKills = quest.currentKills,
                gatheredResources = new Data.Game.StringIntDictionary()
            };

            // Преобразуем обычный Dictionary в StringIntDictionary
            if (quest.currentResources != null)
            {
                progressData.gatheredResources.FromDictionary(quest.currentResources);
            }

            return progressData;
        }

        public void ResetQuests()
        {
            _activeQuests.Clear();
            _completedQuests.Clear();
            _availableQuests.Clear();

            GenerateInitialQuests();
            OnQuestsUpdated?.Invoke();
        }

        public void MigrateFromOldSave(List<string> oldActiveQuests, List<string> oldCompletedQuests)
        {
            if (oldActiveQuests != null)
            {
                foreach (var questId in oldActiveQuests)
                {
                    if (!_activeQuests.Any(q => q.questId == questId))
                    {
                        var quest = FindQuestById(questId);
                        if (quest != null)
                        {
                            quest.isActive = true;
                            _activeQuests.Add(quest);
                            _availableQuests.RemoveAll(q => q.questId == questId);
                        }
                    }
                }
            }

            if (oldCompletedQuests != null)
            {
                foreach (var questId in oldCompletedQuests)
                {
                    if (!_completedQuests.Any(q => q.questId == questId))
                    {
                        var quest = FindQuestById(questId);
                        if (quest != null)
                        {
                            quest.isCompleted = true;
                            quest.isActive = false;
                            _completedQuests.Add(quest);
                            _activeQuests.RemoveAll(q => q.questId == questId);
                            _availableQuests.RemoveAll(q => q.questId == questId);
                        }
                    }
                }
            }

            OnQuestsUpdated?.Invoke();
        }

        public QuestSaveData GetQuestSaveData()
        {
            var saveData = new QuestSaveData();

            saveData.activeQuests = new List<string>(_activeQuests.Select(q => q.questId));
            saveData.completedQuests = new List<string>(_completedQuests.Select(q => q.questId));

            // Сохраняем прогресс квестов
            saveData.questProgress = new StringQuestProgressDictionary();
            foreach (var quest in _activeQuests)
            {
                var progressData = new QuestProgressSaveData
                {
                    currentKills = quest.currentKills
                };

                // Сохраняем прогресс ресурсов если нужно
                if (quest.currentResources != null)
                {
                    progressData.gatheredResources = new Data.Game.StringIntDictionary();
                    progressData.gatheredResources.FromDictionary(quest.currentResources);
                }

                saveData.questProgress[quest.questId] = progressData;
            }

            return saveData;
        }

        public void ApplyQuestSaveData(QuestSaveData saveData)
        {
            if (saveData == null) return;

            // Очищаем текущее состояние
            _activeQuests.Clear();
            _completedQuests.Clear();
            _availableQuests.Clear();

            // Восстанавливаем завершенные квесты
            foreach (var questId in saveData.completedQuests)
            {
                var quest = FindQuestById(questId);
                if (quest != null)
                {
                    quest.isCompleted = true;
                    _completedQuests.Add(quest);
                }
            }

            // Восстанавливаем активные квесты
            foreach (var questId in saveData.activeQuests)
            {
                var quest = FindQuestById(questId);
                if (quest != null)
                {
                    quest.isActive = true;

                    // Восстанавливаем прогресс
                    if (saveData.questProgress != null &&
                        saveData.questProgress.TryGetValue(questId, out var progress))
                    {
                        quest.currentKills = progress.currentKills;

                        if (progress.gatheredResources != null)
                        {
                            quest.currentResources = progress.gatheredResources.ToDictionary();
                        }
                    }

                    _activeQuests.Add(quest);
                }
            }

            // Восстанавливаем доступные квесты
            GenerateInitialQuests();
            foreach (var quest in _availableQuests.ToList())
            {
                if (saveData.activeQuests.Contains(quest.questId) ||
                    saveData.completedQuests.Contains(quest.questId))
                {
                    _availableQuests.Remove(quest);
                }
            }
        }

        public List<Quest> GetAvailableQuests() => new List<Quest>(_availableQuests);
        public List<Quest> GetActiveQuests() => new List<Quest>(_activeQuests);
        public List<Quest> GetCompletedQuests() => new List<Quest>(_completedQuests);
    }
}