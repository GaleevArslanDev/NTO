using System.Collections.Generic;
using UnityEngine;

public class NPCManager : MonoBehaviour
{
    public static NPCManager Instance;
    
    [SerializeField] private List<NPCBehaviour> allNPCs = new List<NPCBehaviour>();
    
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
    
    public void RegisterNPC(NPCBehaviour npc)
    {
        if (!allNPCs.Contains(npc))
        {
            allNPCs.Add(npc);
        }
    }
    
    public void UnregisterNPC(NPCBehaviour npc)
    {
        if (allNPCs.Contains(npc))
        {
            allNPCs.Remove(npc);
        }
    }
    
    public NPCBehaviour GetNPCByID(int npcID)
    {
        return allNPCs.Find(npc => npc.npcDataConfig != null && npc.npcDataConfig.npcID == npcID);
    }
    
    public List<NPCBehaviour> GetAllNPCs()
    {
        return new List<NPCBehaviour>(allNPCs);
    }
    
    public List<NPCBehaviour> GetNPCsInRadius(Vector3 position, float radius)
    {
        var result = new List<NPCBehaviour>();
        
        foreach (var npc in allNPCs)
        {
            if (npc != null && Vector3.Distance(npc.transform.position, position) <= radius)
            {
                result.Add(npc);
            }
        }
        
        return result;
    }
}