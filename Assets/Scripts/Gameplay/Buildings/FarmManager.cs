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
            public float productionRate; // Ресурсов в минуту
            public bool isActive;
            public float timer;
        
            public FarmPlot(string id, ItemType type, float rate)
            {
                plotId = id;
                resourceType = type;
                productionRate = rate;
                isActive = false;
                timer = 0f;
            }
        }
    
        private Dictionary<string, FarmPlot> _farmPlots = new ();
        private Coroutine _productionCoroutine;

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
            _productionCoroutine = StartCoroutine(ProductionRoutine());
        }
    
        public void UnlockFarmPlot(string plotId, ItemType resourceType, float productionRate)
        {
            if (!_farmPlots.ContainsKey(plotId))
            {
                _farmPlots[plotId] = new FarmPlot(plotId, resourceType, productionRate);
            }
            _farmPlots[plotId].isActive = true;
        }
    
        public void UpgradeFarmPlot(string plotId, float newProductionRate)
        {
            if (_farmPlots.TryGetValue(plotId, value: out var plot))
            {
                plot.productionRate = newProductionRate;
            }
        }
    
        public void ActivatePassiveIncome(string resourceTypeStr, float productionRate)
        {
            // Конвертируем строку в ItemType
            var resourceType = (ItemType)System.Enum.Parse(typeof(ItemType), resourceTypeStr);
            var plotId = $"passive_{resourceType}";
        
            UnlockFarmPlot(plotId, resourceType, productionRate);
        }
    
        private IEnumerator ProductionRoutine()
        {
            while (true)
            {
                yield return new WaitForSeconds(1f); // Проверяем каждую секунду

                foreach (var plot in _farmPlots.Values.Where(plot => plot.isActive))
                {
                    plot.timer += 1f;

                    if (!(plot.timer >= 60f / plot.productionRate)) continue; // Когда накопился 1 ресурс
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
    }
}