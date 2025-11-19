using UnityEngine;

public class Building : MonoBehaviour
{
    [Header("Building Settings")]
    [SerializeField] private string _buildingId;
    [SerializeField] private string _buildingName;
    [SerializeField] private BuildingLevel[] _levels;
    
    private int _currentLevel = 0;

    [System.Serializable]
    public class BuildingLevel
    {
        public GameObject LevelModel;
        public string Description;
    }

    void Start()
    {
        if (BuildingManager.Instance != null)
        {
            BuildingManager.Instance.RegisterBuilding(this);
        }
        
        SetLevel(0);
    }

    void OnDestroy()
    {
        if (BuildingManager.Instance != null)
        {
            BuildingManager.Instance.UnregisterBuilding(this);
        }
    }

    public void SetLevel(int level)
    {
        if (level < 0 || level >= _levels.Length) return;

        foreach (var buildingLevel in _levels)
        {
            if (buildingLevel.LevelModel != null)
                buildingLevel.LevelModel.SetActive(false);
        }

        _currentLevel = level;
        
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