using UnityEngine;

public class Building : MonoBehaviour
{
    [Header("Building Settings")]
    [SerializeField] private string _buildingId; // Уникальный ID для идентификации
    [SerializeField] private string _buildingName;
    [SerializeField] private BuildingLevel[] _levels;
    
    private int _currentLevel = 0;

    [System.Serializable]
    public class BuildingLevel
    {
        public GameObject LevelModel;
        public string Description; // Описание улучшения
    }

    void Start()
    {
        // Регистрируем здание в менеджере
        if (BuildingManager.Instance != null)
        {
            BuildingManager.Instance.RegisterBuilding(this);
        }
        
        // Инициализируем начальный уровень
        SetLevel(0);
    }

    void OnDestroy()
    {
        // Отменяем регистрацию при уничтожении
        if (BuildingManager.Instance != null)
        {
            BuildingManager.Instance.UnregisterBuilding(this);
        }
    }

    public void SetLevel(int level)
    {
        if (level < 0 || level >= _levels.Length) return;

        // Отключаем все модели
        foreach (var buildingLevel in _levels)
        {
            if (buildingLevel.LevelModel != null)
                buildingLevel.LevelModel.SetActive(false);
        }

        _currentLevel = level;
        
        // Включаем модель текущего уровня
        if (_levels[_currentLevel].LevelModel != null)
            _levels[_currentLevel].LevelModel.SetActive(true);
    }

    public int GetCurrentLevel() => _currentLevel;
    public int GetMaxLevel() => _levels.Length;
    public string GetBuildingId() => _buildingId;
    public string GetLevelDescription(int level) 
    { 
        return (level >= 0 && level < _levels.Length) ? _levels[level].Description : ""; 
    }
    public string GetBuildingName() => _buildingName;
}