using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class TownHallUI : MonoBehaviour
{
    public static TownHallUI Instance { get; private set; }
    
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
    [SerializeField] private string[] _levelDescriptions;

    private TownHall _townHall;
    public bool isUiOpen = false;

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

    void Start()
    {
        _upgradeButton.onClick.AddListener(OnUpgradeButtonClicked);
        _closeButton.onClick.AddListener(HideDialog);
        
        HideDialog();
    }

    public void ShowDialog(TownHall townHall)
    {
        isUiOpen = true;
        _townHall = townHall;
        UpdateDialog();
        _dialogPanel.SetActive(true);

        // Используем UIManager
        if (UIManager.Instance != null)
            UIManager.Instance.RegisterUIOpen();
    }

    public void HideDialog()
    {
        isUiOpen = false;
        _dialogPanel.SetActive(false);

        // Используем UIManager
        if (UIManager.Instance != null)
            UIManager.Instance.RegisterUIClose();
    }

    private void UpdateDialog()
    {
        if (_townHall == null) {Debug.Log("no town hall");return;}

        int currentLevel = _townHall.GetCurrentLevel();
        int nextLevel = currentLevel + 1;
    
        _titleText.text = string.Format(_titleFormat, currentLevel);
    
        string description = nextLevel <= _townHall.GetMaxLevel() && nextLevel - 1 < _levelDescriptions.Length 
            ? _levelDescriptions[nextLevel - 1] 
            : "Максимальный уровень достигнут.";
        _descriptionText.text = description;
    
        _levelText.text = $"Текущий уровень: {currentLevel} / {_townHall.GetMaxLevel()}";

        ClearContainer(_requirementsContainer);
        ClearContainer(_upgradesContainer);

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
        Invoke(nameof(UpdateDialog), 0.1f);
    }
}