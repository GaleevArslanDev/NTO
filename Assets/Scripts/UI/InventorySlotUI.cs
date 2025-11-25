using Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class InventorySlotUI : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private Image itemIcon;
        [SerializeField] private TextMeshProUGUI countText;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private GameObject emptyIcon;
    
        private ItemType _itemType;
    
        public void Initialize(ItemType type, Color color, int count, string itemName, Sprite icon)
        {
            _itemType = type;
        
            backgroundImage.color = color;
            countText.text = count.ToString();
        
            if (itemIcon != null)
            {
                if (icon != null)
                {
                    itemIcon.sprite = icon;
                    itemIcon.color = Color.white;
                
                    if (emptyIcon != null)
                        emptyIcon.SetActive(false);
                }
                else
                {
                    itemIcon.color = Color.clear;
                    if (emptyIcon != null)
                        emptyIcon.SetActive(true);
                }
            }
        
            if (TryGetComponent<TooltipTrigger>(out var tooltip))
            {
                tooltip.SetTooltip(itemName, $"Type: {type}\nCount: {count}");
            }
        }
    
        public void UpdateCount(int newCount)
        {
            countText.text = newCount.ToString();
        
            if (TryGetComponent<TooltipTrigger>(out var tooltip))
            {
                tooltip.UpdateCount(newCount);
            }
        }
    }
}