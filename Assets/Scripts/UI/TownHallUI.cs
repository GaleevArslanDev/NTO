using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class TownHallUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject _dialogPanel;
    [SerializeField] private TextMeshProUGUI _titleText;
    [SerializeField] private TextMeshProUGUI _descriptionText;
    [SerializeField] private TextMeshProUGUI _levelText;
    [SerializeField] private Button _upgradeButton;
    [SerializeField] private Button _closeButton;
    [SerializeField] private Transform _requirementsContainer;
    [SerializeField] private GameObject _resourceRequirementPrefab;
    [SerializeField] private Transform _upgradesContainer;
    [SerializeField] private GameObject _upgradeInfoPrefab;

    [Header("Texts")]
    [SerializeField] private string _titleFormat = "Ратуша (Уровень {0})";
    [SerializeField] private string _descriptionFormat = "Улучшение ратуши до уровня {0} откроет новые возможности для вашего поселения.";
    [SerializeField] private string[] _levelDescriptions; // Описания для каждого уровня

    private TownHall _townHall;

    void Start()
    {
        _upgradeButton.onClick.AddListener(OnUpgradeButtonClicked);
        _closeButton.onClick.AddListener(HideDialog);
        
        HideDialog();
    }

    public void ShowDialog(TownHall townHall)
    {
        Cursor.lockState = CursorLockMode.None;
        _townHall = townHall;
        UpdateDialog();
        _dialogPanel.SetActive(true);
    }

    public void HideDialog()
    {
        Cursor.lockState = CursorLockMode.Locked;
        _dialogPanel.SetActive(false);
    }

    private void UpdateDialog()
    {
        if (_townHall == null) return;

        int currentLevel = _townHall.GetCurrentLevel();
        int nextLevel = currentLevel + 1;
    
        // Заголовок и описание
        _titleText.text = string.Format(_titleFormat, currentLevel);
    
        string description = nextLevel <= _townHall.GetMaxLevel() && nextLevel - 1 < _levelDescriptions.Length 
            ? _levelDescriptions[nextLevel - 1] 
            : "Максимальный уровень достигнут.";
        _descriptionText.text = description;
    
        _levelText.text = $"Текущий уровень: {currentLevel} / {_townHall.GetMaxLevel()}";

        // Очищаем контейнеры
        ClearContainer(_requirementsContainer);
        ClearContainer(_upgradesContainer);

        // Показываем требования для улучшения
        if (!_townHall.IsMaxLevel())
        {
            var costs = _townHall.GetCurrentLevelCosts();
            if (costs != null)
            {
                foreach (var cost in costs)
                {
                    var requirement = Instantiate(_resourceRequirementPrefab, _requirementsContainer);
                    requirement.GetComponent<ResourceRequirementUI>().Set(cost.Type, cost.Amount);
                }
            }

            // Показываем какие здания будут улучшены
            ShowBuildingUpgrades(nextLevel);
        }
        else
        {
            var text = Instantiate(_resourceRequirementPrefab, _requirementsContainer);
            text.GetComponentInChildren<TextMeshProUGUI>().text = "Максимальный уровень достигнут";
        }

        _upgradeButton.interactable = _townHall.CanUpgrade() && !_townHall.IsMaxLevel();
        _upgradeButton.GetComponentInChildren<TextMeshProUGUI>().text = 
            _townHall.IsMaxLevel() ? "Макс. уровень" : $"Улучшить до уровня {nextLevel}";
    }

    private void ShowBuildingUpgrades(int nextLevel)
    {
        // Здесь нужно получить информацию о том, какие здания будут улучшены
        // Это можно сделать через BuildingManager или напрямую из TownHall
        var buildings = BuildingManager.Instance.GetAllBuildings();
        
        foreach (var building in buildings)
        {
            if (building.GetCurrentLevel() < nextLevel && nextLevel <= building.GetMaxLevel())
            {
                var upgradeInfo = Instantiate(_upgradeInfoPrefab, _upgradesContainer);
                upgradeInfo.GetComponent<BuildingUpgradeInfoUI>().Set(
                    building.GetBuildingName(),
                    building.GetCurrentLevel(), 
                    nextLevel,
                    building.GetLevelDescription(nextLevel)
                );
            }
        }
    }

    private void ClearContainer(Transform container)
    {
        foreach (Transform child in container)
            Destroy(child.gameObject);
    }

    private void OnUpgradeButtonClicked()
    {
        _townHall?.Upgrade();
        // Обновляем диалог после улучшения
        Invoke(nameof(UpdateDialog), 0.1f);
    }
}