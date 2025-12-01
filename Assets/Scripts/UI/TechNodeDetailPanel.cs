using System.Linq;
using Core;
using Data.Tech;
using Gameplay.Buildings;
using Gameplay.Characters.Player;
using Gameplay.Items;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class TechNodeDetailsPanel : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject detailsPanel;
        [SerializeField] private Image nodeIcon;
        [SerializeField] private TMP_Text nodeNameText;
        [SerializeField] private TMP_Text descriptionText;
        [SerializeField] private TMP_Text tierText;
        [SerializeField] private Transform requirementsContainer;
        [SerializeField] private Transform effectsContainer;
        [SerializeField] private Transform prerequisitesContainer;
        [SerializeField] private Button unlockButton;
        [SerializeField] private TMP_Text unlockButtonText;
        [SerializeField] private TMP_Text statusText;

        [Header("Prefabs")]
        [SerializeField] private GameObject requirementPrefab;
        [SerializeField] private GameObject effectPrefab;
        [SerializeField] private GameObject prerequisitePrefab;

        private TechNode _currentNode;
        private TechTree _currentTree;
        
        private bool _canUpgrade;

        private void Start()
        {
            unlockButton.onClick.AddListener(OnUnlockButtonClicked);
            HideDetails();
        }

        public void ShowDetails(TechNode node, TechTree tree, bool canUpgrade)
        {
            _currentNode = node;
            _currentTree = tree;
            _canUpgrade = canUpgrade;

            UpdateDetailsUI();
            detailsPanel.SetActive(true);
        }

        public void HideDetails()
        {
            detailsPanel.SetActive(false);
            _currentNode = null;
            _currentTree = null;
        }

        private void UpdateDetailsUI()
        {
            if (_currentNode == null)
            {
                detailsPanel.SetActive(false);
                return;
            }

            // Основная информация
            nodeIcon.sprite = _currentNode.icon;
            nodeNameText.text = _currentNode.GetLocalizedName();
            descriptionText.text = _currentNode.GetLocalizedDescription();
            tierText.text = LocalizationManager.LocalizationManager.Instance.GetString("tech-node-detail-panel_tier-label")+_currentNode.tier.ToString();

            // Очищаем контейнеры
            ClearContainer(requirementsContainer);
            ClearContainer(effectsContainer);
            ClearContainer(prerequisitesContainer);

            // Требования (ресурсы)
            foreach (var cost in _currentNode.unlockCost)
            {
                var requirement = Instantiate(requirementPrefab, requirementsContainer);
                var requirementUI = requirement.GetComponent<ResourceRequirementUI>();
                if (requirementUI != null)
                {
                    requirementUI.Set(cost.type, cost.amount);
                }
            }

            // Эффекты
            foreach (var effect in _currentNode.effects)
            {
                var effectObj = Instantiate(effectPrefab, effectsContainer);
                var effectText = effectObj.GetComponentInChildren<TMP_Text>();
                if (effectText != null)
                {
                    effectText.text = GetEffectDescription(effect);
                }
            }

            // Пререквизиты
            foreach (var prereqId in _currentNode.prerequisiteNodes)
            {
                var prereqNode = _currentTree.nodes.Find(n => n.nodeId == prereqId);
                if (prereqNode != null)
                {
                    var prereqObj = Instantiate(prerequisitePrefab, prerequisitesContainer);
                    var prereqText = prereqObj.GetComponentInChildren<TMP_Text>();
                    if (prereqText != null)
                    {
                        var status = prereqNode.isUnlocked ? "✓" : "✗";
                        prereqText.text = $"{status} {prereqNode.GetLocalizedName()}";
                        prereqText.color = prereqNode.isUnlocked ? Color.green : Color.red;
                    }
                }
            }

            // Статус и кнопка
            UpdateStatusAndButton();
        }

        private string GetEffectDescription(TechEffect effect)
        {
            return effect.effectType switch
            {
                EffectType.FireRateMultiplier => LocalizationManager.LocalizationManager.Instance.GetString("tech-node-detail-panel_fire-rate-multiplier-label", ((effect.floatValue - 1) * 100).ToString("F0")),
                EffectType.DamageMultiplier => LocalizationManager.LocalizationManager.Instance.GetString("tech-node-detail-panel_damage-multiplier-label", ((effect.floatValue - 1) * 100).ToString("F0")),
                EffectType.MiningSpeedMultiplier => LocalizationManager.LocalizationManager.Instance.GetString("tech-node-detail-panel_mining-speed-label", ((effect.floatValue - 1) * 100).ToString("F0")),
                EffectType.InventoryCapacity => LocalizationManager.LocalizationManager.Instance.GetString("tech-node-detail-panel_inventory-capacity-label", effect.intValue.ToString()),
                EffectType.CollectionRangeMultiplier => LocalizationManager.LocalizationManager.Instance.GetString("tech-node-detail-panel_collection-range-multiplier-label", ((effect.floatValue - 1) * 100).ToString("F0")),
                EffectType.PassiveIncome => LocalizationManager.LocalizationManager.Instance.GetString("tech-node-detail-panel_passive-income-label", effect.stringValue, effect.floatValue.ToString()),
                EffectType.UnlockBuilding => LocalizationManager.LocalizationManager.Instance.GetString("tech-node-detail-panel_unlock-building-label", effect.stringValue),
                EffectType.UnlockUpgradeTier =>LocalizationManager.LocalizationManager.Instance.GetString("tech-node-detail-panel_unlock-upgrade-tier-label", effect.intValue.ToString()),
                _ => effect.effectType.ToString()
            };
        }

        private void UpdateStatusAndButton()
        {
            if (_currentNode.isUnlocked)
            {
                statusText.text = LocalizationManager.LocalizationManager.Instance.GetString("tech-node_unlocked");
                unlockButton.interactable = false;
                unlockButtonText.text = LocalizationManager.LocalizationManager.Instance.GetString("researched");
                return;
            }

            var canUnlock = CanUnlockNode();
            var hasPrerequisites = CheckPrerequisites();

            if (!hasPrerequisites)
            {
                statusText.text = LocalizationManager.LocalizationManager.Instance.GetString("tech-node_no-prerequisites");
                unlockButton.interactable = false;
                unlockButtonText.text = LocalizationManager.LocalizationManager.Instance.GetString("unavailable");
            }
            else if (!canUnlock)
            {
                statusText.text = LocalizationManager.LocalizationManager.Instance.GetString("tech-node_no-resources");
                unlockButton.interactable = false;
                unlockButtonText.text = LocalizationManager.LocalizationManager.Instance.GetString("unavailable");
            }
            else
            {
                statusText.text = LocalizationManager.LocalizationManager.Instance.GetString("tech-node_available");
                unlockButton.interactable = true;
                unlockButtonText.text = LocalizationManager.LocalizationManager.Instance.GetString("upgrade");
            }

            // Проверяем уровень ратуши
            var townHallTier = TownHall.Instance.GetUnlockedTechTier();
            if (_currentNode.tier > townHallTier)
            {
                statusText.text = LocalizationManager.LocalizationManager.Instance.GetString("tech-node_no-town-hall-tier", _currentNode.tier.ToString());
                unlockButton.interactable = false;
                unlockButtonText.text = LocalizationManager.LocalizationManager.Instance.GetString("unavailable");
            }

            if (!_canUpgrade)
            {
                unlockButton.interactable = false;
                unlockButtonText.text = LocalizationManager.LocalizationManager.Instance.GetString("tech-node_available_in_town");
            }
        }

        private bool CanUnlockNode()
        {
            return _currentNode.unlockCost.All(cost => 
                Inventory.Instance.GetItemCount(cost.type) >= cost.amount);
        }

        private bool CheckPrerequisites()
        {
            return _currentNode.prerequisiteNodes.All(prereqId =>
            {
                var prereqNode = _currentTree.nodes.Find(n => n.nodeId == prereqId);
                return prereqNode != null && prereqNode.isUnlocked;
            });
        }

        private void OnUnlockButtonClicked()
        {
            if (_currentNode == null || _currentTree == null) return;

            if (PlayerProgression.Instance.UnlockTech(_currentNode.nodeId, _currentTree))
            {
                UpdateDetailsUI();
                // Обновляем основное дерево
                if (TechTreeUI.Instance != null)
                {
                    TechTreeUI.Instance.RefreshTreeUI();
                }
            }
        }

        private static void ClearContainer(Transform container)
        {
            foreach (Transform child in container)
            {
                Destroy(child.gameObject);
            }
        }

        public bool IsVisible => detailsPanel.activeInHierarchy;
    }
}