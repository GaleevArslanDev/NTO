using Data.Game;

namespace Data.NPC
{
    [System.Serializable]
    public class Memory
    {
        public string memoryText;
        public int relationshipImpact;
        public string sourceNpc;
        public GameTimestamp timestamp;

        public Memory()
        {
            memoryText = "";
            relationshipImpact = 0;
            sourceNpc = "";
            timestamp = new GameTimestamp(1, 0, 0);
        }

        public Memory(string text, int impact, string source, GameTimestamp time)
        {
            memoryText = text;
            relationshipImpact = impact;
            sourceNpc = source;
            timestamp = time;
        }
    }
}