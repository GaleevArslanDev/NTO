using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;

public class TooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private GameObject tooltipPrefab;
    
    private GameObject currentTooltip;
    private string itemName;
    private string description;
    
    public void SetTooltip(string name, string desc)
    {
        itemName = name;
        description = desc;
    }
    
    public void UpdateCount(int newCount)
    {
        // Обновляем описание с новым количеством
        description = description.Substring(0, description.LastIndexOf('\n') + 1) + $"Count: {newCount}";
        
        if (currentTooltip != null)
        {
            UpdateTooltipText();
        }
    }
    
    public void OnPointerEnter(PointerEventData eventData)
    {
        ShowTooltip();
    }
    
    public void OnPointerExit(PointerEventData eventData)
    {
        HideTooltip();
    }
    
    private void ShowTooltip()
    {
        if (tooltipPrefab != null && !string.IsNullOrEmpty(itemName))
        {
            currentTooltip = Instantiate(tooltipPrefab, transform);
            currentTooltip.transform.SetAsLastSibling();
            currentTooltip.transform.localPosition = new Vector3(0, 60, 0); // Смещаем немного выше
            UpdateTooltipText();
        }
    }
    
    private void HideTooltip()
    {
        if (currentTooltip != null)
        {
            Destroy(currentTooltip);
            currentTooltip = null;
        }
    }
    
    private void UpdateTooltipText()
    {
        var nameText = currentTooltip.transform.Find("ItemName")?.GetComponent<TextMeshProUGUI>();
        var descText = currentTooltip.transform.Find("ItemDescription")?.GetComponent<TextMeshProUGUI>();
        
        if (nameText != null) nameText.text = itemName;
        if (descText != null) descText.text = description;
    }
}