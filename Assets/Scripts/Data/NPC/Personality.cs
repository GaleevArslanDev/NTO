using System;
using Core;

namespace Data.NPC
{
    [Serializable]
    public class Personality
    {
        public Trait[] traits;
        public int openness; // 0-100
        public int friendliness; // 0-100
        public int ambition; // 0-100

        public Personality()
        {
            traits = Array.Empty<Trait>();
            openness = 50;
            friendliness = 50;
            ambition = 50;
        }

        public Personality(Trait[] traits, int openness, int friendliness, int ambition)
        {
            this.traits = traits;
            this.openness = Math.Clamp(openness, 0, 100);
            this.friendliness = Math.Clamp(friendliness, 0, 100);
            this.ambition = Math.Clamp(ambition, 0, 100);
        }

        public bool HasTrait(Trait trait)
        {
            return traits != null && Array.Exists(traits, t => t == trait);
        }
    }
}