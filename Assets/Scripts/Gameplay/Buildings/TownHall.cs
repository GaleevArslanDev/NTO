using System.Linq;
using Data.Game;
using Data.Tech;
using Gameplay.Items;
using Gameplay.Systems;
using UI;
using UnityEngine;
using UnityEngine.Events;

namespace Gameplay.Buildings
{
    public class TownHall : MonoBehaviour
    {
        public static TownHall Instance { get; private set; }
    
        [Header("Tech Tree")]
        public TechTree townHallTechTree;
    
        [Header("Town Hall Settings")]
        [SerializeField] private int maxLevel = 5;
        [SerializeField] private TownHallLevel[] levels;
        [SerializeField] private GameObject baseModel;
    
        [Header("Events")]
        public UnityEvent onUpgradeStarted;
        public UnityEvent onUpgradeCompleted;
        public UnityEvent<int> onLevelChanged;
    
        private int _currentLevel;
        private bool _isUpgrading;
        private int _unlockedTier = 1;

        [System.Serializable]
        public class TownHallLevel
        {
            [Header("Requirements")]
            public ResourceCost[] requiredResources;
            public float upgradeTime;
    
            [Header("Visuals")]
            public GameObject levelModel;
    
            [Header("Unlocks")]
            public BuildingUpgrade[] buildingUpgrades;
            public int unlocksTechTier = 1; // Какой тир технологий открывает этот уровень
        }
    
        [System.Serializable]
        public class TownHallLevelUnlock
        {
            public int level;
            public string[] unlockedBuildings;
            public int unlockedTechTier;
        }
    
        public TownHallLevelUnlock[] levelUnlocks;
    
        public TownHallLevelUnlock GetUnlocksForLevel(int level)
        {
            return levelUnlocks.FirstOrDefault(unlock => unlock.level == level);
        }

        [System.Serializable]
        public class BuildingUpgrade
        {
            public string buildingId;
            public int newLevel;
        }

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
            // Регистрируем начальный уровень
            UpdateVisualModel();
        }

        public void Upgrade()
        {
            if (_isUpgrading || !CanUpgrade()) return;

            _isUpgrading = true;
            SpendResources();
            onUpgradeStarted?.Invoke();

            if (levels[_currentLevel].upgradeTime > 0)
                Invoke(nameof(CompleteUpgrade), levels[_currentLevel].upgradeTime);
            else
                CompleteUpgrade();
        }

        public bool CanUpgrade()
        {
            if (_currentLevel >= maxLevel) return false;
            return _currentLevel < levels.Length && levels[_currentLevel].requiredResources.All(cost => Inventory.Instance.GetItemCount(cost.type) >= cost.amount);
        }

        private void SpendResources()
        {
            foreach (var cost in levels[_currentLevel].requiredResources)
            {
                Inventory.Instance.RemoveItem(cost.type, cost.amount);
            }
        }

        private void CompleteUpgrade()
        {
            if (_currentLevel > 0 && levels[_currentLevel - 1].levelModel != null)
                levels[_currentLevel - 1].levelModel.SetActive(false);

            UnlockUpgradeTier(levels[_currentLevel].unlocksTechTier);

            _currentLevel++;

            UpdateVisualModel();
        
            ApplyBuildingUpgrades();
        
            AIAssistant.Instance.OnBuildingUpgraded("TownHall");
        
            _isUpgrading = false;
            onUpgradeCompleted?.Invoke();
            onLevelChanged?.Invoke(_currentLevel);
            
            if (SaveManager.Instance != null)
                SaveManager.Instance.AutoSave();
        }

        private void UpdateVisualModel()
        {
            baseModel.SetActive(_currentLevel == 0);
            // Отключаем все модели
            foreach (var t in levels)
            {
                if (t.levelModel != null)
                    t.levelModel.SetActive(false);
            }

            // Включаем модель текущего уровня
            if (_currentLevel > 0 && _currentLevel - 1 < levels.Length && levels[_currentLevel - 1].levelModel != null)
                levels[_currentLevel - 1].levelModel.SetActive(true);
        }

        private void ApplyBuildingUpgrades()
        {
            var levelData = levels[_currentLevel - 1];
            foreach (var upgrade in levelData.buildingUpgrades)
            {
                var building = BuildingManager.Instance.GetBuilding(upgrade.buildingId);
                if (building != null)
                {
                    building.SetLevel(upgrade.newLevel);
                }
            }
        }

        public ResourceCost[] GetCurrentLevelCosts()
        {
            return _currentLevel >= levels.Length ? null : levels[_currentLevel].requiredResources;
        }
        
        public void SetLevel(int level) => _currentLevel = level;

        public int GetCurrentLevel() => _currentLevel;
        public int GetMaxLevel() => maxLevel;
        public bool IsMaxLevel() => _currentLevel >= maxLevel;
    
        // Новый метод для системы прокачки
        public int GetUnlockedTechTier() => _unlockedTier;
    
        public void UnlockUpgradeTier(int tier)
        {
            _unlockedTier = Mathf.Max(_unlockedTier, tier);
        }
    }
}