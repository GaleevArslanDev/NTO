using UnityEngine;

[System.Serializable]
public class Memory
{
    public string memoryText;
    public int relationshipImpact;
    public string sourceNPC;
    public GameTimestamp timestamp;

    public Memory()
    {
        memoryText = "";
        relationshipImpact = 0;
        sourceNPC = "";
        timestamp = new GameTimestamp(1, 0, 0);
    }

    public Memory(string text, int impact, string source, GameTimestamp time)
    {
        memoryText = text;
        relationshipImpact = impact;
        sourceNPC = source;
        timestamp = time;
    }
}