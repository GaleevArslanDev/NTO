using UnityEngine;

[CreateAssetMenu(menuName = "NPC/NPC Configuration")]
public class NPCConfig : ScriptableObject
{
    public string npcName;
    public int npcID;
    
    [System.Serializable]
    public class StartingRelationship
    {
        public int targetNPCID;
        public int relationshipValue;
    }
    
    [System.Serializable]
    public class StartingMemory
    {
        public string memoryText;
        public int impact;
        public string source;
    }
    
    public StartingRelationship[] startingRelationships;
    public StartingMemory[] startingMemories;
}