using System.Collections.Generic;
using Core;
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
        
        // Quest Data
        public QuestSaveData questData;
        
        // Settings
        public List<string> collectedResourceIds = new List<string>();
        public SettingsSaveData settingsData;
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
        
        // Relationships
        public Dictionary<int, int> relationships;
        
        // Dialogue History
        public Dictionary<int, List<DialogueHistorySaveData>> dialogueHistory;
        public List<DialogueFlagsSaveData> dialogueFlags;
        
        // Progression
        public PlayerProgressionSaveData progression;
    }

    [System.Serializable]
    public class DialogueHistorySaveData
    {
        public string dialogueTreeName;
        public string nodeID;
        public string selectedOption;
        public GameTimestamp timestamp;
        public List<string> flagsSet;
    }

    [System.Serializable]
    public class DialogueFlagsSaveData
    {
        public int npcID;
        public List<string> flags;
    }

    [System.Serializable]
    public class PlayerProgressionSaveData
    {
        public float damageMultiplier = 1f;
        public float fireRateMultiplier = 1f;
        public float miningSpeedMultiplier = 1f;
        public int inventoryCapacity = 50;
        public float collectionRangeMultiplier = 1f;
        public Dictionary<string, bool> unlockedTechs = new();
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
        public Vector3 position;
        public Vector3 rotation;
        public NpcState currentState;
        
        // Relationships
        public Dictionary<int, int> relationships;
        
        // Memories
        public List<MemorySaveData> memories;
        
        // Reactive Dialogue
        public int currentDialogueIndex;
        public bool canCall = true;
        public float lastCallTime;
        
        // Schedule
        public ActivitySaveData currentActivity;
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
        public Dictionary<string, int> buildingLevels = new();
        public Dictionary<string, bool> unlockedBuildings = new();
        public int townHallLevel;
        public int unlockedTechTier;
        
        // Farm plots
        public Dictionary<string, FarmPlotSaveData> farmPlots = new();
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
        public Dictionary<string, bool> unlockedNodes = new();
        public List<string> unlockedTrees = new();
    }

    [System.Serializable]
    public class InventorySaveData
    {
        public Dictionary<string, int> items = new();
        public int gold;
        public int capacity;
    }

    [System.Serializable]
    public class QuestSaveData
    {
        public List<string> activeQuests = new();
        public List<string> completedQuests = new();
        public Dictionary<string, QuestProgressSaveData> questProgress = new();
    }

    [System.Serializable]
    public class QuestProgressSaveData
    {
        public int currentKills;
        public Dictionary<string, int> gatheredResources;
    }

    [System.Serializable]
    public class SettingsSaveData
    {
        public string language = "ru-RU";
        public float masterVolume = 1.0f;
        public float musicVolume = 1.0f;
        public float sfxVolume = 1.0f;
        public bool fullscreen = true;
        public int resolutionWidth = 1920;
        public int resolutionHeight = 1080;
    }

    [System.Serializable]
    public class CollectedResourceData
    {
        public string resourceId;
        public Vector3 position;
        public ItemType itemType;
    }
}