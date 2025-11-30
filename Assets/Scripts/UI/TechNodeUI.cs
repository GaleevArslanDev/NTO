using System;
using System.Linq;
using Data.Tech;
using Gameplay.Buildings;
using Gameplay.Characters.Player;
using Gameplay.Items;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class TechNodeUI : MonoBehaviour
    {
        [Header("UI Elements")]
        public Image nodeIcon;
        public TMP_Text nodeNameText;
        public TMP_Text costText;
        public Button unlockButton;
        public Image background;
        public GameObject lockedOverlay;
    
        private TechNode _node;
        private TechTree _tree;
        public event Action OnNodeUnlocked;
        public event Action<TechNode> OnNodeSelected;
        private bool _isUnlocked;
        private bool _canUnlock = true;
    
        public void Initialize(TechNode node, TechTree tree)
        {
            _node = node;
            _tree = tree;
            _isUnlocked = node.isUnlocked;
    
            nodeNameText.text = node.GetLocalizedName();
            nodeIcon.sprite = node.icon;
    
            // Отображаем стоимость
            var costString = node.unlockCost.Aggregate("", (current, cost) => current + $"{cost.type}: {cost.amount}\n");
            costText.text = costString;
    
            unlockButton.onClick.RemoveAllListeners();
            unlockButton.onClick.AddListener(OnUnlockButtonClicked);
    
            var nodeButton = GetComponent<Button>();
            if (nodeButton == null) nodeButton = gameObject.AddComponent<Button>();
            nodeButton.onClick.RemoveAllListeners();
            nodeButton.onClick.AddListener(OnNodeClicked);
    
            UpdateUI();
        }
    
        public void SetAccess(bool canUnlockTech)
        {
            _canUnlock = canUnlockTech;
            UpdateUI();
        }
    
        public void SetViewMode()
        {
            // В режиме просмотра отключаем кнопку разблокировки
            unlockButton.interactable = false;
            unlockButton.gameObject.SetActive(false);
        }
    
        private void OnNodeClicked()
        {
            OnNodeSelected?.Invoke(_node);
        }
    
        public void UpdateUI()
        {
            if (_node == null || PlayerProgression.Instance == null) return;

            // В режиме просмотра всегда запрещаем прокачку
            var canUnlockNode = TechTreeUI.Instance != null && 
                                TechTreeUI.Instance.allowUnlock && 
                                PlayerProgression.Instance.CanUnlockTech(_node.nodeId, _tree);

            unlockButton.interactable = canUnlockNode && !_isUnlocked;
            lockedOverlay.SetActive(!_isUnlocked);

            if (_isUnlocked)
            {
                costText.text = LocalizationManager.LocalizationManager.Instance.GetString("tech-node_unlocked");
                unlockButton.gameObject.SetActive(false);
                unlockButton.interactable = false;
                return;
            }
    
            // Показываем стоимость
            var costString = "";
            foreach (var cost in _node.unlockCost)
            {
                var hasEnough = Inventory.Instance.GetItemCount(cost.type) >= cost.amount;
                var color = hasEnough ? "green" : "red";
                costString += $"<color={color}>{LocalizationManager.LocalizationManager.Instance.GetString(cost.type.ToString())}: {cost.amount}</color>\n";
            }
            costText.text = costString;
    
            if (!canUnlockNode)
            {
                // Показываем причину почему нельзя разблокировать
                string reason;
                if (_node.tier > TownHall.Instance.GetUnlockedTechTier())
                {
                    reason = LocalizationManager.LocalizationManager.Instance.GetString("tech-node_no-town-hall-tier", _node.tier.ToString());
                }
                else if (!TechTreeUI.Instance.allowUnlock)
                {
                    reason = LocalizationManager.LocalizationManager.Instance.GetString("tech-node_available_in_town");
                }
                else
                {
                    reason = LocalizationManager.LocalizationManager.Instance.GetString("tech-node_no-prerequisites");
                }
                costText.text = reason;
            }
        }
    
        private bool CheckPrerequisites()
        {
            return _node.prerequisiteNodes.All(prereq => PlayerProgression.Instance.IsTechUnlocked(prereq));
        }
    
        public void OnUnlockButtonClicked()
        {
            if (!PlayerProgression.Instance.UnlockTech(_node.nodeId, _tree)) return;
            _isUnlocked = true;
            UpdateUI();
            OnNodeUnlocked?.Invoke();
        }
    
        public bool IsUnlocked()
        {
            return _isUnlocked;
        }
    }
}