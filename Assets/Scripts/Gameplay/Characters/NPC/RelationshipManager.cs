using System.Collections.Generic;
using System.Linq;
using Core;
using Data.Game;
using Data.NPC;
using Gameplay.Dialogue;
using Gameplay.Systems;
using UnityEngine;

namespace Gameplay.Characters.NPC
{
    public class RelationshipManager : MonoBehaviour
    {
        public static RelationshipManager Instance;
    
        private Dictionary<int, NpcData> _allNpcs = new();
        private WorldTime _worldTime;
    
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
        
            _worldTime = FindObjectOfType<WorldTime>();
        }
    
        public void RegisterNpc(NpcData npcData)
        {
            _allNpcs.TryAdd(npcData.npcID, npcData);
        }
    
        public void ModifyRelationship(int npcID, int targetNpcId, int change)
        {
            if (!_allNpcs.TryGetValue(npcID, out var npc))
            {
                Debug.LogError($"NPC с ID {npcID} не найден в RelationshipManager!");
                return;
            }

            var currentRelationship = npc.GetRelationship(targetNpcId);
            var newRelationship = Mathf.Clamp(currentRelationship + change, -100, 100);
    
            Debug.Log($"   Изменение отношений:");
            Debug.Log($"   NPC: {npc.npcName} (ID: {npcID})");
            Debug.Log($"   Цель: ID {targetNpcId}");
            Debug.Log($"   Текущие: {currentRelationship} → Новые: {newRelationship} (изменение: {change})");
    
            npc.SetRelationship(targetNpcId, newRelationship);
    
            AddMemory(npcID, $"Изменение отношений с игроком: {change}", change);
        }

        public void AddMemory(int npcID, string memory, int impact)
        {
            if (!_allNpcs.ContainsKey(npcID)) 
            {
                Debug.LogError($" NPC с ID {npcID} не найден при добавлении памяти!");
                return;
            }
    
            _allNpcs[npcID].AddMemory(memory, impact, "System", _worldTime.GetCurrentTimestamp());
            Debug.Log($"  Добавлена память для NPC {npcID}: '{memory}' (влияние: {impact})");
        }
    
        public void AddMemory(int npcID, string memory, int impact, string source)
        {
            if (!_allNpcs.ContainsKey(npcID)) return;
        
            _allNpcs[npcID].AddMemory(memory, impact, source, _worldTime.GetCurrentTimestamp());
        }
    
        public string GetNpcName(int npcID)
        {
            return _allNpcs.TryGetValue(npcID, out var npc) ? npc.npcName : "Unknown NPC";
        }
    
        public int CalculateRelationshipImpact(NpcData npc, DialogueOption option, PlayerData player)
        {
            var baseImpact = option.relationshipChange;

            if (npc.personality == null || option.personalityPreference == null) return baseImpact;
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

            if (preference.preferredAmbition < 0) return baseImpact;
            if (npc.personality.ambition >= preference.preferredAmbition)
                baseImpact += 2;
            else
                baseImpact -= 2;

            return baseImpact;
        }
    
        public NpcData GetNpcData(int npcID)
        {
            return _allNpcs.GetValueOrDefault(npcID);
        }
    
        public List<NpcData> GetAllNpCs()
        {
            return new List<NpcData>(_allNpcs.Values);
        }
    
        public int GetRelationshipBetween(int npc1ID, int npc2ID)
        {
            return _allNpcs.ContainsKey(npc1ID) ? _allNpcs[npc1ID].GetRelationship(npc2ID) : 0;
        }
    
        public void DebugAllRelationships()
        {
            Debug.Log("=== ВСЕ ОТНОШЕНИЯ ===");
            foreach (var npcEntry in _allNpcs)
            {
                var npc = npcEntry.Value;
                var relationshipWithPlayer = npc.GetRelationship(PlayerData.Instance.playerID);
                Debug.Log($"{npc.npcName} (ID: {npc.npcID}): {relationshipWithPlayer}/100");
            
                foreach (var relationship in npc.Relationships)
                {
                    if (!_allNpcs.TryGetValue(relationship.Key, out var allNpc)) continue;
                    var otherNpcName = allNpc.npcName;
                    Debug.Log($"   -> {otherNpcName}: {relationship.Value}/100");
                }
            }
            Debug.Log("====================");
        }
    
        public void DebugNpcRelationships(int npcID)
        {
            if (!_allNpcs.TryGetValue(npcID, out var npc))
            {
                Debug.LogError($"NPC с ID {npcID} не найден");
                return;
            }

            Debug.Log($"=== ОТНОШЕНИЯ {npc.npcName.ToUpper()} ===");
        
            var playerRelationship = npc.GetRelationship(PlayerData.Instance.playerID);
            Debug.Log($"Игрок: {playerRelationship}/100");
        
            foreach (var relationship in npc.Relationships.Where(relationship => relationship.Key != PlayerData.Instance.playerID))
            {
                if (!_allNpcs.TryGetValue(relationship.Key, out var allNpc)) continue;
                var otherNpcName = allNpc.npcName;
                Debug.Log($"{otherNpcName}: {relationship.Value}/100");
            }

            if (npc.memories is { Count: > 0 })
            {
                Debug.Log("Последние воспоминания:");
                for (var i = npc.memories.Count - 1; i >= Mathf.Max(0, npc.memories.Count - 3); i--)
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
        
            foreach (var npcEntry in _allNpcs)
            {
                var npc = npcEntry.Value;
                var relationship = playerData.GetRelationshipWithNpc(npc.npcID);
                Debug.Log($"{npc.npcName}: {relationship}/100");
            }
            Debug.Log("====================");
        }

        public int GetRelationshipWithPlayer(int npcID)
        {
            return !_allNpcs.ContainsKey(npcID) ? 0 : _allNpcs[npcID].GetRelationship(PlayerData.Instance.playerID);
        }

        public string GetNpcNameByID(int npcID)
        {
            return _allNpcs.TryGetValue(npcID, out var npc) ? npc.npcName : "Unknown NPC";
        }
    }
}