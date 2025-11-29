using System;
using System.Collections.Generic;
using System.Linq;
using Core;
using Data.Game;
using Data.Tech;
using Gameplay.Buildings;
using Gameplay.Items;
using Gameplay.Systems;
using UI;
using UnityEngine;

namespace Gameplay.Characters.Player
{
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
        public int collectedAmountMultiplier = 1;
    
        private Dictionary<string, bool> _unlockedTechs = new();

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public void Initialize()
        {
            // Устанавливаем начальные значения по умолчанию
            ResetTechProgress();
            
            // Применяем все разблокированные улучшения (если есть)
            ApplyAllUnlockedTechs();
        }
    
        public bool CanUnlockTech(string nodeId, TechTree techTree)
        {
            var node = techTree.nodes.Find(n => n.nodeId == nodeId);
            if (node == null) return false;
    
            // Проверяем уровень ратуши
            var currentTier = TownHall.Instance.GetUnlockedTechTier();
            if (node.tier > currentTier)
            {
                return false;
            }

            // Проверяем пройдены ли пререквизиты
            return node.prerequisiteNodes.All(IsTechUnlocked) &&
                   // Проверяем хватает ли ресурсов
                   Inventory.Instance.HasResources(node.unlockCost);
        }
    
        public bool UnlockTech(string nodeId, TechTree techTree)
        {
            if (!CanUnlockTech(nodeId, techTree)) return false;
    
            var node = techTree.nodes.Find(n => n.nodeId == nodeId);
    
            // Тратим ресурсы
            foreach (var cost in node.unlockCost)
            {
                Inventory.Instance.RemoveItem(cost.type, cost.amount);
            }
    
            // Разблокируем
            node.isUnlocked = true;
            _unlockedTechs[nodeId] = true;
    
            // Применяем эффекты
            ApplyTechEffects(node);
    
            // Сохраняем прогресс через SaveManager
            if (SaveManager.Instance != null)
                SaveManager.Instance.AutoSave();
    
            Debug.Log($"Технология разблокирована: {node.nodeName}");
        
            if (AIAssistant.Instance != null)
            {
                AIAssistant.Instance.OnTechUnlocked(node.nodeName);
            }
            
            if (SaveManager.Instance != null)
                SaveManager.Instance.AutoSave();
        
            return true;
        }
    
        private void ApplyTechEffects(TechNode node)
        {
            foreach (var effect in node.effects)
            {
                switch (effect.effectType)
                {
                    case EffectType.FireRateMultiplier:
                        fireRateMultiplier *= effect.floatValue;
                        UpdateMtbStats();
                        break;
                    case EffectType.DamageMultiplier:
                        damageMultiplier *= effect.floatValue;
                        UpdateMtbStats();
                        break;
                    
                    case EffectType.MiningSpeedMultiplier:
                        miningSpeedMultiplier *= effect.floatValue;
                        UpdateMtbStats();
                        break;
                    
                    case EffectType.InventoryCapacity:
                        inventoryCapacity = effect.intValue;
                        UpdateMtbStats();
                        break;
                    
                    case EffectType.CollectionRangeMultiplier:
                        collectionRangeMultiplier *= effect.floatValue;
                        UpdateMtbStats();
                        break;
                    case EffectType.CollectedAmountMultiplier:
                        collectedAmountMultiplier = effect.intValue;
                        UpdateMtbStats();
                        break;
                    case EffectType.PassiveIncome:
                        // Активируем пассивный доход
                        if (FarmManager.Instance != null)
                            FarmManager.Instance.ActivatePassiveIncome(effect.stringValue, effect.floatValue);
                        break;
                    
                    case EffectType.UnlockBuilding:
                        // Разблокируем здание
                        if (BuildingManager.Instance != null)
                            BuildingManager.Instance.UnlockBuilding(effect.stringValue);
                        break;
                    
                    case EffectType.UnlockUpgradeTier:
                        // Открываем новый тир улучшений
                        if (TownHall.Instance != null)
                            TownHall.Instance.UnlockUpgradeTier(effect.intValue);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }
    
        private void ApplyAllUnlockedTechs()
        {
            // Применяем эффекты всех разблокированных технологий
            foreach (var tree in new[] { forgeTechTree, farmTechTree, generalTechTree })
            {
                if (tree == null) continue;
                foreach (var node in tree.nodes.Where(node => node.isUnlocked))
                {
                    ApplyTechEffects(node);
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
        
        public Dictionary<string, bool> GetUnlockedTechsDictionary()
        {
            return _unlockedTechs;
        }

        public void ApplyUnlockedTechs(Dictionary<string, bool> unlockedTechs)
        {
            _unlockedTechs = unlockedTechs ?? new Dictionary<string, bool>();

            // Применяем разблокированные технологии
            ApplyAllUnlockedTechs();
        }

        public TechSaveData GetTechSaveData()
        {
            var saveData = new TechSaveData();
            
            // Сохраняем все разблокированные технологии из всех деревьев
            var trees = new[] { forgeTechTree, farmTechTree, generalTechTree };
            foreach (var tree in trees)
            {
                if (tree != null)
                {
                    foreach (var node in tree.nodes)
                    {
                        saveData.unlockedNodes[node.nodeId] = node.isUnlocked;
                    }
                }
            }
            
            return saveData;
        }

        public void ApplyTechSaveData(TechSaveData data)
        {
            if (data == null) return;
    
            // Сначала сбрасываем все технологии
            ResetTechProgress();
    
            // Затем применяем сохраненные состояния
            var trees = new[] { forgeTechTree, farmTechTree, generalTechTree };
            foreach (var tree in trees)
            {
                if (tree != null)
                {
                    foreach (var node in tree.nodes)
                    {
                        if (data.unlockedNodes.ContainsKey(node.nodeId))
                        {
                            node.isUnlocked = data.unlockedNodes[node.nodeId];
                            _unlockedTechs[node.nodeId] = node.isUnlocked;
                        }
                    }
                }
            }
    
            // Применяем эффекты разблокированных технологий
            ApplyAllUnlockedTechs();
        }
    
        private void UpdateMtbStats()
        {
            // Обновляем характеристики MTB
            if (Mtb.Instance != null)
            {
                Mtb.Instance.UpdateStats(damageMultiplier, miningSpeedMultiplier, collectionRangeMultiplier, fireRateMultiplier);
            }
        
            // Обновляем инвентарь
            if (Inventory.Instance != null)
            {
                Inventory.Instance.UpdateCapacity(inventoryCapacity);
            }
        }
    
        // Методы для проверки статуса технологий
        public bool IsTechUnlocked(string nodeId)
        {
            return _unlockedTechs.ContainsKey(nodeId) && _unlockedTechs[nodeId];
        }
    
        public int GetUnlockedTechCount(TechTree techTree)
        {
            return techTree.nodes.Count(node => node.isUnlocked);
        }
        
        public void ResetTechProgress()
        {
            _unlockedTechs.Clear();
            
            // Сбрасываем все технологии в заблокированное состояние
            var trees = new[] { forgeTechTree, farmTechTree, generalTechTree };
            foreach (var tree in trees)
            {
                if (tree != null)
                {
                    foreach (var node in tree.nodes)
                    {
                        node.isUnlocked = false;
                    }
                }
            }
            
            // Сбрасываем статы к начальным значениям
            damageMultiplier = 1f;
            fireRateMultiplier = 1f;
            miningSpeedMultiplier = 1f;
            inventoryCapacity = 50;
            collectionRangeMultiplier = 1f;
            
            UpdateMtbStats();
        }
    }
}