using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Inventory/ItemDatabase")]
public class ItemDatabase : ScriptableObject
{
    public List<ItemData> items;

    public ItemData GetItemData(ItemType type)
    {
        return items.Find(item => item.Type == type);
    }

    public Sprite GetItemIcon(ItemType type)
    {
        var itemData = GetItemData(type);
        return itemData?.Icon;
    }

    public Color GetItemColor(ItemType type)
    {
        var itemData = GetItemData(type);
        return itemData?.ParticleColor ?? Color.white;
    }
}