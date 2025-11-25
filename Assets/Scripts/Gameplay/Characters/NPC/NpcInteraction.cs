using System;
using Core;
using Data.NPC;
using Gameplay.Buildings;
using Gameplay.Characters.Player;
using Gameplay.Dialogue;
using UI;
using UnityEngine;

namespace Gameplay.Characters.NPC
{
    public class NpcInteraction : MonoBehaviour
    {
        [Header("NPC Type")]
        public NpcType npcType;
    
        [Header("Dialogue Settings")]
        public NpcData npcData;
        public DialogueTree[] dialogueTrees;
        public Sprite portrait;
        public AudioClip defaultVoice;
    
        [Header("UI References")]
        public GameObject interactionPrompt;
    
        private bool _isPlayerInRange;

        private void Start()
        {
            // Регистрация в менеджерах
            if (npcData == null) return;
            foreach (var tree in dialogueTrees)
            {
                if (tree != null)
                    DialogueManager.Instance.LoadDialogueTree(tree);
            }
            
            if (RelationshipManager.Instance != null)
            {
                RelationshipManager.Instance.RegisterNpc(npcData);
            }
        }

        private void Update()
        {
            if (!_isPlayerInRange) return;
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

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player")) return;
            _isPlayerInRange = true;
            ShowInteractionPrompt();
        }

        private void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag("Player")) return;
            _isPlayerInRange = false;
            HideInteractionPrompt();
            CloseAllUI();
        }

        public void StartDialogueWithPlayer()
        {
            if (DialogueManager.Instance.IsInDialogue || npcData == null) return;
        
            var dialogueStarted = DialogueManager.Instance.StartDialogue(this);
        
            if (!dialogueStarted)
            {
                Debug.Log($"Не удалось начать диалог с {npcData.npcName}");
            }
        }

        public void StartSpecificDialogue(string treeName)
        {
            if (DialogueManager.Instance.IsInDialogue) return;
        
            DialogueManager.Instance.StartDialogue(this, treeName);
        }

        public void OnDialogueEnded()
        {
            Debug.Log($"Диалог с {npcData.npcName} завершен");
        }

        private void OpenServices()
        {
            switch (npcType)
            {
                case NpcType.Mayor:
                    // Открываем окно ратуши вместо дерева технологий
                    if (TownHallUI.Instance != null && TownHall.Instance != null)
                    {
                        TownHallUI.Instance.ShowDialog(TownHall.Instance);
                    }
                    break;
            
                case NpcType.Blacksmith:
                    if (TechTreeUI.Instance != null && PlayerProgression.Instance != null)
                    {
                        TechTreeUI.Instance.ShowTechTreeForNpc(
                            PlayerProgression.Instance.forgeTechTree, 
                            "Брук (Кузница)"
                        );
                    }
                    break;
            
                case NpcType.Farmer:
                    if (TechTreeUI.Instance != null && PlayerProgression.Instance != null)
                    {
                        TechTreeUI.Instance.ShowTechTreeForNpc(
                            PlayerProgression.Instance.farmTechTree, 
                            "Горк (Ферма)"
                        );
                    }
                    break;
            
                case NpcType.QuestGiver:
                    OpenQuestUI();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void OpenTownHallUI()
        {
            // Для мэра открываем общее дерево, но с возможностью прокачки
            if (TechTreeUI.Instance != null && PlayerProgression.Instance != null)
            {
                TechTreeUI.Instance.ShowTechTreeForNpc(
                    PlayerProgression.Instance.generalTechTree, 
                    "Зол (Ратуша)"
                );
            }
        }

        private void OpenQuestUI()
        {
            if (QuestBoardUI.Instance != null)
            {
                QuestBoardUI.Instance.ShowQuestUI(true, GetNpcName());
            }
        }

        private void ShowInteractionPrompt()
        {
            if (interactionPrompt == null) return;
            interactionPrompt.SetActive(true);
            UpdatePromptText();
        }

        private void HideInteractionPrompt()
        {
            if (interactionPrompt != null)
                interactionPrompt.SetActive(false);
        }

        private void UpdatePromptText()
        {
            var textMesh = interactionPrompt.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            if (textMesh == null) return;
            var npcName = GetNpcName();
            textMesh.text = $"{npcName}\nE - Поговорить\nF - Услуги";
        }

        private string GetNpcName()
        {
            if (npcData != null)
                return npcData.npcName;
        
            return npcType switch
            {
                NpcType.Mayor => "Зол",
                NpcType.Blacksmith => "Брук", 
                NpcType.Farmer => "Горк",
                NpcType.QuestGiver => "Лип",
                _ => "NPC"
            };
        }

        private void CloseAllUI()
        {
            var townHallUI = FindObjectOfType<TownHallUI>();
            if (townHallUI != null)
                townHallUI.HideDialog();
            
            if (TechTreeUI.Instance != null)
                TechTreeUI.Instance.CloseTechTree();
            
            if (QuestBoardUI.Instance != null)
                QuestBoardUI.Instance.CloseQuestUI();
        }

        private void OnDestroy()
        {
            if (DialogueManager.Instance == null) return;
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