using UnityEngine;

namespace Gameplay.Buildings
{
    public class Building : MonoBehaviour
    {
        [Header("Building Settings")]
        [SerializeField] private string buildingId;
        [SerializeField] private string buildingName;
        [SerializeField] private BuildingLevel[] levels;
    
        private int _currentLevel;

        [System.Serializable]
        public class BuildingLevel
        {
            public GameObject levelModel;
            public string description;
        }

        private void Start()
        {
            if (BuildingManager.Instance != null)
            {
                BuildingManager.Instance.RegisterBuilding(this);
            }
        
            SetLevel(0);
        }

        private void OnDestroy()
        {
            if (BuildingManager.Instance != null)
            {
                BuildingManager.Instance.UnregisterBuilding(this);
            }
        }

        public void SetLevel(int level)
        {
            if (level < 0 || level >= levels.Length) return;

            foreach (var buildingLevel in levels)
            {
                if (buildingLevel.levelModel != null)
                    buildingLevel.levelModel.SetActive(false);
            }

            _currentLevel = level;
        
            if (levels[_currentLevel].levelModel != null)
                levels[_currentLevel].levelModel.SetActive(true);
        }

        public int GetCurrentLevel() => _currentLevel;
        public int GetMaxLevel() => levels.Length;
        public string GetBuildingId() => buildingId;
        public string GetLevelDescription(int level) 
        { 
            return (level >= 0 && level < levels.Length) ? levels[level].description : ""; 
        }
        public string GetBuildingName() => buildingName;
    }
}