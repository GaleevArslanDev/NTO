using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TechNodeUI : MonoBehaviour
{
    [Header("UI Elements")]
    public Image nodeIcon;
    public TMP_Text nodeNameText;
    public TMP_Text costText;
    public Button unlockButton;
    public Image background;
    public GameObject lockedOverlay;
    
    private TechNode node;
    private TechTree tree;
    public event Action OnNodeUnlocked;
    public event Action<TechNode> OnNodeSelected;
    private bool isUnlocked = false;
    private bool canUnlock = true;
    
    public void Initialize(TechNode node, TechTree tree)
    {
        this.node = node;
        this.tree = tree;
        this.isUnlocked = node.isUnlocked;
    
        nodeNameText.text = node.nodeName;
        nodeIcon.sprite = node.icon;
    
        // Отображаем стоимость
        string costString = "";
        foreach (var cost in node.unlockCost)
        {
            costString += $"{cost.Type}: {cost.Amount}\n";
        }
        costText.text = costString;
    
        unlockButton.onClick.RemoveAllListeners();
        unlockButton.onClick.AddListener(OnUnlockButtonClicked);
    
        Button nodeButton = GetComponent<Button>();
        if (nodeButton == null) nodeButton = gameObject.AddComponent<Button>();
        nodeButton.onClick.RemoveAllListeners();
        nodeButton.onClick.AddListener(OnNodeClicked);
    
        UpdateUI();
    }
    
    public void SetAccess(bool canUnlockTech)
    {
        canUnlock = canUnlockTech;
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
        OnNodeSelected?.Invoke(node);
    }
    
    public void UpdateUI()
    {
        if (node == null || PlayerProgression.Instance == null) return;

        // В режиме просмотра всегда запрещаем прокачку
        bool canUnlockNode = TechTreeUI.Instance != null && 
                             TechTreeUI.Instance.allowUnlock && 
                             PlayerProgression.Instance.CanUnlockTech(node.nodeId, tree);

        unlockButton.interactable = canUnlockNode && !isUnlocked;
        lockedOverlay.SetActive(!isUnlocked);

        if (isUnlocked)
        {
            costText.text = "Разблокирован";
            unlockButton.gameObject.SetActive(false);
            unlockButton.interactable = false;
            return;
        }
    
        // Показываем стоимость
        string costString = "";
        foreach (var cost in node.unlockCost)
        {
            bool hasEnough = Inventory.Instance.GetItemCount(cost.Type) >= cost.Amount;
            string color = hasEnough ? "green" : "red";
            costString += $"<color={color}>{cost.Type}: {cost.Amount}</color>\n";
        }
        costText.text = costString;
    
        if (!canUnlockNode)
        {
            // Показываем причину почему нельзя разблокировать
            string reason = "";
            if (node.tier > TownHall.Instance.GetUnlockedTechTier())
            {
                reason = $"Требуется уровень ратуши: {node.tier}";
            }
            else if (!TechTreeUI.Instance.allowUnlock)
            {
                reason = "Доступно только у NPC";
            }
            else
            {
                reason = "Не выполнены условия";
            }
            costText.text = reason;
        }
    }
    
    private bool CheckPrerequisites()
    {
        foreach (string prereq in node.prerequisiteNodes)
        {
            if (!PlayerProgression.Instance.IsTechUnlocked(prereq))
                return false;
        }
        return true;
    }
    
    public void OnUnlockButtonClicked()
    {
        if (PlayerProgression.Instance.UnlockTech(node.nodeId, tree))
        {
            isUnlocked = true;
            UpdateUI();
            OnNodeUnlocked?.Invoke();
        }
    }
    
    public bool IsUnlocked()
    {
        return isUnlocked;
    }
}