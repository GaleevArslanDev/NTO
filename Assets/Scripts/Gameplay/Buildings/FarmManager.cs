using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Core;
using Gameplay.Items;
using UnityEngine;

namespace Gameplay.Buildings
{
    public class FarmManager : MonoBehaviour
    {
        public static FarmManager Instance;
    
        [System.Serializable]
        public class FarmPlot
        {
            public string plotId;
            public ItemType resourceType;
            public float productionRate;
            public bool isActive;
            public float timer;
            public FarmPlotVisual visual;
            public int accumulatedAmount; // Новое поле для накопленных ресурсов
        
            public FarmPlot(string id, ItemType type, float rate)
            {
                plotId = id;
                resourceType = type;
                productionRate = rate;
                isActive = false;
                timer = 0f;
                accumulatedAmount = 0;
            }
        }
    
        private Dictionary<string, FarmPlot> _farmPlots = new();
        private Coroutine _productionCoroutine;
        private Dictionary<string, FarmPlotVisual> _farmPlotVisuals = new();

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            InitializeAllPlotVisuals();
            _productionCoroutine = StartCoroutine(ProductionRoutine());
        }

        // Инициализация всех визуалов полей на сцене
        private void InitializeAllPlotVisuals()
        {
            FarmPlotVisual[] plotVisuals = FindObjectsOfType<FarmPlotVisual>();
            
            foreach (var plotVisual in plotVisuals)
            {
                string plotId = plotVisual.GetPlotId();
                _farmPlotVisuals[plotId] = plotVisual;
                plotVisual.SetVisible(false);
                
                if (_farmPlots.ContainsKey(plotId) && _farmPlots[plotId].isActive)
                {
                    var plot = _farmPlots[plotId];
                    plot.visual = plotVisual;
                    UpdatePlotVisual(plot);
                }
            }
        }

        public void UnlockFarmPlot(string plotId, ItemType resourceType, float productionRate)
        {
            if (!_farmPlots.ContainsKey(plotId))
            {
                _farmPlots[plotId] = new FarmPlot(plotId, resourceType, productionRate);
            }
            
            _farmPlots[plotId].isActive = true;
            _farmPlots[plotId].resourceType = resourceType;
            _farmPlots[plotId].productionRate = productionRate;
            
            UpdatePlotVisual(_farmPlots[plotId]);
        }

        public void DeactivateFarmPlot(string plotId)
        {
            if (_farmPlots.ContainsKey(plotId))
            {
                _farmPlots[plotId].isActive = false;
                
                if (_farmPlots[plotId].visual != null)
                {
                    _farmPlots[plotId].visual.SetVisible(false);
                }
            }
        }

        public void ChangePlotResource(string plotId, ItemType newResourceType)
        {
            if (_farmPlots.TryGetValue(plotId, out var plot) && plot.isActive)
            {
                plot.resourceType = newResourceType;
                UpdatePlotVisual(plot);
            }
        }

        // Новый метод для сбора всех накопленных ресурсов
        public Dictionary<ItemType, int> CollectAllAccumulatedResources()
        {
            Dictionary<ItemType, int> collectedResources = new Dictionary<ItemType, int>();
            
            foreach (var plot in _farmPlots.Values.Where(plot => plot.isActive && plot.accumulatedAmount > 0))
            {
                if (collectedResources.ContainsKey(plot.resourceType))
                {
                    collectedResources[plot.resourceType] += plot.accumulatedAmount;
                }
                else
                {
                    collectedResources[plot.resourceType] = plot.accumulatedAmount;
                }
                
                plot.accumulatedAmount = 0;
                UpdatePlotVisual(plot); // Обновляем визуал
            }
            
            return collectedResources;
        }

        // Получение информации о доступных ресурсах
        public Dictionary<ItemType, int> GetAvailableResources()
        {
            Dictionary<ItemType, int> availableResources = new Dictionary<ItemType, int>();
            
            foreach (var plot in _farmPlots.Values.Where(plot => plot.isActive && plot.accumulatedAmount > 0))
            {
                if (availableResources.ContainsKey(plot.resourceType))
                {
                    availableResources[plot.resourceType] += plot.accumulatedAmount;
                }
                else
                {
                    availableResources[plot.resourceType] = plot.accumulatedAmount;
                }
            }
            
            return availableResources;
        }

