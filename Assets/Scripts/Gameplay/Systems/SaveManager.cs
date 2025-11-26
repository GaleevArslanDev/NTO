using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Core;
using Data.Game;
using Data.NPC;
using Data.Tech;
using Gameplay.Buildings;
using Gameplay.Characters.NPC;
using Gameplay.Characters.Player;
using Gameplay.Items;
using LocalizationManager;
using UnityEngine;

namespace Gameplay.Systems
{
    public class SaveManager : MonoBehaviour
    {
        public static SaveManager Instance;

        [Header("Save Settings")] public string gameVersion = "1.0";
        public int maxManualSaves = 10;
        public float autoSaveInterval = 300f; // 5 minutes

        [Header("UI")] public GameObject saveIndicator;

        private string SaveFolder => Path.Combine(Application.persistentDataPath, "Saves");
        private string AutoSavePath => Path.Combine(SaveFolder, "autosave.json");

        private float _autoSaveTimer;
        private List<string> _manualSaves = new();

        public event Action OnGameSaved;
        public event Action OnGameLoaded;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeSaveSystem();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            // Загружаем список ручных сохранений
            LoadSaveList();
        }

        private void Update()
        {
            // Автосохранение по таймеру
            _autoSaveTimer += Time.deltaTime;
            if (_autoSaveTimer >= autoSaveInterval)
            {
                AutoSave();
                _autoSaveTimer = 0f;
            }

            // Ручное сохранение по F5
            if (Input.GetKeyDown(KeyCode.F5))
            {
                QuickSave();
            }

            // Загрузка по F9
            if (Input.GetKeyDown(KeyCode.F9))
            {
                QuickLoad();
            }
        }

        private void InitializeSaveSystem()
        {
            // Создаем папку для сохранений если не существует
            if (!Directory.Exists(SaveFolder))
            {
                Directory.CreateDirectory(SaveFolder);
            }
        }

        private void LoadSaveList()
        {
            _manualSaves.Clear();

            if (!Directory.Exists(SaveFolder)) return;

            var files = Directory.GetFiles(SaveFolder, "manual_*.json");
            foreach (var file in files)
            {
                _manualSaves.Add(Path.GetFileNameWithoutExtension(file));
            }

            _manualSaves.Sort();
            _manualSaves.Reverse(); // Новейшие сначала
        }

        public void QuickSave()
        {
            SaveGame("quicksave");
            ShowSaveIndicator();
        }

        public void QuickLoad()
        {
            LoadGame("quicksave");
        }

        public void AutoSave()
        {
            SaveGame("autosave");
            Debug.Log("Game auto-saved");
        }

        public void SaveGame(string saveName = null)
        {
            try
            {
                var saveData = CreateSaveData(saveName);
                var json = JsonUtility.ToJson(saveData, true);
                var filePath = GetSaveFilePath(saveName);

                File.WriteAllText(filePath, json);

                Debug.Log($"Game saved: {filePath}");
                OnGameSaved?.Invoke();

                // Обновляем список сохранений если это ручное сохранение
                if (saveName.StartsWith("manual_"))
                {
                    LoadSaveList();
                    CleanupOldSaves();
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Save failed: {e.Message}");
            }
        }

        public bool LoadGame(string saveName)
        {
            try
            {
                var filePath = GetSaveFilePath(saveName);
                if (!File.Exists(filePath))
                {
                    Debug.LogWarning($"Save file not found: {filePath}");
                    return false;
                }

                var json = File.ReadAllText(filePath);
                var saveData = JsonUtility.FromJson<GameSaveData>(json);

                ApplySaveData(saveData);

                Debug.Log($"Game loaded: {filePath}");
                OnGameLoaded?.Invoke();

                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"Load failed: {e.Message}");
                return false;
            }
        }

