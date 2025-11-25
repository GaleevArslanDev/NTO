using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Gameplay.Buildings
{
    public class BuildingManager : MonoBehaviour
    {
        public static BuildingManager Instance { get; private set; }
    
        private Dictionary<string, Building> _buildings = new();
        private Dictionary<string, bool> _unlockedBuildings = new();

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
        
            // Инициализируем словарь разблокированных зданий
            InitializeUnlockedBuildings();
        }

        private void InitializeUnlockedBuildings()
        {
            // Все здания изначально заблокированы, кроме базовых
            _unlockedBuildings.Clear();
        
            // Можно добавить базовые здания, которые разблокированы с начала игры
            // _unlockedBuildings.Add("base_building", true);
        }

        public void RegisterBuilding(Building building)
        {
            var id = building.GetBuildingId();
            if (string.IsNullOrEmpty(id) || _buildings.ContainsKey(id)) return;
            // Проверяем, разблокировано ли здание
            if (IsBuildingUnlocked(id))
            {
                _buildings.Add(id, building);
                building.gameObject.SetActive(true);
                Debug.Log($"Зарегистрировано здание: {id}");
            }
            else
            {
                // Если здание не разблокировано, деактивируем его
                building.gameObject.SetActive(false);
            }
        }

        public void UnregisterBuilding(Building building)
        {
            var id = building.GetBuildingId();
            if (_buildings.ContainsKey(id))
            {
                _buildings.Remove(id);
            }
        }

        public Building GetBuilding(string buildingId)
        {
            _buildings.TryGetValue(buildingId, out var building);
            return building;
        }

        public List<Building> GetAllBuildings()
        {
            return new List<Building>(_buildings.Values);
        }
    
        // Новые методы для системы прокачки
    
        public void UnlockBuilding(string buildingId)
        {
            if (!_unlockedBuildings.TryAdd(buildingId, true)) return;
            Debug.Log($"Здание разблокировано: {buildingId}");
            
            // Активируем здание, если оно уже зарегистрировано
            if (_buildings.TryGetValue(buildingId, out var building))
            {
                building.gameObject.SetActive(true);
            }
        }

        private bool IsBuildingUnlocked(string buildingId)
        {
            // Если здание не в словаре, считаем его разблокированным (для обратной совместимости)
            return !_unlockedBuildings.ContainsKey(buildingId) || _unlockedBuildings[buildingId];
        }
    
        public List<string> GetUnlockedBuildings()
        {
            return (from kvp in _unlockedBuildings where kvp.Value select kvp.Key).ToList();
        }
    }
}