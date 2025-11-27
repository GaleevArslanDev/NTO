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
            nodeNameText.text = _currentNode.nodeName;
            descriptionText.text = _currentNode.description;
            tierText.text = $"Тир: {_currentNode.tier}";

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
                        prereqText.text = $"{status} {prereqNode.nodeName}";
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
                EffectType.FireRateMultiplier => $"Скорость стрельбы: +{(effect.floatValue - 1) * 100:F0}%",
                EffectType.DamageMultiplier => $"Урон: +{(effect.floatValue - 1) * 100:F0}%",
                EffectType.MiningSpeedMultiplier => $"Скорость добычи: +{(effect.floatValue - 1) * 100:F0}%",
                EffectType.InventoryCapacity => $"Вместимость инвентаря: {effect.intValue}",
                EffectType.CollectionRangeMultiplier => $"Радиус сбора: +{(effect.floatValue - 1) * 100:F0}%",
                EffectType.PassiveIncome => $"Пассивный доход: {effect.stringValue} ({effect.floatValue}/мин)",
                EffectType.UnlockBuilding => $"Разблокирует: {effect.stringValue}",
                EffectType.UnlockUpgradeTier => $"Открывает тир улучшений: {effect.intValue}",
                _ => effect.effectType.ToString()
            };
        }

        private void UpdateStatusAndButton()
        {
            if (_currentNode.isUnlocked)
            {
                statusText.text = "<color=green>✓ Разблокировано</color>";
                unlockButton.interactable = false;
                unlockButtonText.text = "Изучено";
                return;
            }

            var canUnlock = CanUnlockNode();
            var hasPrerequisites = CheckPrerequisites();

            if (!hasPrerequisites)
            {
                statusText.text = "<color=red>✗ Не выполнены требования</color>";
                unlockButton.interactable = false;
                unlockButtonText.text = "Недоступно";
            }
            else if (!canUnlock)
            {
                statusText.text = "<color=red>✗ Недостаточно ресурсов</color>";
                unlockButton.interactable = false;
                unlockButtonText.text = "Недоступно";
            }
            else
            {
                statusText.text = "<color=green>✓ Доступно для изучения</color>";
                unlockButton.interactable = true;
                unlockButtonText.text = "Изучить";
            }

            // Проверяем уровень ратуши
            var townHallTier = TownHall.Instance.GetUnlockedTechTier();
            if (_currentNode.tier > townHallTier)
            {
                statusText.text = $"<color=red>✗ Требуется уровень ратуши: {_currentNode.tier}</color>";
                unlockButton.interactable = false;
                unlockButtonText.text = "Недоступно";
            }

            if (!_canUpgrade)
            {
                unlockButton.interactable = false;
                unlockButtonText.text = "Доступно только у NPC";
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