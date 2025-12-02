using System;
using System.Collections.Generic;
using Core;
using Gameplay.Characters.NPC;
using UnityEngine;

namespace Data.Game
{
    [System.Serializable]
    public class GameSaveData
    {
        public string version = "1.0";
        public string saveName;
        public string timestamp;
        public string gameVersion;
        
        // Screenshot data for manual saves
        public string screenshotData; // Base64 encoded thumbnail
        
        // Player Data
        public PlayerSaveData playerData;
        
        // World Data
        public WorldSaveData worldData;
        
        // NPC Data
        public List<NpcSaveData> npcsData;
        
        // Building Data
        public BuildingSaveData buildingData;
        
        // Tech Data
        public TechSaveData techData;
        
        // Inventory Data
        public InventorySaveData inventoryData;
        
        // Enemy Data
        public List<EnemySaveData> enemiesData;
        
        // Quest Data
        public QuestSaveData questData;
        
        // Settings
        public List<string> collectedResourceIds = new List<string>();
        public SettingsSaveData settingsData;

        // Validation and metadata
        public string checksum;
        public bool isValid = true;
    }

    [System.Serializable]
    public class PlayerSaveData
    {
        public int playerID;
        public string playerName;
        public Vector3 position;
        public Vector3 rotation;
        public float health;
        public int level;
        
        // Relationships - using SerializableDictionary
        public IntIntDictionary relationships = new IntIntDictionary();
        
        // Dialogue History
        public IntDialogueHistoryListDictionary dialogueHistory = new IntDialogueHistoryListDictionary();
        public List<DialogueFlagsSaveData> dialogueFlags = new List<DialogueFlagsSaveData>();
        
        // Progression
        public PlayerProgressionSaveData progression = new PlayerProgressionSaveData();
    }
    
    [System.Serializable]
    public class EnemySaveData
    {
        public string enemyId;
        public string enemyType;
        public Vector3 position;
        public Vector3 rotation;
        public float health;
        public EnemyStateSaveData stateData;
    }

    [System.Serializable]
    public class EnemyStateSaveData
    {
        public bool isDead;
        public bool isEmerging;
        public float lastAttackTime;
        public Vector3 targetPosition;
    }
    
    [System.Serializable]
    public class EnemySpawnManagerSaveData
    {
        public List<string> spawnedEnemyIds = new List<string>();
    }

    [System.Serializable]
    public class DialogueHistorySaveData
    {
        public string dialogueTreeName;
        public string nodeID;
        public string selectedOption;
        public GameTimestamp timestamp;
        public List<string> flagsSet = new List<string>();
    }

    [System.Serializable]
    public class DialogueFlagsSaveData
    {
        public int npcID;
        public List<string> flags = new List<string>();
    }

    [System.Serializable]
    public class PlayerProgressionSaveData
    {
        public float damageMultiplier = 1f;
        public float fireRateMultiplier = 1f;
        public float miningSpeedMultiplier = 1f;
        public int inventoryCapacity = 50;
        public float collectionRangeMultiplier = 1f;
        public StringBoolDictionary unlockedTechs = new StringBoolDictionary();
    }

    [System.Serializable]
    public class WorldSaveData
    {
        public int currentDay;
        public int currentHour;
        public int currentMinute;
        public float timeTimer;
    }

    [System.Serializable]
    public class NpcSaveData
    {
        public int npcID;
        public string npcName;
        public NpcState currentState;
    
        // Relationships
        public IntIntDictionary relationships = new IntIntDictionary();
    
        // Memories
        public List<MemorySaveData> memories = new List<MemorySaveData>();
    
        // Reactive Dialogue
        public int currentDialogueIndex;
        public bool canCall = true;
        public float lastCallTime;
    
        // Behaviour Data
        public NpcBehaviourSaveData behaviourData;
    }

    [System.Serializable]
    public class MemorySaveData
    {
        public string memoryText;
        public int relationshipImpact;
        public string sourceNpc;
        public GameTimestamp timestamp;
    }

    [System.Serializable]
    public class ActivitySaveData
    {
        public ActivityType type;
        public Vector3 location;
        public string targetNpc;
        public float duration;
        public float remainingTime;
    }

    [System.Serializable]
    public class BuildingSaveData
    {
        public StringIntDictionary buildingLevels = new StringIntDictionary();
        public StringBoolDictionary unlockedBuildings = new StringBoolDictionary();
        public int townHallLevel;
        public int unlockedTechTier;
        
        // Farm plots
        public StringFarmPlotDictionary farmPlots = new StringFarmPlotDictionary();
    }

    [System.Serializable]
    public class FarmPlotSaveData
    {
        public string plotId;
        public ItemType resourceType;
        public float productionRate;
        public bool isActive;
        public float timer;
    }

    [System.Serializable]
    public class TechSaveData
    {
        public StringBoolDictionary unlockedNodes = new StringBoolDictionary();
        public List<string> unlockedTrees = new List<string>();
    }

    [System.Serializable]
    public class InventorySaveData
    {
        public StringIntDictionary items = new StringIntDictionary();
        public int gold;
        public int capacity;
    }

    [System.Serializable]
    public class QuestSaveData
    {
        public List<string> activeQuests = new List<string>();
        public List<string> completedQuests = new List<string>();
        public StringQuestProgressDictionary questProgress = new StringQuestProgressDictionary();
    }

    [System.Serializable]
    public class QuestProgressSaveData
    {
        public int currentKills;
        public StringIntDictionary gatheredResources = new StringIntDictionary();
    }

    [System.Serializable]
    public class SettingsSaveData
    {
        // Аудио
        public string language = "ru-RU";
        public float masterVolume = 1.0f;
        public float musicVolume = 0.8f;
        public float sfxVolume = 1.0f;
    
        // Графика
        public bool fullscreen = true;
        public int resolutionWidth = 1920;
        public int resolutionHeight = 1080;
        public int qualityLevel = 2; // Среднее качество
        public bool vsyncEnabled = true;
        public int textureQuality = 0;
        public int shadowQuality = 2;
        public int antiAliasing = 2;
    
        // Управление
        public float mouseSensitivity = 300f;
        public bool invertMouseY = false;
    
        // Сохранения
        public float autoSaveInterval = 300f;
        public bool enableScreenshots = true;
        public int screenshotQuality = 75;
        public bool enableChecksum = true;
        public bool backupSaves = true;
    }

    // Specific dictionary types for serialization
    [Serializable] public class IntIntDictionary : SerializableDictionary<int, int> { }
    [Serializable] public class IntBoolDictionary : SerializableDictionary<int, bool> { }
    [Serializable] public class IntStringDictionary : SerializableDictionary<int, string> { }
    [Serializable] public class StringIntDictionary : SerializableDictionary<string, int> { }
    [Serializable] public class StringBoolDictionary : SerializableDictionary<string, bool> { }
    [Serializable] public class StringStringDictionary : SerializableDictionary<string, string> { }
    [Serializable] public class IntDialogueHistoryListDictionary : SerializableDictionary<int, List<DialogueHistorySaveData>> { }
    [Serializable] public class StringFarmPlotDictionary : SerializableDictionary<string, FarmPlotSaveData> { }
    [Serializable] public class StringQuestProgressDictionary : SerializableDictionary<string, QuestProgressSaveData> { }
}