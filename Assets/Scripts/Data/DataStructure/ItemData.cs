using UnityEngine;

[CreateAssetMenu(menuName = "Inventory/ItemData")]
public class ItemData : ScriptableObject
{
    public ItemType Type;
    public Color ParticleColor = Color.white;
    public Sprite Icon;
    
    [Header("Breaking Settings")]
    public float BreakTime = 2f;
    public float ShakeIntensity = 0.3f;
    public float FloatHeight = 0.5f;
    
    [Header("Enemy Spawn Settings")]
    [Range(0f, 1f)]
    public float enemySpawnChance = 0.1f;
    public int maxSpawnCount = 2;
}

public enum ItemType
{
    Crystal_Red,
    Crystal_Blue,
    Stone,
    Wood,
    Metal
}