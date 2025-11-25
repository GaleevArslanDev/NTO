using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace UI
{
    public class TooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private GameObject tooltipPrefab;
    
        private GameObject _currentTooltip;
        private string _itemName;
        private string _description;
    
        public void SetTooltip(string itemName, string desc)
        {
            _itemName = itemName;
            _description = desc;
        }
    
        public void UpdateCount(int newCount)
        {
            _description = _description[..(_description.LastIndexOf('\n') + 1)] + $"Count: {newCount}";
        
            if (_currentTooltip != null)
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
            if (tooltipPrefab == null || string.IsNullOrEmpty(_itemName)) return;
            _currentTooltip = Instantiate(tooltipPrefab, transform);
            _currentTooltip.transform.SetAsLastSibling();
            _currentTooltip.transform.localPosition = new Vector3(0, 60, 0);
            UpdateTooltipText();
        }
    
        private void HideTooltip()
        {
            if (_currentTooltip == null) return;
            Destroy(_currentTooltip);
            _currentTooltip = null;
        }
    
        private void UpdateTooltipText()
        {
            var nameText = _currentTooltip.transform.Find("ItemName")?.GetComponent<TextMeshProUGUI>();
            var descText = _currentTooltip.transform.Find("ItemDescription")?.GetComponent<TextMeshProUGUI>();
        
            if (nameText != null) nameText.text = _itemName;
            if (descText != null) descText.text = _description;
        }
    }
}