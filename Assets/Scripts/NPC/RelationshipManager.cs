using System.Collections.Generic;
using UnityEngine;

public class RelationshipManager : MonoBehaviour
{
    public static RelationshipManager Instance;
    
    private Dictionary<int, NPCData> allNPCs = new Dictionary<int, NPCData>();
    private WorldTime worldTime;
    
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
        
        worldTime = FindObjectOfType<WorldTime>();
    }
    
    public void RegisterNPC(NPCData npcData)
    {
        if (!allNPCs.ContainsKey(npcData.npcID))
        {
            allNPCs.Add(npcData.npcID, npcData);
        }
    }
    
    public void ModifyRelationship(int npcID, int targetNPCID, int change)
    {
        if (!allNPCs.ContainsKey(npcID))
        {
            Debug.LogError($"❌ NPC с ID {npcID} не найден в RelationshipManager!");
            return;
        }
    
        var npc = allNPCs[npcID];
        int currentRelationship = npc.GetRelationship(targetNPCID);
        int newRelationship = Mathf.Clamp(currentRelationship + change, -100, 100);
    
        Debug.Log($"   Изменение отношений:");
        Debug.Log($"   NPC: {npc.npcName} (ID: {npcID})");
        Debug.Log($"   Цель: ID {targetNPCID}");
        Debug.Log($"   Текущие: {currentRelationship} → Новые: {newRelationship} (изменение: {change})");
    
        npc.SetRelationship(targetNPCID, newRelationship);
    
        AddMemory(npcID, $"Изменение отношений с игроком: {change}", change);
    }

    public void AddMemory(int npcID, string memory, int impact)
    {
        if (!allNPCs.ContainsKey(npcID)) 
        {
            Debug.LogError($" NPC с ID {npcID} не найден при добавлении памяти!");
            return;
        }
    
        allNPCs[npcID].AddMemory(memory, impact, "System", worldTime.GetCurrentTimestamp());
        Debug.Log($"  Добавлена память для NPC {npcID}: '{memory}' (влияние: {impact})");
    }
    
    public void AddMemory(int npcID, string memory, int impact, string source)
    {
        if (!allNPCs.ContainsKey(npcID)) return;
        
        allNPCs[npcID].AddMemory(memory, impact, source, worldTime.GetCurrentTimestamp());
    }
    
    public string GetNPCName(int npcID)
    {
        return allNPCs.ContainsKey(npcID) ? allNPCs[npcID].npcName : "Unknown NPC";
    }
    
    public int CalculateRelationshipImpact(NPCData npc, DialogueOption option, PlayerData player)
    {
        int baseImpact = option.relationshipChange;
        
        if (npc.personality != null && option.personalityPreference != null)
        {
            var preference = option.personalityPreference;
            
            if (preference.preferredTrait != Trait.None && npc.personality.HasTrait(preference.preferredTrait))
                baseImpact += 5;
                
            if (preference.dislikedTrait != Trait.None && npc.personality.HasTrait(preference.dislikedTrait))
                baseImpact -= 5;
                
            if (preference.preferredOpenness >= 0)
            {
                if (npc.personality.openness >= preference.preferredOpenness)
                    baseImpact += 2;
                else
                    baseImpact -= 2;
            }
            
            if (preference.preferredFriendliness >= 0)
            {
                if (npc.personality.friendliness >= preference.preferredFriendliness)
                    baseImpact += 2;
                else
                    baseImpact -= 2;
            }
            
            if (preference.preferredAmbition >= 0)
            {
                if (npc.personality.ambition >= preference.preferredAmbition)
                    baseImpact += 2;
                else
                    baseImpact -= 2;
            }
        }
            
        return baseImpact;
    }
    
    public NPCData GetNPCData(int npcID)
    {
        return allNPCs.ContainsKey(npcID) ? allNPCs[npcID] : null;
    }
    
    public List<NPCData> GetAllNPCs()
    {
        return new List<NPCData>(allNPCs.Values);
    }
    
    public int GetRelationshipBetween(int npc1ID, int npc2ID)
    {
        if (allNPCs.ContainsKey(npc1ID))
        {
            return allNPCs[npc1ID].GetRelationship(npc2ID);
        }
        return 0;
    }
    
    public void DebugAllRelationships()
    {
        Debug.Log("=== ВСЕ ОТНОШЕНИЯ ===");
        foreach (var npcEntry in allNPCs)
        {
            var npc = npcEntry.Value;
            int relationshipWithPlayer = npc.GetRelationship(PlayerData.Instance.playerID);
            Debug.Log($"{npc.npcName} (ID: {npc.npcID}): {relationshipWithPlayer}/100");
            
            foreach (var relationship in npc.relationships)
            {
                if (allNPCs.ContainsKey(relationship.Key))
                {
                    string otherNPCName = allNPCs[relationship.Key].npcName;
                    Debug.Log($"   -> {otherNPCName}: {relationship.Value}/100");
                }
            }
        }
        Debug.Log("====================");
    }
    
    public void DebugNPCRelationships(int npcID)
    {
        if (!allNPCs.ContainsKey(npcID))
        {
            Debug.LogError($"NPC с ID {npcID} не найден");
            return;
        }

        var npc = allNPCs[npcID];
        Debug.Log($"=== ОТНОШЕНИЯ {npc.npcName.ToUpper()} ===");
        
        int playerRelationship = npc.GetRelationship(PlayerData.Instance.playerID);
        Debug.Log($"Игрок: {playerRelationship}/100");
        
        foreach (var relationship in npc.relationships)
        {
            if (relationship.Key == PlayerData.Instance.playerID) continue;
            
            if (allNPCs.ContainsKey(relationship.Key))
            {
                string otherNPCName = allNPCs[relationship.Key].npcName;
                Debug.Log($"{otherNPCName}: {relationship.Value}/100");
            }
        }

        if (npc.memories != null && npc.memories.Count > 0)
        {
            Debug.Log("Последние воспоминания:");
            for (int i = npc.memories.Count - 1; i >= Mathf.Max(0, npc.memories.Count - 3); i--)
            {
                var memory = npc.memories[i];
                Debug.Log($"- {memory.memoryText} ({memory.relationshipImpact}) - {memory.timestamp.day}d {memory.timestamp.hour}h");
            }
        }
        Debug.Log("====================");
    }

    public void DebugPlayerRelationships()
    {
        var playerData = PlayerData.Instance;
        Debug.Log("=== ОТНОШЕНИЯ ИГРОКА ===");
        
        foreach (var npcEntry in allNPCs)
        {
            var npc = npcEntry.Value;
            int relationship = playerData.GetRelationshipWithNPC(npc.npcID);
            Debug.Log($"{npc.npcName}: {relationship}/100");
        }
        Debug.Log("====================");
    }

    public int GetRelationshipWithPlayer(int npcID)
    {
        if (!allNPCs.ContainsKey(npcID)) return 0;
        return allNPCs[npcID].GetRelationship(PlayerData.Instance.playerID);
    }

    public string GetNPCNameByID(int npcID)
    {
        return allNPCs.ContainsKey(npcID) ? allNPCs[npcID].npcName : "Unknown NPC";
    }
}