using UnityEngine;

public class NPCInteraction : MonoBehaviour
{
    [Header("NPC Type")]
    public NPCType npcType;
    
    [Header("Dialogue Settings")]
    public NPCData NPCData;
    public DialogueTree[] dialogueTrees;
    public Sprite portrait;
    public AudioClip defaultVoice;
    
    [Header("UI References")]
    public GameObject interactionPrompt;
    
    private bool isPlayerInRange = false;
    
    public enum NPCType
    {
        Mayor,      // Зол - Ратуша
        Blacksmith, // Брук - Кузница  
        Farmer,     // Горк - Ферма
        QuestGiver  // Лип - Квесты
    }

    void Start()
    {
        // Регистрация в менеджерах
        if (NPCData != null)
        {
            foreach (var tree in dialogueTrees)
            {
                if (tree != null)
                    DialogueManager.Instance.LoadDialogueTree(tree);
            }
            
            if (RelationshipManager.Instance != null)
            {
                RelationshipManager.Instance.RegisterNPC(NPCData);
            }
        }
    }

    void Update()
    {
        if (isPlayerInRange)
        {
            // E - диалог
            if (Input.GetKeyDown(KeyCode.E))
            {
                StartDialogueWithPlayer();
            }
            // F - прокачка/услуги
            else if (Input.GetKeyDown(KeyCode.F))
            {
                OpenServices();
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = true;
            ShowInteractionPrompt();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = false;
            HideInteractionPrompt();
            CloseAllUI();
        }
    }

    public void StartDialogueWithPlayer()
    {
        if (DialogueManager.Instance.IsInDialogue || NPCData == null) return;
        
        bool dialogueStarted = DialogueManager.Instance.StartDialogue(this);
        
        if (!dialogueStarted)
        {
            Debug.Log($"Не удалось начать диалог с {NPCData.npcName}");
        }
    }

    public void StartSpecificDialogue(string treeName)
    {
        if (DialogueManager.Instance.IsInDialogue) return;
        
        DialogueManager.Instance.StartDialogue(this, treeName);
    }

    public void OnDialogueEnded()
    {
        Debug.Log($"Диалог с {NPCData.npcName} завершен");
    }

    private void OpenServices()
    {
        switch (npcType)
        {
            case NPCType.Mayor:
                // Открываем окно ратуши вместо дерева технологий
                if (TownHallUI.Instance != null && TownHall.Instance != null)
                {
                    TownHallUI.Instance.ShowDialog(TownHall.Instance);
                }
                break;
            
            case NPCType.Blacksmith:
                if (TechTreeUI.Instance != null && PlayerProgression.Instance != null)
                {
                    TechTreeUI.Instance.ShowTechTreeForNPC(
                        PlayerProgression.Instance.forgeTechTree, 
                        "Брук (Кузница)"
                    );
                }
                break;
            
            case NPCType.Farmer:
                if (TechTreeUI.Instance != null && PlayerProgression.Instance != null)
                {
                    TechTreeUI.Instance.ShowTechTreeForNPC(
                        PlayerProgression.Instance.farmTechTree, 
                        "Горк (Ферма)"
                    );
                }
                break;
            
            case NPCType.QuestGiver:
                OpenQuestUI();
                break;
        }
    }

    private void OpenTownHallUI()
    {
        // Для мэра открываем общее дерево, но с возможностью прокачки
        if (TechTreeUI.Instance != null && PlayerProgression.Instance != null)
        {
            TechTreeUI.Instance.ShowTechTreeForNPC(
                PlayerProgression.Instance.generalTechTree, 
                "Зол (Ратуша)"
            );
        }
    }

    private void OpenQuestUI()
    {
        if (QuestBoardUI.Instance != null)
        {
            QuestBoardUI.Instance.ShowQuestUI(true, GetNPCName());
        }
    }

    private void ShowInteractionPrompt()
    {
        if (interactionPrompt != null)
        {
            interactionPrompt.SetActive(true);
            UpdatePromptText();
        }
    }

    private void HideInteractionPrompt()
    {
        if (interactionPrompt != null)
            interactionPrompt.SetActive(false);
    }

    private void UpdatePromptText()
    {
        var textMesh = interactionPrompt.GetComponentInChildren<TMPro.TextMeshProUGUI>();
        if (textMesh != null)
        {
            string npcName = GetNPCName();
            textMesh.text = $"{npcName}\nE - Поговорить\nF - Услуги";
        }
    }

    private string GetNPCName()
    {
        if (NPCData != null)
            return NPCData.npcName;
        
        return npcType switch
        {
            NPCType.Mayor => "Зол",
            NPCType.Blacksmith => "Брук", 
            NPCType.Farmer => "Горк",
            NPCType.QuestGiver => "Лип",
            _ => "NPC"
        };
    }

    private void CloseAllUI()
    {
        TownHallUI townHallUI = FindObjectOfType<TownHallUI>();
        if (townHallUI != null)
            townHallUI.HideDialog();
            
        if (TechTreeUI.Instance != null)
            TechTreeUI.Instance.CloseTechTree();
            
        if (QuestBoardUI.Instance != null)
            QuestBoardUI.Instance.CloseQuestUI();
    }

    private void OnDestroy()
    {
        if (DialogueManager.Instance != null)
        {
            foreach (var tree in dialogueTrees)
            {
                if (tree != null)
                {
                    DialogueManager.Instance.UnloadDialogueTree(tree.treeName);
                }
            }
        }
    }
}