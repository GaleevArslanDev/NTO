using UnityEngine;

namespace Data.NPC
{
    [CreateAssetMenu(menuName = "NPC/NPC Configuration")]
    public class NpcConfig : ScriptableObject
    {
        public string npcName;
        public int npcID;
    
        [System.Serializable]
        public class StartingRelationship
        {
            public int targetNpcId;
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
}