        public void DeleteSave(string saveName)
        {
            var filePath = GetSaveFilePath(saveName);
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                LoadSaveList();
            }
        }

        public List<SaveGameInfo> GetSaveGames()
        {
            var saves = new List<SaveGameInfo>();

            if (!Directory.Exists(SaveFolder)) return saves;

            // Автосохранения и быстрые сохранения
            var specialSaves = new[] { "autosave", "quicksave" };
            foreach (var saveName in specialSaves)
            {
                var filePath = GetSaveFilePath(saveName);
                if (File.Exists(filePath))
                {
                    var info = GetSaveGameInfo(saveName);
                    if (info != null) saves.Add(info);
                }
            }

            // Ручные сохранения
            foreach (var saveName in _manualSaves)
            {
                var info = GetSaveGameInfo(saveName);
                if (info != null) saves.Add(info);
            }

            return saves.OrderByDescending(s => s.timestamp).ToList();
        }

        public SaveGameInfo GetSaveGameInfo(string saveName)
        {
            try
            {
                var filePath = GetSaveFilePath(saveName);
                if (!File.Exists(filePath)) return null;

                var json = File.ReadAllText(filePath);
                var saveData = JsonUtility.FromJson<GameSaveData>(json);

                return new SaveGameInfo
                {
                    saveName = saveName,
                    timestamp = saveData.timestamp,
                    gameVersion = saveData.gameVersion,
                    playerLevel = saveData.playerData.level,
                    playTime = "Unknown", // Можно добавить расчет времени игры
                    location = "Town" // Можно добавить определение локации
                };
            }
            catch
            {
                return null;
            }
        }

        public void CreateManualSave(string saveName)
        {
            var formattedName = $"manual_{DateTime.Now:yyyyMMdd_HHmmss}";
            if (!string.IsNullOrEmpty(saveName))
            {
                formattedName = $"manual_{saveName}_{DateTime.Now:yyyyMMdd_HHmmss}";
            }

            SaveGame(formattedName);
            ShowSaveIndicator();
        }

        private void CleanupOldSaves()
        {
            var manualSaves = _manualSaves.Where(s => s.StartsWith("manual_")).ToList();
            if (manualSaves.Count > maxManualSaves)
            {
                for (int i = maxManualSaves; i < manualSaves.Count; i++)
                {
                    DeleteSave(manualSaves[i]);
                }
            }
        }

        private string GetSaveFilePath(string saveName)
        {
            return Path.Combine(SaveFolder, $"{saveName}.json");
        }

        private GameSaveData CreateSaveData(string saveName)
        {
            return new GameSaveData
            {
                saveName = saveName,
                timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                gameVersion = gameVersion,

                playerData = CreatePlayerSaveData(),
                worldData = CreateWorldSaveData(),
                npcsData = CreateNpcsSaveData(),
                buildingData = CreateBuildingSaveData(),
                techData = CreateTechSaveData(),
                inventoryData = CreateInventorySaveData(),
                questData = CreateQuestSaveData(),
                settingsData = CreateSettingsSaveData(),
                collectedResourceIds = CreateCollectedResourcesData()
            };
        }

        // Методы создания данных для сохранения
        private PlayerSaveData CreatePlayerSaveData()
        {
            var player = PlayerData.Instance;
            var playerController = PlayerController.Instance;
            var playerHealth = PlayerHealth.Instance;
            var playerProgression = PlayerProgression.Instance;

            return new PlayerSaveData
            {
                playerID = player.playerID,
                playerName = player.playerName,
                position = playerController.transform.position,
                rotation = playerController.transform.eulerAngles,
                health = playerHealth.currentHealth,
                level = player.playerLevel,
                relationships = player.RelationshipsWithNpCs,
                dialogueHistory = ConvertDialogueHistory(player.dialogueHistory),
                dialogueFlags = ConvertDialogueFlags(player.dialogueFlags),
                progression = new PlayerProgressionSaveData
                {
                    damageMultiplier = playerProgression.damageMultiplier,
                    fireRateMultiplier = playerProgression.fireRateMultiplier,
                    miningSpeedMultiplier = playerProgression.miningSpeedMultiplier,
                    inventoryCapacity = playerProgression.inventoryCapacity,
                    collectionRangeMultiplier = playerProgression.collectionRangeMultiplier,
                    unlockedTechs = playerProgression.GetUnlockedTechsDictionary()
                }
            };
        }

        private Dictionary<int, List<DialogueHistorySaveData>> ConvertDialogueHistory(
            Dictionary<int, List<PlayerData.DialogueHistory>> history)
        {
            var result = new Dictionary<int, List<DialogueHistorySaveData>>();
            foreach (var entry in history)
            {
                var saveList = new List<DialogueHistorySaveData>();
                foreach (var item in entry.Value)
                {
                    saveList.Add(new DialogueHistorySaveData
                    {
                        dialogueTreeName = item.dialogueTreeName,
                        nodeID = item.nodeID,
                        selectedOption = item.selectedOption,
                        timestamp = item.timestamp,
                        flagsSet = item.flagsSet
                    });
                }

                result[entry.Key] = saveList;
            }

            return result;
        }

        private List<DialogueFlagsSaveData> ConvertDialogueFlags(List<PlayerData.DialogueFlags> flags)
        {
            return flags.Select(f => new DialogueFlagsSaveData
            {
                npcID = f.npcID,
                flags = f.flags
            }).ToList();
        }

        private WorldSaveData CreateWorldSaveData()
        {
            var worldTime = WorldTime.Instance;
            var timestamp = worldTime.GetCurrentTimestamp();

            return new WorldSaveData
            {
                currentDay = timestamp.day,
                currentHour = timestamp.hour,
                currentMinute = timestamp.minute,
                timeTimer = worldTime.GetPrivateTimer() // Нужно добавить getter в WorldTime
            };
        }

        private List<NpcSaveData> CreateNpcsSaveData()
        {
            var npcs = new List<NpcSaveData>();
            var npcManager = NpcManager.Instance;
            var relationshipManager = RelationshipManager.Instance;

            if (npcManager != null)
            {
                foreach (var npcBehaviour in npcManager.GetAllNpcs())
                {
                    var npcInteraction = npcBehaviour.GetComponent<NpcInteraction>();
                    if (npcInteraction?.npcData != null)
                    {
                        var reactiveTrigger = npcBehaviour.GetComponent<ReactiveDialogueTrigger>();

                        npcs.Add(new NpcSaveData
                        {
                            npcID = npcInteraction.npcData.npcID,
                            npcName = npcInteraction.npcData.npcName,
                            position = npcBehaviour.transform.position,
                            rotation = npcBehaviour.transform.eulerAngles,
                            currentState = npcInteraction.npcData.currentState,
                            relationships = npcInteraction.npcData.Relationships,
                            memories = ConvertMemories(npcInteraction.npcData.memories),
                            currentDialogueIndex = reactiveTrigger?.GetCurrentDialogueIndex() ?? 0,
                            canCall = reactiveTrigger?.CanCall ?? true,
                            lastCallTime = reactiveTrigger?.GetLastCallTime() ?? 0,
                            currentActivity = CreateActivitySaveData(npcInteraction.npcData)
                        });
                    }
                }
            }

            return npcs;
        }

        private List<MemorySaveData> ConvertMemories(List<Memory> memories)
        {
            return memories.Select(m => new MemorySaveData
            {
                memoryText = m.memoryText,
                relationshipImpact = m.relationshipImpact,
                sourceNpc = m.sourceNpc,
                timestamp = m.timestamp
            }).ToList();
        }

        private ActivitySaveData CreateActivitySaveData(NpcData npcData)
        {
            var scheduleManager = FindObjectOfType<ScheduleManager>();
            if (scheduleManager != null)
            {
                var activity = scheduleManager.GetCurrentActivity(npcData);
                return new ActivitySaveData
                {
                    type = activity.type,
                    location = activity.location,
                    targetNpc = activity.targetNpc,
                    duration = activity.duration
                };
            }

            return new ActivitySaveData();
        }

        private BuildingSaveData CreateBuildingSaveData()
        {
            var buildingData = new BuildingSaveData();
            var buildingManager = BuildingManager.Instance;
            var townHall = TownHall.Instance;
            var farmManager = FarmManager.Instance;

            if (buildingManager != null)
            {
                foreach (var building in buildingManager.GetAllBuildings())
                {
                    buildingData.buildingLevels[building.GetBuildingId()] = building.GetCurrentLevel();
                }

                buildingData.unlockedBuildings = buildingManager.GetUnlockedBuildingsDictionary();
            }

            if (townHall != null)
            {
                buildingData.townHallLevel = townHall.GetCurrentLevel();
                buildingData.unlockedTechTier = townHall.GetUnlockedTechTier();
            }

            if (farmManager != null)
            {
                foreach (var plot in farmManager.GetFarmPlots())
                {
                    buildingData.farmPlots[plot.Key] = new FarmPlotSaveData
                    {
                        plotId = plot.Value.plotId,
                        resourceType = plot.Value.resourceType,
                        productionRate = plot.Value.productionRate,
                        isActive = plot.Value.isActive,
                        timer = plot.Value.timer
                    };
                }
            }

            return buildingData;
        }

        private TechSaveData CreateTechSaveData()
        {
            return PlayerProgression.Instance != null 
                ? PlayerProgression.Instance.GetTechSaveData()
                : new TechSaveData();
        }

        private InventorySaveData CreateInventorySaveData()
        {
            var inventory = Inventory.Instance;
            var inventoryData = new InventorySaveData();

            if (inventory != null)
            {
                foreach (var slot in inventory.GetAllItems())
                {
                    inventoryData.items[slot.type.ToString()] = slot.count;
                }

                inventoryData.capacity = inventory.GetCapacity();
            }

            // Золото из PlayerData
            var playerData = PlayerData.Instance;
            if (playerData != null)
            {
                inventoryData.gold = playerData.gold;
            }

            return inventoryData;
        }

        private QuestSaveData CreateQuestSaveData()
        {
            var questData = new QuestSaveData();
            var questSystem = QuestSystem.Instance;
            var playerData = PlayerData.Instance;
    
            if (questSystem != null)
            {
                questData.activeQuests = new List<string>(questSystem.GetActiveQuests().Select(q => q.questId));
                questData.completedQuests = new List<string>(questSystem.GetCompletedQuests().Select(q => q.questId));
        
                // Сохраняем прогресс для активных квестов
                foreach (var quest in questSystem.GetActiveQuests())
                {
                    questData.questProgress[quest.questId] = new QuestProgressSaveData
                    {
                        currentKills = quest.currentKills,
                        gatheredResources = quest.currentResources ?? new Dictionary<string, int>()
                    };
                }
            }
    
            // Для совместимости с PlayerData
            if (playerData != null)
            {
                // Миграция старых данных если нужно
                if (questData.activeQuests.Count == 0 && playerData.activeQuests.Count > 0)
                {
                    questData.activeQuests = playerData.activeQuests;
                }
        
                if (questData.completedQuests.Count == 0 && playerData.completedQuests.Count > 0)
                {
                    questData.completedQuests = playerData.completedQuests;
                }
        
                playerData.activeQuests = questData.activeQuests;
                playerData.completedQuests = questData.completedQuests;
            }
    
            return questData;
        }

        private SettingsSaveData CreateSettingsSaveData()
        {
            var settings = new SettingsSaveData();
    
            // Настройки аудио
            settings.masterVolume = AudioListener.volume;
    
            // Настройки языка
            if (LocalizationManager.LocalizationManager.Instance != null)
            {
                settings.language = LocalizationManager.LocalizationManager.Instance.GetCurrentLanguage();
            }
    
            // Настройки графики
            settings.fullscreen = Screen.fullScreen;
            settings.resolutionWidth = Screen.width;
            settings.resolutionHeight = Screen.height;
    
            return settings;
        }

        private List<string> CreateCollectedResourcesData()
        {
            return ResourceManager.Instance != null 
                ? new List<string>(ResourceManager.Instance.GetCollectedResources())
                : new List<string>();
        }
        
        private void InitializeSystems()
        {
            // Инициализируем PlayerProgression если он еще не инициализирован
            if (PlayerProgression.Instance != null)
            {
                var progressionInitialized = PlayerProgression.Instance.GetUnlockedTechsDictionary().Count > 0;
                if (!progressionInitialized)
                {
                    PlayerProgression.Instance.Initialize();
                }
            }
        }
        
        public void StartNewGame()
        {
            // Сбрасываем все системы к начальному состоянию
            if (PlayerProgression.Instance != null)
            {
                PlayerProgression.Instance.ResetTechProgress();
            }
    
            if (ResourceManager.Instance != null)
            {
                ResourceManager.Instance.SetCollectedResources(new HashSet<string>());
            }
    
            if (QuestSystem.Instance != null)
            {
                QuestSystem.Instance.ResetQuests();
            }
    
            // Сбрасываем инвентарь
            if (Inventory.Instance != null)
            {
                Inventory.Instance.ClearInventory();
            }
    
            Debug.Log("New game started");
        }

        // Методы применения загруженных данных
        private void ApplySaveData(GameSaveData saveData)
        {
            if (saveData == null) return;

            try
            {
                // Сначала инициализируем системы, если они еще не инициализированы
                InitializeSystems();
        
                ApplyPlayerSaveData(saveData.playerData);
                ApplyWorldSaveData(saveData.worldData);
                ApplyNpcsSaveData(saveData.npcsData);
                ApplyBuildingSaveData(saveData.buildingData);
                ApplyTechSaveData(saveData.techData);
                ApplyInventorySaveData(saveData.inventoryData);
                ApplyQuestSaveData(saveData.questData);
                ApplySettingsSaveData(saveData.settingsData);
                ApplyCollectedResourcesData(saveData.collectedResourceIds);

                Debug.Log("Save data applied successfully");
            }
            catch (Exception e)
            {
                Debug.LogError($"Error applying save data: {e.Message}");
            }
        }

        private void ApplyPlayerSaveData(PlayerSaveData data)
        {
            var player = PlayerData.Instance;
            var playerController = PlayerController.Instance;
            var playerHealth = PlayerHealth.Instance;
            var playerProgression = PlayerProgression.Instance;

            if (playerController != null)
            {
                playerController.transform.position = data.position;
                playerController.transform.eulerAngles = data.rotation;
            }

            if (playerHealth != null)
            {
                playerHealth.currentHealth = data.health;
            }

            if (player != null)
            {
                player.playerID = data.playerID;
                player.playerName = data.playerName;
                player.playerLevel = data.level;
                player.RelationshipsWithNpCs = data.relationships ?? new Dictionary<int, int>();
                player.dialogueHistory = ConvertSaveDialogueHistory(data.dialogueHistory);
                player.dialogueFlags = ConvertSaveDialogueFlags(data.dialogueFlags);
            }

            if (playerProgression != null && data.progression != null)
            {
                playerProgression.damageMultiplier = data.progression.damageMultiplier;
                playerProgression.fireRateMultiplier = data.progression.fireRateMultiplier;
                playerProgression.miningSpeedMultiplier = data.progression.miningSpeedMultiplier;
                playerProgression.inventoryCapacity = data.progression.inventoryCapacity;
                playerProgression.collectionRangeMultiplier = data.progression.collectionRangeMultiplier;
                playerProgression.ApplyUnlockedTechs(data.progression.unlockedTechs);
            }
        }

        private Dictionary<int, List<PlayerData.DialogueHistory>> ConvertSaveDialogueHistory(
            Dictionary<int, List<DialogueHistorySaveData>> saveHistory)
        {
            var result = new Dictionary<int, List<PlayerData.DialogueHistory>>();
            foreach (var entry in saveHistory)
            {
                var historyList = new List<PlayerData.DialogueHistory>();
                foreach (var item in entry.Value)
                {
                    var history = new PlayerData.DialogueHistory(
                        item.dialogueTreeName,
                        item.nodeID,
                        item.selectedOption,
                        item.timestamp
                    );
                    history.flagsSet = item.flagsSet;
                    historyList.Add(history);
                }

                result[entry.Key] = historyList;
            }

            return result;
        }

        private List<PlayerData.DialogueFlags> ConvertSaveDialogueFlags(List<DialogueFlagsSaveData> saveFlags)
        {
            return saveFlags.Select(f => new PlayerData.DialogueFlags(f.npcID) { flags = f.flags }).ToList();
        }

        private void ApplyWorldSaveData(WorldSaveData data)
        {
            var worldTime = WorldTime.Instance;
            if (worldTime != null)
            {
                worldTime.SetTime(data.currentDay, data.currentHour, data.currentMinute);
                worldTime.SetPrivateTimer(data.timeTimer); // Нужно добавить setter в WorldTime
            }
        }

        private void ApplyNpcsSaveData(List<NpcSaveData> data)
        {
            var npcManager = NpcManager.Instance;
            var relationshipManager = RelationshipManager.Instance;

            if (npcManager == null || relationshipManager == null) return;

            foreach (var npcSaveData in data)
            {
                var npcBehaviour = npcManager.GetNpcByID(npcSaveData.npcID);
                if (npcBehaviour != null)
                {
                    // Восстанавливаем позицию и rotation
                    npcBehaviour.transform.position = npcSaveData.position;
                    npcBehaviour.transform.eulerAngles = npcSaveData.rotation;

                    var npcInteraction = npcBehaviour.GetComponent<NpcInteraction>();
                    if (npcInteraction?.npcData != null)
                    {
                        // Восстанавливаем данные NPC
                        npcInteraction.npcData.currentState = npcSaveData.currentState;
                        npcInteraction.npcData.Relationships = npcSaveData.relationships ?? new Dictionary<int, int>();
                        npcInteraction.npcData.memories = ConvertSaveMemories(npcSaveData.memories);

                        // Регистрируем в менеджере отношений
                        relationshipManager.RegisterNpc(npcInteraction.npcData);

                        // Восстанавливаем реактивные диалоги
                        var reactiveTrigger = npcBehaviour.GetComponent<ReactiveDialogueTrigger>();
                        if (reactiveTrigger != null)
                        {
                            reactiveTrigger.SetCurrentDialogueIndex(npcSaveData.currentDialogueIndex);
                            reactiveTrigger.SetCallCooldown(npcSaveData.lastCallTime);
                            reactiveTrigger.SetCanCall(npcSaveData.canCall);
                        }
                    }
                }
            }
        }

        private List<Memory> ConvertSaveMemories(List<MemorySaveData> saveMemories)
        {
            return saveMemories.Select(m => new Memory(m.memoryText, m.relationshipImpact, m.sourceNpc, m.timestamp))
                .ToList();
        }

        private void ApplyBuildingSaveData(BuildingSaveData data)
        {
            var buildingManager = BuildingManager.Instance;
            var townHall = TownHall.Instance;
            var farmManager = FarmManager.Instance;

            if (buildingManager != null)
            {
                // Восстанавливаем уровни зданий
                foreach (var buildingLevel in data.buildingLevels)
                {
                    var building = buildingManager.GetBuilding(buildingLevel.Key);
                    if (building != null)
                    {
                        building.SetLevel(buildingLevel.Value);
                    }
                }

                // Восстанавливаем разблокированные здания
                buildingManager.ApplyUnlockedBuildings(data.unlockedBuildings);
            }

            if (townHall != null)
            {
                // Восстанавливаем уровень ратуши
                for (int i = 0; i < data.townHallLevel; i++)
                {
                    // Нужно добавить метод для установки уровня в TownHall
                    townHall.SetLevel(data.townHallLevel);
                }

                townHall.UnlockUpgradeTier(data.unlockedTechTier);
            }

            if (farmManager != null)
            {
                // Восстанавливаем фермерские участки
                foreach (var plotData in data.farmPlots.Values)
                {
                    farmManager.UnlockFarmPlot(plotData.plotId, plotData.resourceType, plotData.productionRate);
                    if (plotData.isActive)
                    {
                        farmManager.SetPlotTimer(plotData.plotId, plotData.timer);
                    }
                }
            }
        }

        private void ApplyTechSaveData(TechSaveData techData)
        {
            if (PlayerProgression.Instance != null)
            {
                PlayerProgression.Instance.ApplyTechSaveData(techData);
            }
        }
        
        private void ApplyInventorySaveData(InventorySaveData data)
        {
            var inventory = Inventory.Instance;
            var playerData = PlayerData.Instance;

            if (inventory != null)
            {
                // Очищаем инвентарь
                inventory.ClearInventory();

                // Восстанавливаем предметы
                foreach (var itemEntry in data.items)
                {
                    if (Enum.TryParse<ItemType>(itemEntry.Key, out var itemType))
                    {
                        inventory.AddItem(itemType, itemEntry.Value);
                    }
                }

                inventory.UpdateCapacity(data.capacity);
            }

            if (playerData != null)
            {
                playerData.gold = data.gold;
            }
        }

        private void ApplyQuestSaveData(QuestSaveData data)
        {
            var questSystem = QuestSystem.Instance;
            var playerData = PlayerData.Instance;

            if (questSystem != null)
            {
                questSystem.ApplySaveData(data);
            }

            if (playerData != null)
            {
                playerData.activeQuests = data.activeQuests;
                playerData.completedQuests = data.completedQuests;
            }
        }

        private void ApplySettingsSaveData(SettingsSaveData settingsData)
        {
            if (settingsData == null) return;
    
            // Применяем настройки аудио
            AudioListener.volume = settingsData.masterVolume;
    
            // Применяем настройки языка
            if (LocalizationManager.LocalizationManager.Instance != null && 
                !string.IsNullOrEmpty(settingsData.language))
            {
                LocalizationManager.LocalizationManager.Instance.SetLanguage(settingsData.language);
            }
    
            // Применяем настройки графики
            Screen.SetResolution(
                settingsData.resolutionWidth, 
                settingsData.resolutionHeight, 
                settingsData.fullscreen
            );
        }
        
        public SettingsSaveData GetSettingsData()
        {
            return CreateSettingsSaveData();
        }

        private void ApplyCollectedResourcesData(List<string> collectedResourceIds)
        {
            if (ResourceManager.Instance != null)
            {
                ResourceManager.Instance.SetCollectedResources(
                    new HashSet<string>(collectedResourceIds)
                );
            }
        }

        private void ShowSaveIndicator()
        {
            if (saveIndicator != null)
            {
                saveIndicator.SetActive(true);
                Invoke(nameof(HideSaveIndicator), 2f);
            }
        }

        private void HideSaveIndicator()
        {
            if (saveIndicator != null)
            {
                saveIndicator.SetActive(false);
            }
        }

        private void OnApplicationQuit()
        {
            // Автосохранение при выходе
            AutoSave();
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                // Автосохранение при паузе
                AutoSave();
            }
        }
    }

    [System.Serializable]
    public class SaveGameInfo
    {
        public string saveName;
        public string timestamp;
        public string gameVersion;
        public int playerLevel;
        public string playTime;
        public string location;
        public Texture2D screenshot; // Можно добавить систему скриншотов
    }
}