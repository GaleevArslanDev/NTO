using System;
using Gameplay.Buildings;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class TownHallUI : MonoBehaviour
    {
        public static TownHallUI Instance { get; private set; }
    
        [Header("UI References")]
        [SerializeField] private GameObject dialogPanel;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private TextMeshProUGUI levelText;
        [SerializeField] private Button upgradeButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private Transform requirementsContainer;
        [SerializeField] private GameObject resourceRequirementPrefab;
        [SerializeField] private Transform upgradesContainer;
        [SerializeField] private GameObject upgradeInfoPrefab;

        [Header("Texts")]
        [SerializeField] private string titleFormat = "Ратуша (Уровень {0})";
        [SerializeField] private string descriptionFormat = "Улучшение ратуши до уровня {0} откроет новые возможности для вашего поселения.";
        [SerializeField] private string[] levelDescriptions;

        private TownHall _townHall;
        public bool isUiOpen;

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
        }

        private void Update()
        {
            if (isUiOpen && Input.GetKeyDown(KeyCode.Escape))
            {
                HideDialog();
            }
        }

        private void Start()
        {
            upgradeButton.onClick.AddListener(OnUpgradeButtonClicked);
            closeButton.onClick.AddListener(HideDialog);
        
            HideDialog();
        }

        public void ShowDialog(TownHall townHall)
        {
            _townHall = townHall;
            UpdateDialog();
            dialogPanel.SetActive(true);

            // Используем UIManager
            if (UIManager.Instance != null)
                if (!isUiOpen)
                    UIManager.Instance.RegisterUIOpen();
            isUiOpen = true;
        }

        public void HideDialog()
        {
            isUiOpen = false;
            dialogPanel.SetActive(false);

            // Используем UIManager
            if (UIManager.Instance != null)
                UIManager.Instance.RegisterUIClose();
        }

        private void UpdateDialog()
        {
            if (_townHall == null) {Debug.Log("no town hall");return;}

            var currentLevel = _townHall.GetCurrentLevel();
            var nextLevel = currentLevel + 1;
    
            titleText.text = string.Format(titleFormat, currentLevel);
    
            var description = nextLevel <= _townHall.GetMaxLevel() && nextLevel - 1 < levelDescriptions.Length 
                ? levelDescriptions[nextLevel - 1] 
                : "Максимальный уровень достигнут.";
            descriptionText.text = description;
    
            levelText.text = $"Текущий уровень: {currentLevel} / {_townHall.GetMaxLevel()}";

            ClearContainer(requirementsContainer);
            ClearContainer(upgradesContainer);

            if (!_townHall.IsMaxLevel())
            {
                var costs = _townHall.GetCurrentLevelCosts();
                if (costs != null)
                {
                    foreach (var cost in costs)
                    {
                        var requirement = Instantiate(resourceRequirementPrefab, requirementsContainer);
                        requirement.GetComponent<ResourceRequirementUI>().Set(cost.type, cost.amount);
                    }
                }

                ShowBuildingUpgrades(nextLevel);
            }
            else
            {
                var text = Instantiate(resourceRequirementPrefab, requirementsContainer);
                text.GetComponentInChildren<TextMeshProUGUI>().text = "Максимальный уровень достигнут";
            }

            upgradeButton.interactable = _townHall.CanUpgrade() && !_townHall.IsMaxLevel();
            upgradeButton.GetComponentInChildren<TextMeshProUGUI>().text = 
                _townHall.IsMaxLevel() ? "Макс. уровень" : $"Улучшить до уровня {nextLevel}";
        }

        private void ShowBuildingUpgrades(int nextLevel)
        {
            var buildings = BuildingManager.Instance.GetAllBuildings();
        
            foreach (var building in buildings)
            {
                if (building.GetCurrentLevel() >= nextLevel || nextLevel > building.GetMaxLevel()) continue;
                var upgradeInfo = Instantiate(upgradeInfoPrefab, upgradesContainer);
                upgradeInfo.GetComponent<BuildingUpgradeInfoUI>().Set(
                    building.GetBuildingName(),
                    building.GetCurrentLevel(), 
                    nextLevel,
                    building.GetLevelDescription(nextLevel)
                );
            }
        }

        private static void ClearContainer(Transform container)
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
}