using System.Collections.Generic;
using UnityEngine;

public class BuildingManager : MonoBehaviour
{
    public static BuildingManager Instance { get; private set; }
    
    private Dictionary<string, Building> _buildings = new Dictionary<string, Building>();

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

    public void RegisterBuilding(Building building)
    {
        string id = building.GetBuildingId();
        if (!string.IsNullOrEmpty(id) && !_buildings.ContainsKey(id))
        {
            _buildings.Add(id, building);
            //Debug.Log($"Зарегистрировано здание: {id}");
        }
    }

    public void UnregisterBuilding(Building building)
    {
        string id = building.GetBuildingId();
        if (_buildings.ContainsKey(id))
        {
            _buildings.Remove(id);
        }
    }

    public Building GetBuilding(string buildingId)
    {
        _buildings.TryGetValue(buildingId, out Building building);
        return building;
    }

    public List<Building> GetAllBuildings()
    {
        return new List<Building>(_buildings.Values);
    }
}