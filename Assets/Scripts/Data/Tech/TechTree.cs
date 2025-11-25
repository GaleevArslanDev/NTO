using System.Collections.Generic;
using UnityEngine;

namespace Data.Tech
{
    [CreateAssetMenu(menuName = "Tech/TechTree")]
    public class TechTree : ScriptableObject
    {
        public string treeName;
        public string description;
        public List<TechNode> nodes = new List<TechNode>();
        public int maxTier = 5;
    }
}