        private void UpdatePlotVisual(FarmPlot plot)
        {
            if (plot.visual == null && _farmPlotVisuals.ContainsKey(plot.plotId))
            {
                plot.visual = _farmPlotVisuals[plot.plotId];
            }
            
            if (plot.visual != null)
            {
                if (plot.isActive)
                {
                    int productionLevel = CalculateProductionLevel(plot.productionRate);
                    int currentEra = GetCurrentEra();
                    
                    plot.visual.SetVisible(true);
                    plot.visual.UpdateVisuals(plot.resourceType, productionLevel, currentEra);
                    
                    // Обновляем отображение количества накопленных ресурсов
                    plot.visual.UpdateAccumulatedAmount(plot.accumulatedAmount);
                }
                else
                {
                    plot.visual.SetVisible(false);
                }
            }
        }

        private int CalculateProductionLevel(float productionRate)
        {
            if (productionRate <= 2) return 1;
            if (productionRate <= 4) return 2;
            return 3;
        }

        private int GetCurrentEra()
        {
            if (TownHall.Instance != null)
            {
                return TownHall.Instance.GetCurrentLevel();
            }
            return 1;
        }

        public void OnEraChanged()
        {
            foreach (var plot in _farmPlots.Values.Where(plot => plot.isActive))
            {
                UpdatePlotVisual(plot);
            }
        }
    
        public void ActivatePassiveIncome(string resourceTypeStr, float productionRate)
        {
            var resourceType = (ItemType)System.Enum.Parse(typeof(ItemType), resourceTypeStr);
            var plotId = $"passive_{resourceType}";
            
            Debug.Log(plotId);
        
            UnlockFarmPlot(plotId, resourceType, productionRate);
        }
    
        public void DeactivatePassiveIncome(string resourceTypeStr)
        {
            var resourceType = (ItemType)System.Enum.Parse(typeof(ItemType), resourceTypeStr);
            var plotId = $"passive_{resourceType}";
            
            DeactivateFarmPlot(plotId);
        }
    
        private IEnumerator ProductionRoutine()
        {
            while (true)
            {
                yield return new WaitForSeconds(1f);

                foreach (var plot in _farmPlots.Values.Where(plot => plot.isActive))
                {
                    plot.timer += 1f;

                    if (!(plot.timer >= 60f / plot.productionRate)) continue;
                    
                    // Вместо добавления в инвентарь, накапливаем ресурсы
                    plot.accumulatedAmount++;
                    plot.timer = 0f;
                    
                    // Обновляем визуал
                    UpdatePlotVisual(plot);
                }
            }
        }
    
        public Dictionary<string, FarmPlot> GetFarmPlots()
        {
            return new Dictionary<string, FarmPlot>(_farmPlots);
        }

        private void OnDestroy()
        {
            if (_productionCoroutine != null)
                StopCoroutine(_productionCoroutine);
        }

        public void SetPlotTimer(string plotId, float time)
        {
            if (_farmPlots.ContainsKey(plotId))
                _farmPlots[plotId].timer = time;
        }

        public bool IsPlotActive(string plotId)
        {
            return _farmPlots.ContainsKey(plotId) && _farmPlots[plotId].isActive;
        }
        
        // Новый метод для получения сохраненных данных
        public FarmManagerSaveData GetSaveData()
        {
            var saveData = new FarmManagerSaveData();
            
            foreach (var plot in _farmPlots)
            {
                var plotData = new FarmPlotSaveData
                {
                    plotId = plot.Value.plotId,
                    resourceType = plot.Value.resourceType,
                    productionRate = plot.Value.productionRate,
                    isActive = plot.Value.isActive,
                    timer = plot.Value.timer,
                    accumulatedAmount = plot.Value.accumulatedAmount
                };
                
                saveData.farmPlots[plot.Key] = plotData;
            }
            
            return saveData;
        }
        
        // Новый метод для применения сохраненных данных
        public void ApplySaveData(FarmManagerSaveData saveData)
        {
            if (saveData == null) return;
            
            _farmPlots.Clear();
            
            foreach (var plotData in saveData.farmPlots)
            {
                var plot = new FarmPlot(
                    plotData.Value.plotId,
                    plotData.Value.resourceType,
                    plotData.Value.productionRate
                )
                {
                    isActive = plotData.Value.isActive,
                    timer = plotData.Value.timer,
                    accumulatedAmount = plotData.Value.accumulatedAmount
                };
                
                _farmPlots[plotData.Key] = plot;
                
                if (plot.isActive)
                {
                    UpdatePlotVisual(plot);
                }
            }
        }
    }
    
    // Классы для сохранения данных
    [System.Serializable]
    public class FarmManagerSaveData
    {
        public Dictionary<string, FarmPlotSaveData> farmPlots = new Dictionary<string, FarmPlotSaveData>();
    }
    
    [System.Serializable]
    public class FarmPlotSaveData
    {
        public string plotId;
        public ItemType resourceType;
        public float productionRate;
        public bool isActive;
        public float timer;
        public int accumulatedAmount;
    }
}