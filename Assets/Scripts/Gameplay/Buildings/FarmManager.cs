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
        
            public FarmPlot(string id, ItemType type, float rate)
            {
                plotId = id;
                resourceType = type;
                productionRate = rate;
                isActive = false;
                timer = 0f;
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
            // Находим все визуальные компоненты полей на сцене
            FarmPlotVisual[] plotVisuals = FindObjectsOfType<FarmPlotVisual>();
            
            foreach (var plotVisual in plotVisuals)
            {
                string plotId = plotVisual.GetPlotId();
                _farmPlotVisuals[plotId] = plotVisual;
                
                // Сначала скрываем все поля - они по умолчанию неактивны
                plotVisual.SetVisible(false);
                
                // Если поле уже разблокировано и активно - показываем его
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
            // Создаем или обновляем поле
            if (!_farmPlots.ContainsKey(plotId))
            {
                _farmPlots[plotId] = new FarmPlot(plotId, resourceType, productionRate);
            }
            
            // Активируем поле
            _farmPlots[plotId].isActive = true;
            _farmPlots[plotId].resourceType = resourceType;
            _farmPlots[plotId].productionRate = productionRate;
            
            // Обновляем визуал
            UpdatePlotVisual(_farmPlots[plotId]);
        }

        // Новый метод для деактивации поля (когда пассивный доход отключен)
        public void DeactivateFarmPlot(string plotId)
        {
            if (_farmPlots.ContainsKey(plotId))
            {
                _farmPlots[plotId].isActive = false;
                
                // Скрываем визуал
                if (_farmPlots[plotId].visual != null)
                {
                    _farmPlots[plotId].visual.SetVisible(false);
                }
            }
        }

        // Метод для изменения ресурса на активном поле
        public void ChangePlotResource(string plotId, ItemType newResourceType)
        {
            if (_farmPlots.TryGetValue(plotId, out var plot) && plot.isActive)
            {
                plot.resourceType = newResourceType;
                UpdatePlotVisual(plot);
            }
        }

        // Обновление визуала поля (только для активных полей)
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
                    
                    // Показываем и обновляем поле
                    plot.visual.SetVisible(true);
                    plot.visual.UpdateVisuals(plot.resourceType, productionLevel, currentEra);
                }
                else
                {
                    // Скрываем неактивное поле
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

        // Обновление всех визуалов при изменении эпохи
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
        
            UnlockFarmPlot(plotId, resourceType, productionRate);
        }
    
        // Деактивация пассивного дохода для ресурса
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
                    Inventory.Instance.AddItem(plot.resourceType);
                    plot.timer = 0f;
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

        // Метод для проверки, активно ли поле
        public bool IsPlotActive(string plotId)
        {
            return _farmPlots.ContainsKey(plotId) && _farmPlots[plotId].isActive;
        }
    }
}