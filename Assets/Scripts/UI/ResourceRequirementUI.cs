using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ResourceRequirementUI : MonoBehaviour
{
    [SerializeField] private Image _icon;
    [SerializeField] private TextMeshProUGUI _amountText;
    [SerializeField] private Color _hasEnoughColor = Color.white;
    [SerializeField] private Color _notEnoughColor = Color.red;

    public void Set(ItemType type, int amount)
    {
        // Здесь можно добавить получение иконки по типу предмета
        // _icon.sprite = GetIconForType(type);
        
        _amountText.text = $"{amount}";
        
        // Проверяем достаточно ли ресурсов и меняем цвет
        bool hasEnough = Inventory.Instance.GetItemCount(type) >= amount;
        _amountText.color = hasEnough ? _hasEnoughColor : _notEnoughColor;
    }
}