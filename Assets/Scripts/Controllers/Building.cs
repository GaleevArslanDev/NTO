using System;
using UnityEngine;
using UnityEngine.Events;

public class Building : MonoBehaviour
{
    [SerializeField] private int _maxLevel = 3;
    [SerializeField] private BuildingLevel[] _levels;
    [SerializeField] private GameObject _buildingModel;
    
    [Header("Events")]
    public UnityEvent OnUpgradeStarted;
    public UnityEvent OnUpgradeCompleted;
    
    private int _currentLevel = 0;
    private bool _isUpgrading = false;

    [System.Serializable]
    public class BuildingLevel
    {
        public ItemCost[] RequiredResources;
        public float UpgradeTime = 0f;
        public GameObject LevelModel;
    }

    [System.Serializable]
    public class ItemCost
    {
        public ItemType Type;
        public int Amount;
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

    // Сделал метод публичным
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
        // Отключаем предыдущую модель
        if (_currentLevel > 0 && _levels[_currentLevel - 1].LevelModel != null)
            _levels[_currentLevel - 1].LevelModel.SetActive(false);

        _currentLevel++;
        _isUpgrading = false;
        UpdateVisualModel();
        OnUpgradeCompleted?.Invoke();
    }

    private void UpdateVisualModel()
    {
        if (_buildingModel != null)
            _buildingModel.SetActive(false);

        if (_currentLevel > 0 && _levels[_currentLevel - 1].LevelModel != null)
            _levels[_currentLevel - 1].LevelModel.SetActive(true);
    }

    // Добавил метод для получения стоимости текущего уровня улучшения
    public ItemCost[] GetCurrentLevelCosts()
    {
        if (_currentLevel >= _levels.Length) return null;
        return _levels[_currentLevel].RequiredResources;
    }

    public int GetCurrentLevel() => _currentLevel + 1;
    public int GetMaxLevel() => _maxLevel;
    public bool IsMaxLevel() => _currentLevel >= _maxLevel;
}