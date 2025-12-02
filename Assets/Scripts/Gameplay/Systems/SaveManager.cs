using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
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

        [Header("Screenshot Settings")] [SerializeField]
        private int screenshotWidth = 320;

        [SerializeField] private int screenshotHeight = 180;
        [SerializeField] private int screenshotQuality = 75;
        [SerializeField] private bool enableScreenshots = true;

        [Header("AutoSave Screenshot")] [SerializeField]
        private bool enableAutoSaveScreenshots = false;

        [Header("Validation Settings")] [SerializeField]
        private bool enableChecksum = true;

        [SerializeField] private bool backupSaves = true;
        [SerializeField] private int maxBackupCount = 3;

        [Header("UI")] public GameObject saveIndicator;

        private string SaveFolder => Path.Combine(Application.persistentDataPath, "Saves");
        private string AutoSavePath => Path.Combine(SaveFolder, "autosave.json");

        private Coroutine _screenshotCoroutine;

        private float _autoSaveTimer;
        private List<string> _manualSaves = new();
        private bool _isQuitting;

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

        public void LoadSaveAfterSceneLoad(string saveName)
        {
            if (string.IsNullOrEmpty(saveName)) return;

            // Ждем завершения загрузки сцены и инициализации всех систем
            StartCoroutine(LoadSaveAfterDelay(saveName, 0.1f));
        }

        private IEnumerator LoadSaveAfterDelay(string saveName, float delay)
        {
            yield return new WaitForSeconds(delay);

            if (LoadGame(saveName))
            {
                Debug.Log($"Save loaded after scene transition: {saveName}");
            }
            else
            {
                Debug.LogError($"Failed to load save after scene transition: {saveName}");
            }
        }

        public bool SaveExists(string saveName)
        {
            var filePath = GetSaveFilePath(saveName);
            return File.Exists(filePath);
        }

        private IEnumerator TakeScreenshotCoroutine(string saveName, System.Action<Texture2D> callback)
        {
            // Ждем конец кадра чтобы захватить весь экран
            yield return new WaitForEndOfFrame();

            try
            {
                // Создаем текстуру для скриншота
                var screenTexture = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);

                // Читаем пиксели с экрана
                screenTexture.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
                screenTexture.Apply();

                // Масштабируем до миниатюры
                var scaledTexture = ScaleTexture(screenTexture, screenshotWidth, screenshotHeight);
                Destroy(screenTexture);

                callback(scaledTexture);
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to take screenshot: {e.Message}");
                callback(null);
            }
        }

        private Texture2D ScaleTexture(Texture2D source, int targetWidth, int targetHeight)
        {
            var result = new Texture2D(targetWidth, targetHeight, source.format, false);

            var scaleX = (float)source.width / targetWidth;
            var scaleY = (float)source.height / targetHeight;

            for (var y = 0; y < targetHeight; y++)
            {
                for (var x = 0; x < targetWidth; x++)
                {
                    var sourceX = Mathf.FloorToInt(x * scaleX);
                    var sourceY = Mathf.FloorToInt(y * scaleY);
                    var color = source.GetPixel(sourceX, sourceY);
                    result.SetPixel(x, y, color);
                }
            }

            result.Apply();
            return result;
        }

        private string SaveScreenshotToBase64(Texture2D screenshot)
        {
            if (screenshot == null) return string.Empty;

            try
            {
                var bytes = screenshot.EncodeToJPG(screenshotQuality);
                var base64 = Convert.ToBase64String(bytes);
                Destroy(screenshot);
                return base64;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to encode screenshot: {e.Message}");
                return string.Empty;
            }
        }

        public Texture2D LoadScreenshotFromBase64(string base64Data)
        {
            if (string.IsNullOrEmpty(base64Data)) return null;

            try
            {
                var bytes = Convert.FromBase64String(base64Data);
                var texture = new Texture2D(2, 2);
                texture.LoadImage(bytes);
                return texture;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to decode screenshot: {e.Message}");
                return null;
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
            if (enableAutoSaveScreenshots)
            {
                StartCoroutine(CreateAutoSaveWithScreenshot());
            }
            else
            {
                SaveGame("autosave");
            }

            Debug.Log("Game auto-saved");
        }

        private IEnumerator CreateAutoSaveWithScreenshot()
        {
            // Показываем индикатор загрузки
            if (saveIndicator != null)
                saveIndicator.SetActive(true);

            // Делаем скриншот
            Texture2D screenshot = null;
            yield return StartCoroutine(TakeScreenshotCoroutine("autosave", tex => screenshot = tex));
            Debug.Log("Screenshot taken");
            // Создаем данные сохранения
            var saveData = CreateSaveData("autosave");

            // Добавляем скриншот если удалось сделать
            if (screenshot != null)
            {
                saveData.screenshotData = SaveScreenshotToBase64(screenshot);
            }

            // Сохраняем игру
            var result = SaveGameData(saveData, "autosave");
            result.LogResult();

            // Показываем результат
            ShowSaveIndicator();
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
                    screenshotData = saveData.screenshotData,
                    timestamp = saveData.timestamp,
                    gameVersion = saveData.gameVersion,
                    playerLevel = saveData.playerData.level,
                    playTime = "Unknown",
                    location = "Town"
                };
            }
            catch
            {
                return null;
            }
        }

        public void CreateManualSave(string saveName)
        {
            if (enableScreenshots)
            {
                StartCoroutine(CreateManualSaveWithScreenshot(saveName));
            }
            else
            {
                var formattedName = FormatManualSaveName(saveName);
                SaveGame(formattedName);
                ShowSaveIndicator();
            }
        }

        private string FormatManualSaveName(string saveName)
        {
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            return string.IsNullOrEmpty(saveName)
                ? $"manual_{timestamp}"
                : $"manual_{saveName}_{timestamp}";
        }

        private IEnumerator CreateManualSaveWithScreenshot(string saveName)
        {
            var formattedName = FormatManualSaveName(saveName);

            // Показываем индикатор загрузки
            if (saveIndicator != null)
                saveIndicator.SetActive(true);

            // Делаем скриншот
            Texture2D screenshot = null;
            yield return StartCoroutine(TakeScreenshotCoroutine(formattedName, tex => screenshot = tex));

            // Создаем данные сохранения
            var saveData = CreateSaveData(formattedName);

            // Добавляем скриншот если удалось сделать
            if (screenshot != null)
            {
                saveData.screenshotData = SaveScreenshotToBase64(screenshot);
            }

            // Сохраняем игру
            var result = SaveGameData(saveData, formattedName);
            result.LogResult();

            // Показываем результат
            ShowSaveIndicator();

            // Обновляем список сохранений
            LoadSaveList();
            CleanupOldSaves();
        }

        private SaveOperationResult SaveGameData(GameSaveData saveData, string saveName)
        {
            try
            {
                var filePath = GetSaveFilePath(saveName);

                // Валидация данных перед сохранением
                var validationResult = ValidateSaveData(saveData);
                if (!validationResult.Success)
                {
                    return SaveOperationResult.FromError($"Save data validation failed: {validationResult.Message}",
                        SaveErrorType.InvalidData);
                }

                // Создаем бэкап если файл уже существует
                if (backupSaves && File.Exists(filePath))
                {
                    CreateBackup(filePath);
                }

                // Добавляем контрольную сумму если включено
                if (enableChecksum)
                {
                    saveData.checksum = CalculateChecksum(saveData);
                }

                // Сериализуем данные
                var json = JsonUtility.ToJson(saveData, true);

                // Сохраняем в файл
                File.WriteAllText(filePath, json);

                var fileInfo = new FileInfo(filePath);
                return SaveOperationResult.FromSuccess($"Game saved successfully", filePath, fileInfo.Length);
            }
            catch (Exception e)
            {
                var errorType = SaveErrorHandler.GetErrorTypeFromException(e);
                return SaveOperationResult.FromError($"Save failed: {e.Message}", errorType, e);
            }
        }

        private SaveOperationResult LoadGameData(string saveName)
        {
            try
            {
                var filePath = GetSaveFilePath(saveName);

                if (!File.Exists(filePath))
                {
                    return SaveOperationResult.FromError($"Save file not found: {filePath}",
                        SaveErrorType.FileNotFound);
                }

                // Читаем файл
                var json = File.ReadAllText(filePath);

                // Десериализуем данные
                var saveData = JsonUtility.FromJson<GameSaveData>(json);

                // Проверяем контрольную сумму если включено
                if (enableChecksum && !string.IsNullOrEmpty(saveData.checksum))
                {
                    var currentChecksum = CalculateChecksum(saveData);
                    if (currentChecksum != saveData.checksum)
                    {
                        return SaveOperationResult.FromError("Save file checksum mismatch - data may be corrupted",
                            SaveErrorType.DataCorrupted);
                    }
                }

                // Валидация загруженных данных
                var validationResult = ValidateSaveData(saveData);
                if (!validationResult.Success)
                {
                    return SaveOperationResult.FromError(
                        $"Loaded save data validation failed: {validationResult.Message}",
                        SaveErrorType.InvalidData);
                }

                var fileInfo = new FileInfo(filePath);
                return SaveOperationResult.FromSuccess($"Game loaded successfully", filePath, fileInfo.Length);
            }
            catch (Exception e)
            {
                var errorType = SaveErrorHandler.GetErrorTypeFromException(e);
                return SaveOperationResult.FromError($"Load failed: {e.Message}", errorType, e);
            }
        }

        private SaveOperationResult ValidateSaveData(GameSaveData saveData)
        {
            if (saveData == null)
                return SaveOperationResult.FromError("Save data is null", SaveErrorType.InvalidData);

            if (string.IsNullOrEmpty(saveData.version))
                return SaveOperationResult.FromError("Save version is missing", SaveErrorType.InvalidData);

            if (saveData.playerData == null)
                return SaveOperationResult.FromError("Player data is missing", SaveErrorType.InvalidData);

            // Проверка версии игры
            if (saveData.gameVersion != gameVersion)
            {
                Debug.LogWarning($"Save version mismatch: {saveData.gameVersion} vs {gameVersion}");
                // Здесь можно добавить логику миграции версий
            }

            return SaveOperationResult.FromSuccess("Save data validation passed");
        }

        private string CalculateChecksum(GameSaveData saveData)
        {
            try
            {
                // Временно убираем checksum чтобы он не влиял на расчет
                var originalChecksum = saveData.checksum;
                saveData.checksum = string.Empty;

                var json = JsonUtility.ToJson(saveData);
                using var sha256 = SHA256.Create();
                var bytes = Encoding.UTF8.GetBytes(json);
                var hash = sha256.ComputeHash(bytes);

                // Восстанавливаем checksum
                saveData.checksum = originalChecksum;

                return Convert.ToBase64String(hash);
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to calculate checksum: {e.Message}");
                return string.Empty;
            }
        }

        private void CreateBackup(string originalFilePath)
        {
            try
            {
                var backupDir = Path.Combine(SaveFolder, "Backups");
                if (!Directory.Exists(backupDir))
                {
                    Directory.CreateDirectory(backupDir);
                }

                var fileName = Path.GetFileName(originalFilePath);
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var backupPath = Path.Combine(backupDir,
                    $"{Path.GetFileNameWithoutExtension(fileName)}_{timestamp}.backup");

                File.Copy(originalFilePath, backupPath);

                // Очистка старых бэкапов
                CleanupOldBackups(backupDir, fileName);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Failed to create backup: {e.Message}");
            }
        }

        private void CleanupOldBackups(string backupDir, string fileName)
        {
            try
            {
                var backupFiles = Directory
                    .GetFiles(backupDir, $"{Path.GetFileNameWithoutExtension(fileName)}_*.backup")
                    .OrderByDescending(f => new FileInfo(f).CreationTime)
                    .ToArray();

                for (var i = maxBackupCount; i < backupFiles.Length; i++)
                {
                    File.Delete(backupFiles[i]);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Failed to cleanup old backups: {e.Message}");
            }
        }

        public SaveOperationResult RestoreFromBackup(string saveName)
        {
            try
            {
                var backupDir = Path.Combine(SaveFolder, "Backups");
                if (!Directory.Exists(backupDir))
                {
                    return SaveOperationResult.FromError("Backup directory not found", SaveErrorType.FileNotFound);
                }

                var backupFiles = Directory.GetFiles(backupDir, $"{saveName}_*.backup")
                    .OrderByDescending(f => new FileInfo(f).CreationTime)
                    .ToArray();

                if (backupFiles.Length == 0)
                {
                    return SaveOperationResult.FromError($"No backups found for {saveName}",
                        SaveErrorType.FileNotFound);
                }

                var latestBackup = backupFiles[0];
                var originalFile = GetSaveFilePath(saveName);

                File.Copy(latestBackup, originalFile, true);

                return SaveOperationResult.FromSuccess(
                    $"Successfully restored from backup: {Path.GetFileName(latestBackup)}", originalFile);
            }
            catch (Exception e)
            {
                var errorType = SaveErrorHandler.GetErrorTypeFromException(e);
                return SaveOperationResult.FromError($"Restore failed: {e.Message}", errorType, e);
            }
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
            Debug.Log("Creating save data");
            return new GameSaveData
            {
                saveName = saveName,
                timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                gameVersion = gameVersion,

                playerData = CreatePlayerSaveData(),
                worldData = CreateWorldSaveData(),
                enemiesData = CreateEnemiesSaveData(),
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
            Debug.Log("Player position: " + playerController.transform.position + " rotation: " +
                      playerController.transform.eulerAngles + " health: " + playerHealth.currentHealth + " level: " +
                      player.playerLevel + " dialogue history: " + player.dialogueHistory + " dialogue flags: " +
                      player.dialogueFlags + " relationships: " + player.RelationshipsWithNpCs);

            var playerSaveData = new PlayerSaveData
            {
                playerID = player.playerID,
                playerName = player.playerName,
                position = playerController.transform.position,
                rotation = playerController.transform.eulerAngles,
                health = playerHealth.currentHealth,
                level = player.playerLevel,
                dialogueHistory = ConvertDialogueHistory(player.dialogueHistory),
                dialogueFlags = ConvertDialogueFlags(player.dialogueFlags),
                progression = new PlayerProgressionSaveData
                {
                    damageMultiplier = playerProgression.damageMultiplier,
                    fireRateMultiplier = playerProgression.fireRateMultiplier,
                    miningSpeedMultiplier = playerProgression.miningSpeedMultiplier,
                    inventoryCapacity = playerProgression.inventoryCapacity,
                    collectionRangeMultiplier = playerProgression.collectionRangeMultiplier
                }
            };

            // Преобразуем обычный Dictionary в IntIntDictionary
            playerSaveData.relationships.FromDictionary(player.RelationshipsWithNpCs);

            return playerSaveData;
        }

        private IntDialogueHistoryListDictionary ConvertDialogueHistory(
            Dictionary<int, List<PlayerData.DialogueHistory>> history)
        {
            var result = new IntDialogueHistoryListDictionary();
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

            if (npcManager != null)
            {
                foreach (var npcBehaviour in npcManager.GetAllNpcs())
                {
                    var npcInteraction = npcBehaviour.GetComponent<NpcInteraction>();
                    var reactiveTrigger = npcBehaviour.GetComponent<ReactiveDialogueTrigger>();

                    if (npcInteraction?.npcData != null)
                    {
                        var npcSaveData = new NpcSaveData
                        {
                            npcID = npcInteraction.npcData.npcID,
                            npcName = npcInteraction.npcData.npcName,
                            currentState = npcInteraction.npcData.currentState,
                            memories = ConvertMemories(npcInteraction.npcData.memories),
                            behaviourData = npcBehaviour.GetSaveData()
                        };

                        // Сохраняем данные реактивного диалога
                        if (reactiveTrigger != null)
                        {
                            var reactiveData = reactiveTrigger.GetSaveData();
                            npcSaveData.currentDialogueIndex = reactiveData.currentDialogueIndex;
                            npcSaveData.canCall = reactiveData.canCall;
                            npcSaveData.lastCallTime = reactiveData.lastCallTime;
                        }

                        npcSaveData.relationships.FromDictionary(npcInteraction.npcData.Relationships);
                        npcs.Add(npcSaveData);
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
                // Преобразуем обычный Dictionary в StringIntDictionary
                var buildingLevelsDict = new Dictionary<string, int>();
                var unlockedBuildingsDict = new Dictionary<string, bool>();

                foreach (var building in buildingManager.GetAllBuildings())
                {
                    buildingLevelsDict[building.GetBuildingId()] = building.GetCurrentLevel();
                }

                buildingData.buildingLevels.FromDictionary(buildingLevelsDict);
                buildingData.unlockedBuildings.FromDictionary(buildingManager.GetUnlockedBuildingsDictionary());
            }

            if (townHall != null)
            {
                buildingData.townHallLevel = townHall.GetCurrentLevel();
                buildingData.unlockedTechTier = townHall.GetUnlockedTechTier();
            }

            if (farmManager != null)
            {
                var farmPlotsDict = new Dictionary<string, FarmPlotSaveData>();
                foreach (var plot in farmManager.GetFarmPlots())
                {
                    farmPlotsDict[plot.Key] = new FarmPlotSaveData
                    {
                        plotId = plot.Value.plotId,
                        resourceType = plot.Value.resourceType,
                        productionRate = plot.Value.productionRate,
                        isActive = plot.Value.isActive,
                        timer = plot.Value.timer
                    };
                }

                buildingData.farmPlots.FromDictionary(farmPlotsDict);
            }

            return buildingData;
        }

        private Dictionary<int, List<PlayerData.DialogueHistory>> ConvertSaveDialogueHistory(
            IntDialogueHistoryListDictionary saveHistory)
        {
            var result = new Dictionary<int, List<PlayerData.DialogueHistory>>();
            if (saveHistory == null) return result;

            foreach (var entry in saveHistory.ToDictionary())
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
                var itemsDict = new Dictionary<string, int>();
                foreach (var slot in inventory.GetAllItems())
                {
                    itemsDict[slot.type.ToString()] = slot.count;
                }

                inventoryData.items.FromDictionary(itemsDict);
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
                questData.questProgress = new StringQuestProgressDictionary();

                foreach (var quest in questSystem.GetActiveQuests())
                {
                    var progressData = new QuestProgressSaveData
                    {
                        currentKills = quest.currentKills
                    };

                    // Преобразуем обычный Dictionary в StringIntDictionary
                    progressData.gatheredResources = new Data.Game.StringIntDictionary();
                    if (quest.currentResources != null)
                    {
                        progressData.gatheredResources.FromDictionary(quest.currentResources);
                    }

                    questData.questProgress[quest.questId] = progressData;
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

        private List<EnemySaveData> CreateEnemiesSaveData()
        {
            if (EnemySpawnManager.Instance != null)
            {
                return EnemySpawnManager.Instance.GetAllEnemiesSaveData();
            }

            return new List<EnemySaveData>();
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
                Debug.Log("Starting save data application...");

                InitializeSystems();

                // 1. Сначала применяем настройки и базовые данные
                ApplySettingsSaveData(saveData.settingsData);
                ApplyWorldSaveData(saveData.worldData);

                // 2. Затем применяем игровые данные
                ApplyPlayerSaveData(saveData.playerData);
                ApplyInventorySaveData(saveData.inventoryData);
                ApplyTechSaveData(saveData.techData);
                ApplyBuildingSaveData(saveData.buildingData);
                ApplyQuestSaveData(saveData.questData);

                // 3. Затем ресурсы
                ApplyCollectedResourcesData(saveData.collectedResourceIds);

                // 4. Затем NPC
                ApplyNpcsSaveData(saveData.npcsData);

                // 5. Убедимся, что EnemySpawnManager готов перед загрузкой врагов
                StartCoroutine(WaitForEnemySpawnManagerAndRestore(saveData.enemiesData));

                Debug.Log("Save data application process started successfully");
            }
            catch (Exception e)
            {
                Debug.LogError($"Error applying save data: {e.Message}");
            }
        }

        private IEnumerator WaitForEnemySpawnManagerAndRestore(List<EnemySaveData> enemiesData)
        {
            // Ждем пока EnemySpawnManager станет активным и включенным
            float timeout = 5f;
            float timer = 0f;

            while (EnemySpawnManager.Instance == null ||
                   !EnemySpawnManager.Instance.gameObject.activeInHierarchy ||
                   !EnemySpawnManager.Instance.enabled)
            {
                timer += Time.deltaTime;
                if (timer >= timeout)
                {
                    Debug.LogError("EnemySpawnManager not ready after " + timeout + " seconds!");
                    yield break;
                }

                yield return null;
            }

            // Даем дополнительное время для инициализации
            yield return new WaitForSeconds(0.2f);

            Debug.Log($"Starting enemy restoration for {enemiesData?.Count ?? 0} enemies");

            if (EnemySpawnManager.Instance != null)
            {
                EnemySpawnManager.Instance.RestoreAllEnemies(enemiesData);
            }
            else
            {
                Debug.LogError("EnemySpawnManager instance is null!");
            }
        }

        private void ApplyEnemiesSaveData(List<EnemySaveData> enemiesData)
        {
            if (EnemySpawnManager.Instance == null)
            {
                Debug.LogWarning("EnemySpawnManager not found, cannot restore enemies");
                return;
            }

            if (enemiesData == null || enemiesData.Count == 0)
            {
                Debug.Log("No enemy data to restore");
                return;
            }

            // Используем улучшенный метод массового восстановления
            EnemySpawnManager.Instance.RestoreAllEnemies(enemiesData);
            Debug.Log($"Enemy restore process started for {enemiesData.Count} enemies");
        }


        private void ApplyPlayerSaveData(PlayerSaveData data)
        {
            var player = PlayerData.Instance;
            var playerController = PlayerController.Instance;
            var playerHealth = PlayerHealth.Instance;
            var playerProgression = PlayerProgression.Instance;

            if (playerController != null)
            {
                // Отключаем CharacterController для изменения позиции
                var characterController = playerController.GetComponent<CharacterController>();
                if (characterController != null)
                {
                    characterController.enabled = false;
                }

                playerController.transform.position = data.position;
                playerController.transform.eulerAngles = data.rotation;

                // Включаем обратно
                if (characterController != null)
                {
                    characterController.enabled = true;
                }
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

                // Преобразуем IntIntDictionary в обычный Dictionary
                player.RelationshipsWithNpCs = data.relationships?.ToDictionary() ?? new Dictionary<int, int>();

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
                    // Восстанавливаем данные поведения
                    npcBehaviour.ApplySaveData(npcSaveData.behaviourData);

                    // Восстанавливаем реактивные диалоги
                    var reactiveTrigger = npcBehaviour.GetComponent<ReactiveDialogueTrigger>();
                    if (reactiveTrigger != null)
                    {
                        var reactiveData = new ReactiveDialogueSaveData
                        {
                            currentDialogueIndex = npcSaveData.currentDialogueIndex,
                            canCall = npcSaveData.canCall,
                            lastCallTime = npcSaveData.lastCallTime
                        };
                        reactiveTrigger.ApplySaveData(reactiveData);

                        Debug.Log($"Applied reactive data for NPC {npcSaveData.npcID}: " +
                                  $"canCall={npcSaveData.canCall}, index={npcSaveData.currentDialogueIndex}");
                    }
                }
                else
                {
                    Debug.LogWarning($"NPC with ID {npcSaveData.npcID} not found in scene");
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
                var buildingLevels = data.buildingLevels?.ToDictionary() ?? new Dictionary<string, int>();
                foreach (var buildingLevel in buildingLevels)
                {
                    var building = buildingManager.GetBuilding(buildingLevel.Key);
                    if (building != null)
                    {
                        building.SetLevel(buildingLevel.Value);
                    }
                }

                // Восстанавливаем разблокированные здания
                var unlockedBuildings = data.unlockedBuildings?.ToDictionary() ?? new Dictionary<string, bool>();
                buildingManager.ApplyUnlockedBuildings(unlockedBuildings);
            }

            if (townHall != null)
            {
                // Восстанавливаем уровень ратуши
                for (int i = 0; i < data.townHallLevel; i++)
                {
                    townHall.SetLevel(data.townHallLevel);
                }

                townHall.UnlockUpgradeTier(data.unlockedTechTier);
                townHall.UpdateVisualModel();
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
                var items = data.items?.ToDictionary() ?? new Dictionary<string, int>();
                foreach (var itemEntry in items)
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
                // Используем новый метод для немедленного применения
                ResourceManager.Instance.ApplyCollectedResourcesImmediately(
                    new HashSet<string>(collectedResourceIds)
                );
                Debug.Log($"Applied {collectedResourceIds?.Count ?? 0} collected resource IDs");
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
        public string screenshotData;
        public bool isCorrupted;
        public long fileSize;
        public string checksum;
    }
}