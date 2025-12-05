using Core;
using UnityEngine;

namespace Data.Inventory
{
    [CreateAssetMenu(menuName = "Inventory/ItemData")]
    public class ItemData : ScriptableObject
    {
        public ItemType type;
        public Color particleColor = Color.white;
        public Sprite icon;
        
        public ItemSoundData soundData;
    
        [Header("Breaking Settings")]
        public float breakTime = 2f;
        public float shakeIntensity = 0.3f;
        public float floatHeight = 0.5f;
        public float vacuumTime = 1f;
        public int amount = 1;
    
        [Header("Enemy Spawn Settings")]
        [Range(0f, 1f)]
        public float enemySpawnChance = 0.1f;
        public int maxSpawnCount = 2;
    }
    
    [System.Serializable]
    public class ItemSoundData
    {
        public AudioClip breakingSound;
        public AudioClip collectSound;
        public float breakingVolume = 1f;
        public float collectVolume = 1f;
    }
}