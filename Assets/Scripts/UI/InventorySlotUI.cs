using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventorySlotUI : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private Image itemIcon;
    [SerializeField] private TextMeshProUGUI countText;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private GameObject emptyIcon; // GameObject который показывается если иконки нет
    
    private ItemType itemType;
    
    public void Initialize(ItemType type, Color color, int count, string itemName, Sprite icon)
    {
        itemType = type;
        
        // Настраиваем внешний вид
        backgroundImage.color = color;
        countText.text = count.ToString();
        
        // Устанавливаем иконку
        if (itemIcon != null)
        {
            if (icon != null)
            {
                itemIcon.sprite = icon;
                itemIcon.color = Color.white;
                
                // Скрываем placeholder если есть иконка
                if (emptyIcon != null)
                    emptyIcon.SetActive(false);
            }
            else
            {
                // Показываем placeholder если иконки нет
                itemIcon.color = Color.clear;
                if (emptyIcon != null)
                    emptyIcon.SetActive(true);
            }
        }
        
        // Добавляем tooltip
        if (TryGetComponent<TooltipTrigger>(out var tooltip))
        {
            tooltip.SetTooltip(itemName, $"Type: {type}\nCount: {count}");
        }
    }
    
    public void UpdateCount(int newCount)
    {
        countText.text = newCount.ToString();
        
        // Обновляем tooltip
        if (TryGetComponent<TooltipTrigger>(out var tooltip))
        {
            tooltip.UpdateCount(newCount);
        }
    }
}