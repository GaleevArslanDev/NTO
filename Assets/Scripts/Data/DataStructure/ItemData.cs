using UnityEngine;

[CreateAssetMenu(menuName = "Inventory/ItemData")]
public class ItemData : ScriptableObject
{
    public ItemType Type;
    public Color ParticleColor = Color.white;
    public Sprite Icon;
    
    [Header("Breaking Settings")]
    public float BreakTime = 2f; // Время необходимое для "ломания"
    public float ShakeIntensity = 0.3f; // Интенсивность тряски
    public float FloatHeight = 0.5f; // Высота подъема при ломании
}

public enum ItemType
{
    Crystal_Red,
    Crystal_Blue,
    Stone,
    Wood,
    Metal
}