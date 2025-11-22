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
    private bool isUnlocked = false;
    
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
        
        UpdateUI();
    }
    
    public void UpdateUI()
    {
        if (node == null || PlayerProgression.Instance == null) return;

        bool canUnlock = PlayerProgression.Instance.CanUnlockTech(node.nodeId, tree);
    
        // Отладочная информация
        Debug.Log($"Узел {node.nodeName}: canUnlock={canUnlock}, isUnlocked={isUnlocked}, Tier={node.tier}, TownHallTier={TownHall.Instance.GetUnlockedTechTier()}");
    
        unlockButton.interactable = canUnlock && !isUnlocked;
        lockedOverlay.SetActive(!isUnlocked);

        if (isUnlocked)
        {
            costText.text = "Разблокирован";
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
        
        if(!canUnlock)
        {
            background.color = Color.gray;
        
            // Показываем почему нельзя разблокировать
            string reason = "";
            if (node.tier > TownHall.Instance.GetUnlockedTechTier())
            {
                reason = $"Требуется уровень ратуши: {node.tier}";
            }
            else
            {
                reason = costString;
            }
            costText.text = reason;
        }
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