using System.Collections.Generic;
using Core;
using Data.Inventory;
using Gameplay.Items;
using UnityEngine;

namespace UI
{
    public class InventoryUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Transform itemsContainer;
        [SerializeField] private GameObject itemSlotPrefab;
    
        [Header("Database")]
        [SerializeField] private ItemDatabase itemDatabase;
    
        private Dictionary<ItemType, InventorySlotUI> _slotUIs = new();

        private void Start()
        {
            Inventory.Instance.OnInventoryChanged += UpdateItemDisplay;
        
            InitializeUI();
        }

        private void OnDestroy()
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
            _slotUIs.Clear();
        
            var allItems = Inventory.Instance.GetAllItems();
            foreach (var item in allItems)
            {
                CreateSlotUI(item.type, item.count);
            }
        }
    
        private void UpdateItemDisplay(ItemType itemType, int count)
        {
            if (_slotUIs.ContainsKey(itemType))
            {
                if (count <= 0)
                {
                    Destroy(_slotUIs[itemType].gameObject);
                    _slotUIs.Remove(itemType);
                }
                else
                {
                    _slotUIs[itemType].UpdateCount(count);
                }
            }
            else if (count > 0)
            {
                CreateSlotUI(itemType, count);
            }
        }
    
        private void CreateSlotUI(ItemType itemType, int count)
        {
            var slotObject = Instantiate(itemSlotPrefab, itemsContainer);
            var slotUI = slotObject.GetComponent<InventorySlotUI>();
        
            if (slotUI != null && itemDatabase != null)
            {
                var itemData = itemDatabase.GetItemData(itemType);
                var icon = itemData?.icon;
                var color = itemData?.particleColor ?? Color.white;
                var itemName = itemData?.name ?? itemType.ToString();
            
                slotUI.Initialize(itemType, color, count, itemName, icon);
                _slotUIs[itemType] = slotUI;
            }
            else if (itemDatabase == null)
            {
                Debug.LogWarning("ItemDatabase is not assigned in InventoryUI");
            }
        }
    }
}