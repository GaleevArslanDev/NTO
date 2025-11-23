using System.Collections.Generic;
using UnityEngine;

public class PlayerProgression : MonoBehaviour
{
    public static PlayerProgression Instance;
    
    [Header("Tech Trees")]
    public TechTree forgeTechTree;
    public TechTree farmTechTree;
    public TechTree generalTechTree;
    
    [Header("Player Stats")]
    public float damageMultiplier = 1f;
    public float fireRateMultiplier = 1f;
    public float miningSpeedMultiplier = 1f;
    public int inventoryCapacity = 50;
    public float collectionRangeMultiplier = 1f;
    
    private Dictionary<string, bool> unlockedTechs = new Dictionary<string, bool>();
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeTechTrees();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void InitializeTechTrees()
    {
        // Загрузка сохраненных прокачек
        LoadTechProgress();
        
        // Применяем все разблокированные улучшения
        ApplyAllUnlockedTechs();
    }
    
    public bool CanUnlockTech(string nodeId, TechTree techTree)
    {
        TechNode node = techTree.nodes.Find(n => n.nodeId == nodeId);
        if (node == null) return false;
    
        // Проверяем уровень ратуши
        int currentTier = TownHall.Instance.GetUnlockedTechTier();
        if (node.tier > currentTier)
        {
            Debug.Log($"Для этой технологии требуется уровень ратуши, открывающий {node.tier} тир");
            return false;
        }
    
        // Проверяем пройдены ли пререквизиты
        foreach (string prereq in node.prerequisiteNodes)
        {
            if (!IsTechUnlocked(prereq))
                return false;
        }
    
        // Проверяем хватает ли ресурсов
        if (!Inventory.Instance.HasResources(node.unlockCost))
            return false;
    
        return true;
    }
    
    public bool UnlockTech(string nodeId, TechTree techTree)
    {
        if (!CanUnlockTech(nodeId, techTree)) return false;
    
        TechNode node = techTree.nodes.Find(n => n.nodeId == nodeId);
    
        // Тратим ресурсы
        foreach (var cost in node.unlockCost)
        {
            Inventory.Instance.RemoveItem(cost.Type, cost.Amount);
        }
    
        // Разблокируем
        node.isUnlocked = true;
        unlockedTechs[nodeId] = true;
    
        // Применяем эффекты
        ApplyTechEffects(node);
    
        // Сохраняем прогресс
        SaveTechProgress();
    
        Debug.Log($"Технология разблокирована: {node.nodeName}");
        
        if (AIAssistant.Instance != null)
        {
            AIAssistant.Instance.OnTechUnlocked(node.nodeName);
        }
        
        return true;
    }
    
    private void ApplyTechEffects(TechNode node)
    {
        foreach (var effect in node.effects)
        {
            switch (effect.effectType)
            {
                case TechEffect.EffectType.FireRateMultiplier:
                    fireRateMultiplier *= effect.floatValue;
                    UpdateMTBStats();
                    break;
                case TechEffect.EffectType.DamageMultiplier:
                    damageMultiplier *= effect.floatValue;
                    UpdateMTBStats();
                    break;
                    
                case TechEffect.EffectType.MiningSpeedMultiplier:
                    miningSpeedMultiplier *= effect.floatValue;
                    UpdateMTBStats();
                    break;
                    
                case TechEffect.EffectType.InventoryCapacity:
                    inventoryCapacity = effect.intValue;
                    UpdateMTBStats();
                    break;
                    
                case TechEffect.EffectType.CollectionRangeMultiplier:
                    collectionRangeMultiplier *= effect.floatValue;
                    UpdateMTBStats();
                    break;
                    
                case TechEffect.EffectType.PassiveIncome:
                    // Активируем пассивный доход
                    if (FarmManager.Instance != null)
                        FarmManager.Instance.ActivatePassiveIncome(effect.stringValue, effect.floatValue);
                    break;
                    
                case TechEffect.EffectType.UnlockBuilding:
                    // Разблокируем здание
                    if (BuildingManager.Instance != null)
                        BuildingManager.Instance.UnlockBuilding(effect.stringValue);
                    break;
                    
                case TechEffect.EffectType.UnlockUpgradeTier:
                    // Открываем новый тир улучшений
                    if (TownHall.Instance != null)
                        TownHall.Instance.UnlockUpgradeTier(effect.intValue);
                    break;
            }
        }
    }
    
    private void ApplyAllUnlockedTechs()
    {
        // Применяем эффекты всех разблокированных технологий
        foreach (var tree in new[] { forgeTechTree, farmTechTree, generalTechTree })
        {
            if (tree != null)
            {
                foreach (var node in tree.nodes)
                {
                    if (node.isUnlocked)
                    {
                        ApplyTechEffects(node);
                    }
                }
            }
        }
    }
    
    public void UnlockFarmPlot(string plotId, ItemType resourceType, float productionRate)
    {
        if (FarmManager.Instance != null)
        {
            FarmManager.Instance.UnlockFarmPlot(plotId, resourceType, productionRate);
        }
    }

    public void UpgradeFarmPlot(string plotId, float newProductionRate)
    {
        if (FarmManager.Instance != null)
        {
            FarmManager.Instance.UpgradeFarmPlot(plotId, newProductionRate);
        }
    }
    
    private void UpdateMTBStats()
    {
        // Обновляем характеристики MTB
        if (MTB.Instance != null)
        {
            MTB.Instance.UpdateStats(damageMultiplier, miningSpeedMultiplier, collectionRangeMultiplier, fireRateMultiplier);
        }
        
        // Обновляем инвентарь
        if (Inventory.Instance != null)
        {
            Inventory.Instance.UpdateCapacity(inventoryCapacity);
        }
    }
    
    private void SaveTechProgress()
    {
        foreach (var tree in new[] { forgeTechTree, farmTechTree, generalTechTree })
        {
            if (tree != null)
            {
                foreach (var node in tree.nodes)
                {
                    PlayerPrefs.SetInt($"Tech_{node.nodeId}", node.isUnlocked ? 1 : 0);
                }
            }
        }
        PlayerPrefs.Save();
    }
    
    private void LoadTechProgress()
    {
        foreach (var tree in new[] { forgeTechTree, farmTechTree, generalTechTree })
        {
            if (tree != null)
            {
                foreach (var node in tree.nodes)
                {
                    node.isUnlocked = PlayerPrefs.GetInt($"Tech_{node.nodeId}", 0) == 1;
                    unlockedTechs[node.nodeId] = node.isUnlocked;
                }
            }
        }
    }
    
    // Методы для проверки статуса технологий
    public bool IsTechUnlocked(string nodeId)
    {
        return unlockedTechs.ContainsKey(nodeId) && unlockedTechs[nodeId];
    }
    
    public int GetUnlockedTechCount(TechTree techTree)
    {
        int count = 0;
        foreach (var node in techTree.nodes)
        {
            if (node.isUnlocked) count++;
        }
        return count;
    }
}