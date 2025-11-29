using System.Collections.Generic;
using System.Linq;
using Core;
using Data.Game;
using Gameplay.Systems;
using UnityEngine;
using UnityEngine.Events;

namespace Gameplay.Items
{
    public class Inventory : MonoBehaviour
    {
        public static Inventory Instance { get; private set; }
    
        private int _capacity = 50;
    
        [System.Serializable]
        public class InventorySlot
        {
            public ItemType type; 
            public int count;
        }

        [SerializeField] private List<InventorySlot> items;
    
        public UnityAction<ItemType, int> OnInventoryChanged;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }
    
        public void AddItem(ItemType type, int count = 1)
        {
            var existingSlot = items.Find(slot => slot.type == type);
            if (existingSlot != null)
            {
                existingSlot.count += count;
                if (existingSlot.count > _capacity) existingSlot.count = _capacity;
            }
            else
            {
                items.Add(new InventorySlot { type = type, count = count });
            }
        
            OnInventoryChanged?.Invoke(type, GetItemCount(type));
            
            if (count >= 5 || type == ItemType.CrystalRed || type == ItemType.CrystalBlue)
            {
                if (SaveManager.Instance != null)
                    SaveManager.Instance.AutoSave();
            }
        }
    
        public void RemoveItem(ItemType type, int count = 1)
        {
            var existingSlot = items.Find(slot => slot.type == type);
            if (existingSlot == null) return;
            existingSlot.count -= count;
            if (existingSlot.count <= 0)
                items.Remove(existingSlot);
            
            OnInventoryChanged?.Invoke(type, GetItemCount(type));
        }
    
        public int GetItemCount(ItemType type)
        {
            var existingSlot = items.Find(slot => slot.type == type);
            return existingSlot?.count ?? 0;
        }
    
        public bool HasItems(ItemType type, int count)
        {
            return GetItemCount(type) >= count;
        }

        public bool HasResources(List<ResourceCost> requiredResources)
        {
            return requiredResources.All(resource => GetItemCount(resource.type) >= resource.amount);
        }
    
        public void UpdateCapacity(int newCapacity)
        {
            _capacity = newCapacity;
        }

        public void ClearInventory()
        {
            items.Clear();
        }
    
        public int GetCapacity() => _capacity;
    
        public List<InventorySlot> GetAllItems()
        {
            return new List<InventorySlot>(items);
        }
    }
}