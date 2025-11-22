using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
    
    private Dictionary<string, FarmPlot> farmPlots = new Dictionary<string, FarmPlot>();
    private Coroutine productionCoroutine;
    
    void Awake()
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
    
    void Start()
    {
        productionCoroutine = StartCoroutine(ProductionRoutine());
    }
    
    public void UnlockFarmPlot(string plotId, ItemType resourceType, float productionRate)
    {
        if (!farmPlots.ContainsKey(plotId))
        {
            farmPlots[plotId] = new FarmPlot(plotId, resourceType, productionRate);
        }
        farmPlots[plotId].isActive = true;
    }
    
    public void UpgradeFarmPlot(string plotId, float newProductionRate)
    {
        if (farmPlots.ContainsKey(plotId))
        {
            farmPlots[plotId].productionRate = newProductionRate;
        }
    }
    
    public void ActivatePassiveIncome(string resourceTypeStr, float productionRate)
    {
        // Конвертируем строку в ItemType
        ItemType resourceType = (ItemType)System.Enum.Parse(typeof(ItemType), resourceTypeStr);
        string plotId = $"passive_{resourceType}";
        
        UnlockFarmPlot(plotId, resourceType, productionRate);
    }
    
    private IEnumerator ProductionRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(1f); // Проверяем каждую секунду
            
            foreach (var plot in farmPlots.Values)
            {
                if (plot.isActive)
                {
                    plot.timer += 1f;
                    float resourcesProduced = plot.productionRate / 60f; // Ресурсов в секунду
                    
                    if (plot.timer >= 60f / plot.productionRate) // Когда накопился 1 ресурс
                    {
                        Inventory.Instance.AddItem(plot.resourceType, 1);
                        plot.timer = 0f;
                    }
                }
            }
        }
    }
    
    public Dictionary<string, FarmPlot> GetFarmPlots()
    {
        return new Dictionary<string, FarmPlot>(farmPlots);
    }
    
    void OnDestroy()
    {
        if (productionCoroutine != null)
            StopCoroutine(productionCoroutine);
    }
}