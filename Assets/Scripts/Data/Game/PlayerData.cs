using System.Collections.Generic;
using System.Linq;
using Gameplay.Characters.NPC;
using Gameplay.Systems;
using UnityEngine;

namespace Data.Game
{
    public class PlayerData : MonoBehaviour
    {
        public static PlayerData Instance;
    
        [Header("Player Info")]
        public int playerID = 1;
        public string playerName = "Player";
    
        [Header("Relationships")]
        public Dictionary<int, int> RelationshipsWithNpCs = new();
    
        [Header("Inventory & Resources")]
        public Dictionary<string, int> Inventory;
        public int gold = 100;

        [Header("Quests")] public List<string> activeQuests = new();
        public List<string> completedQuests = new();
    
        [Header("Dialogue History")]
        public Dictionary<int, List<DialogueHistory>> dialogueHistory = new();
    
        [Header("Dialogue System")]
        public List<DialogueFlags> dialogueFlags = new();
        public int playerLevel = 1;
    
        [System.Serializable]
        public class DialogueHistory
        {
            public string dialogueTreeName;
            public string nodeID;
            public string selectedOption;
            public GameTimestamp timestamp;
            public List<string> flagsSet;
    
            public DialogueHistory(string tree, string node, string option, GameTimestamp time)
            {
                dialogueTreeName = tree;
                nodeID = node;
                selectedOption = option;
                timestamp = time;
                flagsSet = new List<string>();
            }
        }

        [System.Serializable]
        public class DialogueFlags
        {
            public int npcID;
            public List<string> flags;
    
            public DialogueFlags(int id)
            {
                npcID = id;
                flags = new List<string>();
            }
        }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        
            InitializeDefaultRelationships();
        }
    
        public void SetDialogueFlag(int npcID, string flag)
        {
            var npcFlags = dialogueFlags.Find(f => f.npcID == npcID);
            if (npcFlags == null)
            {
                npcFlags = new DialogueFlags(npcID);
                dialogueFlags.Add(npcFlags);
            }
    
            if (!npcFlags.flags.Contains(flag))
            {
                npcFlags.flags.Add(flag);
            }
        }

        public bool HasDialogueFlag(int npcID, string flag)
        {
            var npcFlags = dialogueFlags.Find(f => f.npcID == npcID);
            return npcFlags?.flags.Contains(flag) == true;
        }
    
        public void RecordDialogueChoice(int npcID, string treeName, string nodeID, string option, string[] flags = null)
        {
            AddDialogueHistory(npcID, treeName, nodeID, option);

            if (flags == null) return;
            if (dialogueHistory.ContainsKey(npcID) && dialogueHistory[npcID].Count > 0)
            {
                dialogueHistory[npcID][dialogueHistory[npcID].Count - 1].flagsSet.AddRange(flags);
            }
        }

        private void AddDialogueHistory(int npcID, string treeName, string nodeID, string selectedOption)
        {
            if (!dialogueHistory.ContainsKey(npcID))
                dialogueHistory[npcID] = new List<DialogueHistory>();
            
            var history = new DialogueHistory(treeName, nodeID, selectedOption, WorldTime.Instance.GetCurrentTimestamp());
            dialogueHistory[npcID].Add(history);
        
            if (dialogueHistory[npcID].Count > 50)
                dialogueHistory[npcID].RemoveAt(0);
        }
    
        public bool HasFlagInDialogue(int npcID, string flag)
        {
            return dialogueHistory.TryGetValue(npcID, out var value) && value.Any(history => history.flagsSet.Contains(flag));
        }
    
        public bool WasOptionSelected(int npcID, string optionText)
        {
            return dialogueHistory.TryGetValue(npcID, out var value) && value.Any(history => history.selectedOption.Contains(optionText));
        }
    
        public int GetDialogueCountWithNpc(int npcID)
        {
            return dialogueHistory.TryGetValue(npcID, out var value) ? value.Count : 0;
        }
    
    
        private void InitializeDefaultRelationships()
        {
            RelationshipsWithNpCs[1] = 0;
            RelationshipsWithNpCs[2] = 0;
        }
    
        public void ModifyRelationship(int npcID, int change)
        {
            RelationshipsWithNpCs.TryAdd(npcID, 0);

            RelationshipsWithNpCs[npcID] = Mathf.Clamp(RelationshipsWithNpCs[npcID] + change, -100, 100);
        }
    
        public int GetRelationshipWithNpc(int npcID)
        {
            return RelationshipsWithNpCs.GetValueOrDefault(npcID, 0);
        }

        private bool HasResource(string resource, int amount = 1)
        {
            return Inventory.ContainsKey(resource) && Inventory[resource] >= amount;
        }
    
        public void AddResource(string resource, int amount)
        {
            Inventory.TryAdd(resource, 0);
            Inventory[resource] += amount;
        }
    
        public bool SpendResource(string resource, int amount)
        {
            if (!HasResource(resource, amount)) return false;
            Inventory[resource] -= amount;
            return true;
        }
    
        public List<int> GetKnownNpcs()
        {
            return new List<int>(RelationshipsWithNpCs.Keys);
        }

        public void DebugPlayerRelationships()
        {
            Debug.Log("=== ОТНОШЕНИЯ ИГРОКА ===");
            foreach (var relationship in RelationshipsWithNpCs)
            {
                var npcName = RelationshipManager.Instance?.GetNpcNameByID(relationship.Key) ?? $"NPC_{relationship.Key}";
                Debug.Log($"{npcName}: {relationship.Value}/100");
            }
        }
    }
}