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
        private NpcBehaviour _npcBehaviour;

        private void Start()
        {
            _npcBehaviour = GetComponent<NpcBehaviour>();
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
        
        public void StartSpecificDialogue(string treeName)
        {
            if (DialogueManager.Instance.IsInDialogue) return;
            
            _npcBehaviour?.SetDialogueState(true);
        
            DialogueManager.Instance.StartDialogue(this, treeName);
        }

        public void OnDialogueEnded()
        {
            _npcBehaviour?.SetDialogueState(false);
            Debug.Log($"Диалог с {npcData.npcName} завершен");
        }

        public void OpenServices()
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
                            LocalizationManager.LocalizationManager.Instance.GetString("npc-interaction_blacksmith")
                        );
                    }
                    break;
            
                case NpcType.Farmer:
                    if (TechTreeUI.Instance != null && PlayerProgression.Instance != null)
                    {
                        TechTreeUI.Instance.ShowTechTreeForNpc(
                            PlayerProgression.Instance.farmTechTree, 
                            LocalizationManager.LocalizationManager.Instance.GetString("npc-interaction_farmer")
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

        public string GetNpcType()
        {
            return npcType.ToString();
        }

        public string GetNpcName()
        {
            return npcType switch
            {
                NpcType.Mayor => LocalizationManager.LocalizationManager.Instance.GetString("npc_mayor"),
                NpcType.Blacksmith => LocalizationManager.LocalizationManager.Instance.GetString("npc_blacksmith"), 
                NpcType.Farmer => LocalizationManager.LocalizationManager.Instance.GetString("npc_farmer"),
                NpcType.QuestGiver => LocalizationManager.LocalizationManager.Instance.GetString("npc_questgiver"),
                _ => "NPC"
            };
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