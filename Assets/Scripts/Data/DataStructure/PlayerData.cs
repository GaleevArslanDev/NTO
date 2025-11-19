using System.Collections.Generic;
using UnityEngine;

public class PlayerData : MonoBehaviour
{
    public static PlayerData Instance;
    
    [Header("Player Info")]
    public int playerID = 1;
    public string playerName = "Player";
    
    [Header("Relationships")]
    public Dictionary<int, int> relationshipsWithNPCs = new Dictionary<int, int>();
    
    [Header("Inventory & Resources")]
    public Dictionary<string, int> inventory = new Dictionary<string, int>();
    public int gold = 100;
    
    [Header("Quests")]
    public List<string> activeQuests = new List<string>();
    public List<string> completedQuests = new List<string>();
    
    [Header("Dialogue History")]
    public Dictionary<int, List<DialogueHistory>> dialogueHistory = new Dictionary<int, List<DialogueHistory>>();
    
    [Header("Dialogue System")]
    public List<DialogueFlags> dialogueFlags = new List<DialogueFlags>();
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
    
        if (flags != null)
        {
            if (dialogueHistory.ContainsKey(npcID) && dialogueHistory[npcID].Count > 0)
            {
                dialogueHistory[npcID][dialogueHistory[npcID].Count - 1].flagsSet.AddRange(flags);
            }
        }
    }
    
    public void AddDialogueHistory(int npcID, string treeName, string nodeID, string selectedOption)
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
        if (!dialogueHistory.ContainsKey(npcID)) return false;
        
        foreach (var history in dialogueHistory[npcID])
        {
            if (history.flagsSet.Contains(flag))
                return true;
        }
        return false;
    }
    
    public bool WasOptionSelected(int npcID, string optionText)
    {
        if (!dialogueHistory.ContainsKey(npcID)) return false;
        
        foreach (var history in dialogueHistory[npcID])
        {
            if (history.selectedOption.Contains(optionText))
                return true;
        }
        return false;
    }
    
    public int GetDialogueCountWithNPC(int npcID)
    {
        return dialogueHistory.ContainsKey(npcID) ? dialogueHistory[npcID].Count : 0;
    }
    
    
    private void InitializeDefaultRelationships()
    {
        relationshipsWithNPCs[1] = 0;
        relationshipsWithNPCs[2] = 0;
    }
    
    public void ModifyRelationship(int npcID, int change)
    {
        if (!relationshipsWithNPCs.ContainsKey(npcID))
            relationshipsWithNPCs[npcID] = 0;
            
        relationshipsWithNPCs[npcID] = Mathf.Clamp(relationshipsWithNPCs[npcID] + change, -100, 100);
    }
    
    public int GetRelationshipWithNPC(int npcID)
    {
        return relationshipsWithNPCs.ContainsKey(npcID) ? relationshipsWithNPCs[npcID] : 0;
    }
    
    public bool HasResource(string resource, int amount = 1)
    {
        return inventory.ContainsKey(resource) && inventory[resource] >= amount;
    }
    
    public void AddResource(string resource, int amount)
    {
        if (!inventory.ContainsKey(resource))
            inventory[resource] = 0;
        inventory[resource] += amount;
    }
    
    public bool SpendResource(string resource, int amount)
    {
        if (HasResource(resource, amount))
        {
            inventory[resource] -= amount;
            return true;
        }
        return false;
    }
    
    public List<int> GetKnownNPCs()
    {
        return new List<int>(relationshipsWithNPCs.Keys);
    }

    public void DebugPlayerRelationships()
    {
        Debug.Log("=== ОТНОШЕНИЯ ИГРОКА ===");
        foreach (var relationship in relationshipsWithNPCs)
        {
            string npcName = RelationshipManager.Instance?.GetNPCNameByID(relationship.Key) ?? $"NPC_{relationship.Key}";
            Debug.Log($"{npcName}: {relationship.Value}/100");
        }
    }
}