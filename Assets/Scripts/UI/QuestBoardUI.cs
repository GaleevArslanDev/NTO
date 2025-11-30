using Gameplay.Systems;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
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
    
        private QuestSystem.Quest _selectedQuest;
        private bool _isNpcMode;
        public bool isUIOpen;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
        
            closeButton.onClick.AddListener(CloseQuestUI);
            acceptButton.onClick.AddListener(AcceptSelectedQuest);
            completeButton.onClick.AddListener(CompleteSelectedQuest);
        }

        private void Start()
        {
            if (QuestSystem.Instance != null)
            {
                QuestSystem.Instance.OnQuestsUpdated += RefreshQuestsUI;
            }
        
            questBoardPanel.SetActive(false);
            npcModePanel.SetActive(false);
        
            RefreshQuestsUI();
        }

        private void OnDestroy()
        {
            if (QuestSystem.Instance != null)
            {
                QuestSystem.Instance.OnQuestsUpdated -= RefreshQuestsUI;
            }
        }

        private void Update()
        {
            // Горячая клавиша для просмотра (только если не у NPC)
            if (Input.GetKeyDown(toggleKey) && !_isNpcMode && !isUIOpen)
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
            _isNpcMode = npcMode;
            questPanel.SetActive(true);
            npcModePanel.SetActive(npcMode);

            if (npcMode && npcModeTitle != null)
            {
                npcModeTitle.text = LocalizationManager.LocalizationManager.Instance.GetString("quests-npc", npcName);
            }

            if (UIManager.Instance != null)
                if (!isUIOpen) 
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
            _isNpcMode = false;
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
            if (_selectedQuest == null || _selectedQuest.isActive || !_isNpcMode) return;
            QuestSystem.Instance.AcceptQuest(_selectedQuest.questId);
            RefreshQuestsUI();
        }
    
        private void CompleteSelectedQuest()
        {
            if (_selectedQuest is not { isActive: true } || !_isNpcMode) return;
            QuestSystem.Instance.CompleteQuest(_selectedQuest.questId);
            RefreshQuestsUI();
        }
    
        public void OnQuestSelected(QuestSystem.Quest quest)
        {
            _selectedQuest = quest;
            UpdateActionButtons();
        }
    
        private void UpdateActionButtons()
        {
            if (_selectedQuest != null && _isNpcMode)
            {
                acceptButton.interactable = !_selectedQuest.isActive && !_selectedQuest.isCompleted;
                completeButton.interactable = _selectedQuest.isActive && !_selectedQuest.isCompleted;
            
                acceptButton.GetComponentInChildren<TMP_Text>().text = 
                    _selectedQuest.isCompleted ? LocalizationManager.LocalizationManager.Instance.GetString("completed") : LocalizationManager.LocalizationManager.Instance.GetString("accept");
            }
            else
            {
                acceptButton.interactable = false;
                completeButton.interactable = false;
            }
        }
    
        private void CreateQuestEntry(QuestSystem.Quest quest, Transform container, bool showProgress)
        {
            var entry = Instantiate(questEntryPrefab, container);
            var entryUI = entry.GetComponent<QuestEntryUI>();
            if (entryUI == null) return;
            entryUI.Initialize(quest, showProgress, _isNpcMode);
            entryUI.OnQuestSelected += OnQuestSelected;
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

        private void ToggleQuestBoard()
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

        private void OpenQuestBoard()
        {
            questBoardPanel.SetActive(true);
            _isNpcMode = false;
            npcModePanel.SetActive(false);

            if (UIManager.Instance != null)
                if (!isUIOpen) 
                    UIManager.Instance.RegisterUIOpen();
            isUIOpen = true;
            RefreshQuestsUI();
        }

        private void CloseQuestBoard()
        {
            questBoardPanel.SetActive(false);
            isUIOpen = false;

            if (UIManager.Instance != null)
                UIManager.Instance.RegisterUIClose();
        }
    }
}