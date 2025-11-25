using Core;

namespace Data.Tech
{
    [System.Serializable]
    public class TechEffect
    {
        public EffectType effectType;
        public float floatValue;
        public int intValue;
        public string stringValue; // Для названий зданий и т.д.
    }
}