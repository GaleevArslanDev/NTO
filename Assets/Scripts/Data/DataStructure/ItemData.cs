using UnityEngine;

[CreateAssetMenu(menuName = "Inventory/ItemData")]
public class ItemData : ScriptableObject
{
    public ItemType Type;
    public Color ParticleColor = Color.white;
    public Sprite Icon;
}

public enum ItemType
{
    Crystal_Red,
    Crystal_Blue,
    Stone,
    Wood,
    Metal
}