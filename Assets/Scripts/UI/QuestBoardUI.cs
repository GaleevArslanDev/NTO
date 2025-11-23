using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class QuestBoardUI : MonoBehaviour
{
    public static QuestBoardUI Instance;
    
    [Header("UI References")]
    public GameObject questPanel;
    public GameObject questBoardPanel;
    public Transform availableQuestsContainer;
    public Transform activeQuestsContainer;
    public Transform completedQuestsContainer;
    public GameObject questEntryPrefab;
    
    [Header("Input Settings")]
    public KeyCode toggleKey = KeyCode.Q;
    
    [Header("NPC Mode")]
    public GameObject npcModePanel;
    public Button acceptButton;
    public Button completeButton;
    public Button closeButton;
    public TMP_Text npcModeTitle;
    
    private QuestSystem.Quest selectedQuest;
    private bool isNPCMode = false;
    public bool isUIOpen = false;
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        
        closeButton.onClick.AddListener(CloseQuestUI);
        acceptButton.onClick.AddListener(AcceptSelectedQuest);
        completeButton.onClick.AddListener(CompleteSelectedQuest);
    }
    
    void Start()
    {
        if (QuestSystem.Instance != null)
        {
            QuestSystem.Instance.OnQuestsUpdated += RefreshQuestsUI;
        }
        
        questBoardPanel.SetActive(false);
        npcModePanel.SetActive(false);
        
        RefreshQuestsUI();
    }
    
    void OnDestroy()
    {
        if (QuestSystem.Instance != null)
        {
            QuestSystem.Instance.OnQuestsUpdated -= RefreshQuestsUI;
        }
    }
    
    void Update()
    {
        // Горячая клавиша для просмотра (только если не у NPC)
        if (Input.GetKeyDown(toggleKey) && !isNPCMode && !isUIOpen)
        {
            ToggleQuestBoard();
        }
        
        if (isUIOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            CloseQuestUI();
        }
    }
    
    public void ShowQuestUI(bool npcMode = false, string npcName = "NPC")
    {
        isNPCMode = npcMode;
        questPanel.SetActive(true);
        npcModePanel.SetActive(npcMode);

        if (npcMode && npcModeTitle != null)
        {
            npcModeTitle.text = $"Квесты - {npcName}";
        }

        if (UIManager.Instance != null)
            UIManager.Instance.RegisterUIOpen();

        isUIOpen = true;
        RefreshQuestsUI();
    }
    
    public void CloseQuestUI()
    {
        questPanel.SetActive(false);
        npcModePanel.SetActive(false);

        if (UIManager.Instance != null)
            UIManager.Instance.RegisterUIClose();

        isUIOpen = false;
        isNPCMode = false;
    }
    
    private void RefreshQuestsUI()
    {
        UpdateQuestsList();
        UpdateActionButtons();
    }
    
    private void UpdateQuestsList()
    {
        ClearContainers();
        
        if (QuestSystem.Instance == null) return;
        
        // Доступные квесты
        foreach (var quest in QuestSystem.Instance.GetAvailableQuests())
        {
            CreateQuestEntry(quest, availableQuestsContainer, false);
        }
        
        // Активные квесты
        foreach (var quest in QuestSystem.Instance.GetActiveQuests())
        {
            CreateQuestEntry(quest, activeQuestsContainer, true);
        }
        
        // Завершенные квесты
        foreach (var quest in QuestSystem.Instance.GetCompletedQuests())
        {
            CreateQuestEntry(quest, completedQuestsContainer, false);
        }
    }
    
    private void AcceptSelectedQuest()
    {
        if (selectedQuest != null && !selectedQuest.isActive && isNPCMode)
        {
            QuestSystem.Instance.AcceptQuest(selectedQuest.questId);
            RefreshQuestsUI();
        }
    }
    
    private void CompleteSelectedQuest()
    {
        if (selectedQuest != null && selectedQuest.isActive && isNPCMode)
        {
            QuestSystem.Instance.CompleteQuest(selectedQuest.questId);
            RefreshQuestsUI();
        }
    }
    
    public void OnQuestSelected(QuestSystem.Quest quest)
    {
        selectedQuest = quest;
        UpdateActionButtons();
    }
    
    private void UpdateActionButtons()
    {
        if (selectedQuest != null && isNPCMode)
        {
            acceptButton.interactable = !selectedQuest.isActive && !selectedQuest.isCompleted;
            completeButton.interactable = selectedQuest.isActive && !selectedQuest.isCompleted;
            
            acceptButton.GetComponentInChildren<TMP_Text>().text = 
                selectedQuest.isCompleted ? "Завершено" : "Принять";
        }
        else
        {
            acceptButton.interactable = false;
            completeButton.interactable = false;
        }
    }
    
    private void CreateQuestEntry(QuestSystem.Quest quest, Transform container, bool showProgress)
    {
        GameObject entry = Instantiate(questEntryPrefab, container);
        QuestEntryUI entryUI = entry.GetComponent<QuestEntryUI>();
        if (entryUI != null)
        {
            entryUI.Initialize(quest, showProgress, isNPCMode);
            entryUI.OnQuestSelected += OnQuestSelected;
        }
    }
    
    private void ClearContainers()
    {
        ClearContainer(availableQuestsContainer);
        ClearContainer(activeQuestsContainer);
        ClearContainer(completedQuestsContainer);
    }
    
    private void ClearContainer(Transform container)
    {
        if (container == null) return;
        
        foreach (Transform child in container)
        {
            Destroy(child.gameObject);
        }
    }
    
    public void ToggleQuestBoard()
    {
        if (isUIOpen)
        {
            CloseQuestBoard();
        }
        else
        {
            OpenQuestBoard();
        }
    }
    
    public void OpenQuestBoard()
    {
        questBoardPanel.SetActive(true);
        isUIOpen = true;
        isNPCMode = false;
        npcModePanel.SetActive(false);

        if (UIManager.Instance != null)
            UIManager.Instance.RegisterUIOpen();

        RefreshQuestsUI();
    }
    
    public void CloseQuestBoard()
    {
        questBoardPanel.SetActive(false);
        isUIOpen = false;

        if (UIManager.Instance != null)
            UIManager.Instance.RegisterUIClose();
    }
}