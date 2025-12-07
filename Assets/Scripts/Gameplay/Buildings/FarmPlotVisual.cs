using UnityEngine;
using TMPro;
using Core;

namespace Gameplay.Buildings
{
    public class FarmPlotVisual : MonoBehaviour
    {
        [Header("Идентификатор поля")]
        [SerializeField] private string plotId;

        [Header("Визуалы эпох")]
        public GameObject stoneAgeVisual;
        public GameObject medievalVisual;
        public GameObject modernVisual;
        public GameObject futureVisual;

        [Header("Ряды маркеров ресурсов")]
        public GameObject[] markerRow1;
        public GameObject[] markerRow2;
        public GameObject[] markerRow3;
        
        [Header("UI для отображения количества")]
        [SerializeField] private TextMeshProUGUI amountText;
        [SerializeField] private GameObject amountDisplay;

        private ItemType _currentResourceType;
        private int _currentProductionLevel;
        private int _currentEra;
        private bool _isVisible = false;
        private int _accumulatedAmount = 0;

        public void Initialize(string id)
        {
            plotId = id;
            SetVisible(false);
        }

        public void UpdateVisuals(ItemType resourceType, int productionLevel, int era)
        {
            _currentResourceType = resourceType;
            _currentProductionLevel = productionLevel;
            _currentEra = era;

            UpdateVisuals();
        }
        
        // Новый метод для обновления отображения количества
        public void UpdateAccumulatedAmount(int amount)
        {
            _accumulatedAmount = amount;
            
            if (amountDisplay != null)
            {
                amountDisplay.SetActive(amount > 0);
            }
            
            if (amountText != null)
            {
                amountText.text = amount.ToString();
                
                // Изменяем цвет текста в зависимости от количества
                if (amount >= 20)
                    amountText.color = Color.green;
                else if (amount >= 10)
                    amountText.color = Color.yellow;
                else
                    amountText.color = Color.white;
            }
            
            UpdateVisuals();
        }

        private void UpdateVisuals()
        {
            if (!_isVisible) return;

            UpdateEraVisual();
            UpdateProductionMarkers();
        }

        private void UpdateEraVisual()
        {
            stoneAgeVisual?.SetActive(false);
            medievalVisual?.SetActive(false);
            modernVisual?.SetActive(false);
            futureVisual?.SetActive(false);

            switch (_currentEra)
            {
                case 0: stoneAgeVisual?.SetActive(true); break;
                case 1: medievalVisual?.SetActive(true); break;
                case 2: modernVisual?.SetActive(true); break;
                case 3: futureVisual?.SetActive(true); break;
                default: stoneAgeVisual?.SetActive(true); break;
            }
        }

        private void UpdateProductionMarkers()
        {
            int resourceIndex = GetResourceIndex(_currentResourceType);
            DeactivateAllMarkers();

            switch (_currentProductionLevel)
            {
                case 1:
                    if (IsValidIndex(resourceIndex, markerRow1))
                        markerRow1[resourceIndex]?.SetActive(true);
                    break;
                case 2:
                    if (IsValidIndex(resourceIndex, markerRow1))
                        markerRow1[resourceIndex]?.SetActive(true);
                    if (IsValidIndex(resourceIndex, markerRow2))
                        markerRow2[resourceIndex]?.SetActive(true);
                    break;
                case 3:
                    if (IsValidIndex(resourceIndex, markerRow1))
                        markerRow1[resourceIndex]?.SetActive(true);
                    if (IsValidIndex(resourceIndex, markerRow2))
                        markerRow2[resourceIndex]?.SetActive(true);
                    if (IsValidIndex(resourceIndex, markerRow3))
                        markerRow3[resourceIndex]?.SetActive(true);
                    break;
            }
        }

        private void DeactivateAllMarkers()
        {
            DeactivateMarkerRow(markerRow1);
            DeactivateMarkerRow(markerRow2);
            DeactivateMarkerRow(markerRow3);
        }

        private void DeactivateMarkerRow(GameObject[] row)
        {
            if (row != null)
            {
                foreach (var marker in row)
                {
                    marker?.SetActive(false);
                }
            }
        }

        private bool IsValidIndex(int index, GameObject[] array)
        {
            return array != null && index >= 0 && index < array.Length;
        }

        private int GetResourceIndex(ItemType resourceType)
        {
            switch (resourceType)
            {
                case ItemType.Wood: return 0;
                case ItemType.Stone: return 1;
                case ItemType.Metal: return 2;
                case ItemType.CrystalRed: return 3;
                case ItemType.CrystalBlue: return 4;
                default: return 0;
            }
        }

        public void SetVisible(bool visible)
        {
            _isVisible = visible;
            
            gameObject.SetActive(visible);
            
            if (!visible)
            {
                DeactivateAllMarkers();
                stoneAgeVisual?.SetActive(false);
                medievalVisual?.SetActive(false);
                modernVisual?.SetActive(false);
                futureVisual?.SetActive(false);
                
                if (amountDisplay != null)
                    amountDisplay.SetActive(false);
            }
        }

        public string GetPlotId() => plotId;
        
        public void SetPlotId(string id) => plotId = id;
    }
}