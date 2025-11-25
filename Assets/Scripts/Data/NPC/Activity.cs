using Core;
using UnityEngine;

namespace Data.NPC
{
    [System.Serializable]
    public struct Activity
    {
        public ActivityType type;
        public Vector3 location;
        public string targetNpc;
        public float duration;
    
        public Activity(ActivityType type, Vector3 location, string targetNpc = "", float duration = 60f)
        {
            this.type = type;
            this.location = location;
            this.targetNpc = targetNpc;
            this.duration = duration;
        }
    }
}