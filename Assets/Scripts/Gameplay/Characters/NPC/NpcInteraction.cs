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
        
        [Header("Reactive Dialogue")]
        public ReactiveDialogueTrigger reactiveTrigger;
    
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
            
            if (reactiveTrigger == null)
                reactiveTrigger = GetComponent<ReactiveDialogueTrigger>();
        
            if (reactiveTrigger != null)
            {
                reactiveTrigger.OnPlayerResponded += OnPlayerRespondedToCall;
                reactiveTrigger.OnPlayerIgnored += OnPlayerIgnoredCall;
            }
        }
        
        private void OnPlayerRespondedToCall()
        {
            Debug.Log($"{npcData.npcName}: Игрок откликнулся на мой зов!");
        }
        
        private void OnPlayerIgnoredCall()
        {
            Debug.Log($"{npcData.npcName}: Игрок проигнорировал меня...");
    
            if (AIAssistant.Instance != null)
            {
                AIAssistant.Instance.OnNpcIgnored(npcData.npcName);
            }
        }

        private void Update()
        {
            if (!_isPlayerInRange) return;
    
            // E - ТОЛЬКО реактивный диалог (когда NPC зовет)
            if (Input.GetKeyDown(KeyCode.E))
            {
                if (reactiveTrigger != null && reactiveTrigger.IsCalling)
                {
                    reactiveTrigger.TriggerDialogue();
                }
                // Убрана возможность начать диалог по E без вызова
            }
            // F - прокачка/услуги (без изменений)
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

        // УБРАН публичный метод StartDialogueWithPlayer - диалоги теперь только реактивные
        
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

            if (reactiveTrigger != null && reactiveTrigger.IsCalling)
            {
                textMesh.text = $"{npcName} (зовет)\nE - Ответить\nF - Услуги";
            }
            else
            {
                // Только услуги, диалог недоступен
                textMesh.text = $"{npcName}\nF - Услуги";
            }
        }

        public string GetNpcName()
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
            
            if (reactiveTrigger != null)
            {
                reactiveTrigger.OnPlayerResponded -= OnPlayerRespondedToCall;
                reactiveTrigger.OnPlayerIgnored -= OnPlayerIgnoredCall;
            }
        }
    }
}