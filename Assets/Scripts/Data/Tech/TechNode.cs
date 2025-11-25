using System.Collections.Generic;
using Data.Game;
using UnityEngine;

namespace Data.Tech
{
    [System.Serializable]
    public class TechNode
    {
        public string nodeId;
        public string nodeName;
        public string description;
        public int tier;
        public List<ResourceCost> unlockCost;
        public List<string> prerequisiteNodes; // IDs узлов, которые должны быть открыты до этого
        public bool isUnlocked;
        public TechEffect[] effects;
    
        [Header("UI Settings")]
        public Vector2 graphPosition;
        public Sprite icon;
    }
}