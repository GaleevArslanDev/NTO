using UnityEngine;

public class TownHallInteractor : MonoBehaviour
{
    [SerializeField] private LayerMask _townHallLayer;
    [SerializeField] private float _interactionDistance = 3f;
    
    private Camera _mainCamera;
    private TownHall _currentTownHall;
    private TownHallUI _townHallUI;

    void Start()
    {
        _mainCamera = Camera.main;
        _townHallUI = FindObjectOfType<TownHallUI>();
    }

    void Update()
    {
        HandleTownHallInteraction();
    }

    private void HandleTownHallInteraction()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, _interactionDistance, _townHallLayer))
            {
                TownHall townHall = hit.collider.GetComponent<TownHall>();
                if (townHall != null)
                {
                    _currentTownHall = townHall;
                    _townHallUI.ShowDialog(townHall);
                }
            }
        }

        // ESC для закрытия диалога
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            _townHallUI.HideDialog();
        }
    }
}