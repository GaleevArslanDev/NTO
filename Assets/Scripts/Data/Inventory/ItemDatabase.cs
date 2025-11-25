using System.Collections.Generic;
using Core;
using UnityEngine;

namespace Data.Inventory
{
    [CreateAssetMenu(menuName = "Inventory/ItemDatabase")]
    public class ItemDatabase : ScriptableObject
    {
        public List<ItemData> items = new();

        public ItemData GetItemData(ItemType type)
        {
            return items.Find(item => item.type == type);
        }

        public Sprite GetItemIcon(ItemType type)
        {
            var itemData = GetItemData(type);
            return itemData?.icon;
        }

        public Color GetItemColor(ItemType type)
        {
            var itemData = GetItemData(type);
            return itemData?.particleColor ?? Color.white;
        }
    }
}