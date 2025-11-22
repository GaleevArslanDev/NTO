using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class QuestBoardUI : MonoBehaviour
{
    [Header("UI References")]
    public GameObject questBoardPanel;
    public Transform availableQuestsContainer;
    public Transform activeQuestsContainer;
    public Transform completedQuestsContainer;
    public GameObject questEntryPrefab;
    
    [Header("Input Settings")]
    public KeyCode toggleKey = KeyCode.Q;
    
    public bool isUIOpen = false;
    
    void Start()
    {
        if (QuestSystem.Instance != null)
        {
            QuestSystem.Instance.OnQuestsUpdated += RefreshQuestsUI;
        }
        
        // Скрываем панель при старте
        questBoardPanel.SetActive(false);
        
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
        // Обработка клавиши для открытия/закрытия
        if (Input.GetKeyDown(toggleKey))
        {
            ToggleQuestBoard();
        }
        
        // ESC для закрытия
        if (isUIOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            CloseQuestBoard();
        }
    }
    
    public void RefreshQuestsUI()
    {
        if (QuestSystem.Instance == null) return;
        
        ClearContainers();
        
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
    
    private void CreateQuestEntry(QuestSystem.Quest quest, Transform container, bool showProgress)
    {
        GameObject entry = Instantiate(questEntryPrefab, container);
        QuestEntryUI entryUI = entry.GetComponent<QuestEntryUI>();
        if (entryUI != null)
        {
            entryUI.Initialize(quest, showProgress);
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
        
        // Разблокируем курсор
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        
        // Обновляем квесты при открытии
        RefreshQuestsUI();
    }
    
    public void CloseQuestBoard()
    {
        questBoardPanel.SetActive(false);
        isUIOpen = false;
        
        // Возвращаем курсор в игровой режим
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}