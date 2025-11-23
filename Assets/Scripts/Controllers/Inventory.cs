using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class Inventory : MonoBehaviour
{
    public static Inventory Instance { get; private set; }
    
    private int _capacity = 50;
    
    [System.Serializable]
    public class InventorySlot
    {
        public ItemType Type;
        public int Count;
    }
    
    [SerializeField] private List<InventorySlot> items = new List<InventorySlot>();
    
    public UnityAction<ItemType, int> OnInventoryChanged;
    
    void Awake()
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
        var existingSlot = items.Find(slot => slot.Type == type);
        if (existingSlot != null)
        {
            existingSlot.Count += count;
            if (existingSlot.Count > _capacity) existingSlot.Count = _capacity;
        }
        else
        {
            items.Add(new InventorySlot { Type = type, Count = count });
        }
        
        OnInventoryChanged?.Invoke(type, GetItemCount(type));
    }
    
    public void RemoveItem(ItemType type, int count = 1)
    {
        var existingSlot = items.Find(slot => slot.Type == type);
        if (existingSlot != null)
        {
            existingSlot.Count -= count;
            if (existingSlot.Count <= 0)
                items.Remove(existingSlot);
            
            OnInventoryChanged?.Invoke(type, GetItemCount(type));
        }
    }
    
    public int GetItemCount(ItemType type)
    {
        var existingSlot = items.Find(slot => slot.Type == type);
        return existingSlot?.Count ?? 0;
    }
    
    public bool HasItems(ItemType type, int count)
    {
        return GetItemCount(type) >= count;
    }

    public bool HasResources(System.Collections.Generic.List<ResourceCost> requiredResources)
    {
        foreach (var resource in requiredResources)
        {
            if (GetItemCount(resource.Type) < resource.Amount)
                return false;
        }
        return true;
    }
    
    public void UpdateCapacity(int newCapacity)
    {
        _capacity = newCapacity;
    }
    
    public int GetCapacity() => _capacity;
    
    public List<InventorySlot> GetAllItems()
    {
        return new List<InventorySlot>(items);
    }
}