using Core;
using Data.Inventory;
using Gameplay.Items;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class ResourceRequirementUI : MonoBehaviour
    {
        [SerializeField] private Image icon;
        [SerializeField] private TextMeshProUGUI amountText;
        [SerializeField] private Color hasEnoughColor = Color.white;
        [SerializeField] private Color notEnoughColor = Color.red;
        [SerializeField] private ItemDatabase itemDatabase;

        public void Set(ItemType type, int amount)
        {
            // Здесь можно добавить получение иконки по типу предмета
            icon.sprite = itemDatabase.GetItemIcon(type);
        
            amountText.text = $"{itemDatabase.GetItemData(type).name} x{amount}";
        
            // Проверяем достаточно ли ресурсов и меняем цвет
            var hasEnough = Inventory.Instance.GetItemCount(type) >= amount;
            amountText.color = hasEnough ? hasEnoughColor : notEnoughColor;
        }
    }
}