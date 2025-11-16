using UnityEngine;

public class BuildingInteractor : MonoBehaviour
{
    [SerializeField] private LayerMask _buildingLayer;
    [SerializeField] private float _interactionDistance = 3f;
    
    private Camera _mainCamera;
    private Building _currentBuilding;
    private BuildingUI _buildingUI;

    void Start()
    {
        _mainCamera = Camera.main;
        _buildingUI = FindObjectOfType<BuildingUI>();
    }

    void Update()
    {
        HandleBuildingSelection();
    }

    private void HandleBuildingSelection()
    {
        Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, _interactionDistance, _buildingLayer))
        {
            Building building = hit.collider.GetComponent<Building>();
            if (building != null)
            {
                if (Input.GetMouseButtonDown(0)) // ЛКМ для выбора
                {
                    _currentBuilding = building;
                    _buildingUI.ShowForBuilding(building);
                }
            }
        }

        // ESC для закрытия UI
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            _buildingUI.HideUI();
            _currentBuilding = null;
        }
    }
}