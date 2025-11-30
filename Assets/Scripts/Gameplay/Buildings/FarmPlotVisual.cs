using UnityEngine;
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

        private ItemType _currentResourceType;
        private int _currentProductionLevel;
        private int _currentEra;
        private bool _isVisible = false;

        public void Initialize(string id)
        {
            plotId = id;
            SetVisible(false); // По умолчанию скрыто
        }

        public void UpdateVisuals(ItemType resourceType, int productionLevel, int era)
        {
            _currentResourceType = resourceType;
            _currentProductionLevel = productionLevel;
            _currentEra = era;

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
                case 1: stoneAgeVisual?.SetActive(true); break;
                case 2: medievalVisual?.SetActive(true); break;
                case 3: modernVisual?.SetActive(true); break;
                case 4: futureVisual?.SetActive(true); break;
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

        // Метод для управления видимостью всего поля
        public void SetVisible(bool visible)
        {
            _isVisible = visible;
            
            // Включаем/выключаем основной GameObject
            gameObject.SetActive(visible);
            
            // Если скрываем, гарантируем что все визуалы выключены
            if (!visible)
            {
                DeactivateAllMarkers();
                stoneAgeVisual?.SetActive(false);
                medievalVisual?.SetActive(false);
                modernVisual?.SetActive(false);
                futureVisual?.SetActive(false);
            }
        }

        public string GetPlotId() => plotId;
        
        // Метод для установки plotId из инспектора (если нужно)
        public void SetPlotId(string id) => plotId = id;
    }
}