using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class TownHall : MonoBehaviour
{
    public static TownHall Instance { get; private set; }
    
    [Header("Town Hall Settings")]
    [SerializeField] private int _maxLevel = 5;
    [SerializeField] private TownHallLevel[] _levels;
    
    [Header("Events")]
    public UnityEvent OnUpgradeStarted;
    public UnityEvent OnUpgradeCompleted;
    public UnityEvent<int> OnLevelChanged;
    
    private int _currentLevel = 0;
    private bool _isUpgrading = false;
    private int _unlockedTier = 1;

    [System.Serializable]
    public class TownHallLevel
    {
        [Header("Requirements")]
        public ResourceCost[] RequiredResources;
        public float UpgradeTime = 0f;
    
        [Header("Visuals")]
        public GameObject LevelModel;
    
        [Header("Unlocks")]
        public BuildingUpgrade[] BuildingUpgrades;
        public int UnlocksTechTier = 1; // Какой тир технологий открывает этот уровень
    }

    [System.Serializable]
    public class BuildingUpgrade
    {
        public string BuildingId;
        public int NewLevel;
    }

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
        // Регистрируем начальный уровень
        UpdateVisualModel();
    }

    public void Upgrade()
    {
        if (_isUpgrading || !CanUpgrade()) return;

        _isUpgrading = true;
        SpendResources();
        OnUpgradeStarted?.Invoke();

        if (_levels[_currentLevel].UpgradeTime > 0)
            Invoke(nameof(CompleteUpgrade), _levels[_currentLevel].UpgradeTime);
        else
            CompleteUpgrade();
    }

    public bool CanUpgrade()
    {
        if (_currentLevel >= _maxLevel) return false;
        if (_currentLevel >= _levels.Length) return false;

        foreach (var cost in _levels[_currentLevel].RequiredResources)
        {
            if (Inventory.Instance.GetItemCount(cost.Type) < cost.Amount)
                return false;
        }
        return true;
    }

    private void SpendResources()
    {
        foreach (var cost in _levels[_currentLevel].RequiredResources)
        {
            Inventory.Instance.RemoveItem(cost.Type, cost.Amount);
        }
    }

    private void CompleteUpgrade()
    {
        if (_currentLevel > 0 && _levels[_currentLevel - 1].LevelModel != null)
            _levels[_currentLevel - 1].LevelModel.SetActive(false);

        _currentLevel++;
        
        UpdateVisualModel();
        
        ApplyBuildingUpgrades();
        
        _isUpgrading = false;
        OnUpgradeCompleted?.Invoke();
        OnLevelChanged?.Invoke(_currentLevel);
    }

    private void UpdateVisualModel()
    {
        // Отключаем все модели
        for (int i = 0; i < _levels.Length; i++)
        {
            if (_levels[i].LevelModel != null)
                _levels[i].LevelModel.SetActive(false);
        }
        
        // Включаем модель текущего уровня
        if (_currentLevel > 0 && _currentLevel - 1 < _levels.Length && _levels[_currentLevel - 1].LevelModel != null)
            _levels[_currentLevel - 1].LevelModel.SetActive(true);
    }

    private void ApplyBuildingUpgrades()
    {
        var levelData = _levels[_currentLevel - 1];
        foreach (var upgrade in levelData.BuildingUpgrades)
        {
            var building = BuildingManager.Instance.GetBuilding(upgrade.BuildingId);
            if (building != null)
            {
                building.SetLevel(upgrade.NewLevel);
            }
        }
    }

    public ResourceCost[] GetCurrentLevelCosts()
    {
        if (_currentLevel >= _levels.Length) return null;
        return _levels[_currentLevel].RequiredResources;
    }

    public int GetCurrentLevel() => _currentLevel;
    public int GetMaxLevel() => _maxLevel;
    public bool IsMaxLevel() => _currentLevel >= _maxLevel;
    
    // Новый метод для системы прокачки
    public int GetUnlockedTechTier() => _unlockedTier;
    
    public void UnlockUpgradeTier(int tier)
    {
        _unlockedTier = Mathf.Max(_unlockedTier, tier);
    }
}