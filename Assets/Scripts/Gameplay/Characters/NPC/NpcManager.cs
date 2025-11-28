using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Gameplay.Characters.NPC
{
    public class NpcManager : MonoBehaviour
    {
        public static NpcManager Instance;
    
        [SerializeField] private List<NpcBehaviour> allNpcs = new List<NpcBehaviour>();
    
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
            // Автоматически находим всех NPC при старте
            var allNpcsInScene = FindObjectsOfType<NpcBehaviour>();
            foreach (var npc in allNpcsInScene)
            {
                RegisterNpc(npc);
            }
        }
    
        public void RegisterNpc(NpcBehaviour npc)
        {
            if (!allNpcs.Contains(npc))
            {
                allNpcs.Add(npc);
            }
        }
    
        public void UnregisterNpc(NpcBehaviour npc)
        {
            if (allNpcs.Contains(npc))
            {
                allNpcs.Remove(npc);
            }
        }
    
        public NpcBehaviour GetNpcByID(int npcID)
        {
            return allNpcs.Find(npc => npc.npcDataConfig != null && npc.npcDataConfig.npcID == npcID);
        }
    
        public List<NpcBehaviour> GetAllNpcs()
        {
            return new List<NpcBehaviour>(allNpcs);
        }
    
        public List<NpcBehaviour> GetNpcsInRadius(Vector3 position, float radius)
        {
            return allNpcs.Where(npc => npc != null && Vector3.Distance(npc.transform.position, position) <= radius).ToList();
        }
    }
}