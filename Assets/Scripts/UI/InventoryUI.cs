using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InventoryUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Transform itemsContainer;
    [SerializeField] private GameObject itemSlotPrefab;
    
    [Header("Database")]
    [SerializeField] private ItemDatabase itemDatabase;
    
    private Dictionary<ItemType, InventorySlotUI> slotUIs = new Dictionary<ItemType, InventorySlotUI>();
    
    void Start()
    {
        Inventory.Instance.OnInventoryChanged += UpdateItemDisplay;
        
        InitializeUI();
    }
    
    void OnDestroy()
    {
        if (Inventory.Instance != null)
            Inventory.Instance.OnInventoryChanged -= UpdateItemDisplay;
    }
    
    private void InitializeUI()
    {
        foreach (Transform child in itemsContainer)
        {
            Destroy(child.gameObject);
        }
        slotUIs.Clear();
        
        var allItems = Inventory.Instance.GetAllItems();
        foreach (var item in allItems)
        {
            CreateSlotUI(item.Type, item.Count);
        }
    }
    
    private void UpdateItemDisplay(ItemType itemType, int count)
    {
        if (slotUIs.ContainsKey(itemType))
        {
            if (count <= 0)
            {
                Destroy(slotUIs[itemType].gameObject);
                slotUIs.Remove(itemType);
            }
            else
            {
                slotUIs[itemType].UpdateCount(count);
            }
        }
        else if (count > 0)
        {
            CreateSlotUI(itemType, count);
        }
    }
    
    private void CreateSlotUI(ItemType itemType, int count)
    {
        GameObject slotObject = Instantiate(itemSlotPrefab, itemsContainer);
        InventorySlotUI slotUI = slotObject.GetComponent<InventorySlotUI>();
        
        if (slotUI != null && itemDatabase != null)
        {
            ItemData itemData = itemDatabase.GetItemData(itemType);
            Sprite icon = itemData?.Icon;
            Color color = itemData?.ParticleColor ?? Color.white;
            string itemName = itemData?.name ?? itemType.ToString();
            
            slotUI.Initialize(itemType, color, count, itemName, icon);
            slotUIs[itemType] = slotUI;
        }
        else if (itemDatabase == null)
        {
            Debug.LogWarning("ItemDatabase is not assigned in InventoryUI");
        }
    }
}