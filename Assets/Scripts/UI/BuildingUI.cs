using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BuildingUI : MonoBehaviour
{
    [SerializeField] private GameObject _uiPanel;
    [SerializeField] private TextMeshProUGUI _levelText;
    [SerializeField] private Button _upgradeButton;
    [SerializeField] private Transform _requirementsContainer;
    [SerializeField] private GameObject _resourceRequirementPrefab;

    private Building _targetBuilding;

    void Start()
    {
        _upgradeButton.onClick.AddListener(OnUpgradeButtonClicked);
        HideUI();
    }

    public void ShowForBuilding(Building building)
    {
        _targetBuilding = building;
        UpdateUI();
        _uiPanel.SetActive(true);
    }

    public void HideUI()
    {
        _uiPanel.SetActive(false);
    }

    private void UpdateUI()
    {
        if (_targetBuilding == null) return;

        _levelText.text = $"Уровень: {_targetBuilding.GetCurrentLevel()}/{_targetBuilding.GetMaxLevel()}";
        
        // Очищаем контейнер требований
        foreach (Transform child in _requirementsContainer)
            Destroy(child.gameObject);

        // Показываем требования для следующего уровня
        if (!_targetBuilding.IsMaxLevel())
        {
            var costs = _targetBuilding.GetCurrentLevelCosts();
            if (costs != null)
            {
                foreach (var cost in costs)
                {
                    var requirement = Instantiate(_resourceRequirementPrefab, _requirementsContainer);
                    requirement.GetComponent<ResourceRequirementUI>().Set(cost.Type, cost.Amount);
                }
            }
        }
        else
        {
            // Если достигнут максимальный уровень
            var text = Instantiate(_resourceRequirementPrefab, _requirementsContainer);
            text.GetComponentInChildren<TextMeshProUGUI>().text = "Макс. уровень достигнут";
        }

        _upgradeButton.interactable = _targetBuilding.CanUpgrade() && !_targetBuilding.IsMaxLevel();
        _upgradeButton.GetComponentInChildren<TextMeshProUGUI>().text = 
            _targetBuilding.IsMaxLevel() ? "Макс. уровень" : "Улучшить";
    }

    private void OnUpgradeButtonClicked()
    {
        _targetBuilding?.Upgrade();
        // Обновляем UI через небольшую задержку, чтобы инвентарь успел обновиться
        Invoke(nameof(UpdateUI), 0.1f);
    }
}