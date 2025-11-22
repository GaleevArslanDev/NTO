using UnityEngine;

[System.Serializable]
public struct Activity
{
    public ActivityType type;
    public Vector3 location;
    public string targetNPC;
    public float duration;
    
    public Activity(ActivityType type, Vector3 location, string targetNPC = "", float duration = 60f)
    {
        this.type = type;
        this.location = location;
        this.targetNPC = targetNPC;
        this.duration = duration;
    }
}