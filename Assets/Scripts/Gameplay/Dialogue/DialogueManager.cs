using System;
using System.Collections.Generic;
using System.Linq;
using Core;
using Data.Game;
using Data.NPC;
using Gameplay.Characters.NPC;
using Gameplay.Systems;
using UI;
using UnityEngine;

namespace Gameplay.Dialogue
{
    public class DialogueManager : MonoBehaviour
    {
        public static DialogueManager Instance;
    
        [Header("Settings")]
        public float defaultTypingSpeed = 0.05f;
    
        private Dictionary<string, DialogueTree> _loadedTrees = new();
        private DialogueTree _currentTree;
        private NpcInteraction _currentNpc;

        public Action<DialogueNode, string> OnDialogueStarted;
        public Action<DialogueNode> OnDialogueNodeChanged;
        public Action OnDialogueEnded;
    
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }
    
        public void LoadDialogueTree(DialogueTree tree)
        {
            if (tree != null)
            {
                _loadedTrees.TryAdd(tree.treeName, tree);
            }
        }
    
        public void UnloadDialogueTree(string treeName)
        {
            if (_loadedTrees.ContainsKey(treeName))
            {
                _loadedTrees.Remove(treeName);
            }
        }
    
        public bool StartDialogue(NpcInteraction npc, string treeName = null)
        {
            if (IsInDialogue || npc == null || npc.npcData == null) return false;
        
            _currentNpc = npc;
            DialogueTree treeToUse;
        
            // Если указано конкретное дерево, используем его
            if (!string.IsNullOrEmpty(treeName) && _loadedTrees.TryGetValue(treeName, out var tree))
            {
                treeToUse = tree;
            }
            else
            {
                // Ищем подходящее дерево по приоритету и условиям
                treeToUse = FindSuitableTree(npc);
            }
        
            if (treeToUse == null) return false;
        
            _currentTree = treeToUse;
            IsInDialogue = true;
        
            // Устанавливаем флаги при старте
            if (_currentTree.setFlagsOnStart != null)
            {
                foreach (var flag in _currentTree.setFlagsOnStart)
                {
                    PlayerData.Instance.SetDialogueFlag(npc.npcData.npcID, flag);
                }
            }
        
            // Находим стартовый узел
            CurrentNode = FindStartNode(_currentTree);
            if (CurrentNode == null)
            {
                EndDialogue();
                return false;
            }
        
            OnDialogueStarted?.Invoke(CurrentNode, npc.npcData.npcName);
            return true;
        }
    
        private DialogueTree FindSuitableTree(NpcInteraction npc)
        {
            var suitableTrees = _loadedTrees.Values.Where(tree => CheckTreeConditions(tree, npc.npcData)).ToList();

            // Возвращаем дерево с наивысшим приоритетом
            return suitableTrees.OrderByDescending(t => t.priority).FirstOrDefault();
        }
    
        private static bool CheckTreeConditions(DialogueTree tree, NpcData npcData)
        {
            // Проверяем глобальные условия
            if (tree.globalConditions != null && !ConditionChecker.CheckConditions(tree.globalConditions, npcData))
                return false;

            // Проверяем требуемые флаги
            return tree.requiredFlags == null || tree.requiredFlags.All(flag => PlayerData.Instance.HasDialogueFlag(npcData.npcID, flag));
        }
    
        private DialogueNode FindStartNode(DialogueTree tree)
        {
            // Сначала ищем узел с ID "start"
            var startNode = tree.nodes.FirstOrDefault(n => n.nodeID == "start" && CheckNodeConditions(n));
            return startNode ??
                   // Ищем любой подходящий узел
                   tree.nodes.FirstOrDefault(CheckNodeConditions);
        }
    
        private bool CheckNodeConditions(DialogueNode node)
        {
            return node != null && ConditionChecker.CheckConditions(node.conditions, _currentNpc.npcData);
        }
    
        public void SelectOption(DialogueOption option)
        {
            if (!IsInDialogue || _currentNpc == null) return;
        
            // Применяем последствия выбора
            ApplyOptionConsequences(option);
        
            // Переходим к следующему узлу или завершаем диалог
            if (!option.isExitOption && !string.IsNullOrEmpty(option.nextNodeID))
            {
                MoveToNode(option.nextNodeID);
            }
            else
            {
                EndDialogue();
            }
        }
    
        private void ApplyOptionConsequences(DialogueOption option)
        {
            if (_currentNpc == null) return;
        
            var npcData = _currentNpc.npcData;
            var playerData = PlayerData.Instance;
        
            // ОТЛАДКА: Выводим информацию о выборе
            Debug.Log($"   Обработка выбора диалога:");
            Debug.Log($"   NPC: {npcData.npcName} (ID: {npcData.npcID})");
            Debug.Log($"   Опция: '{option.GetLocalizedText()}'");
            Debug.Log($"   Изменение отношений: {option.relationshipChange}");
        
            // Изменяем отношения
            if (option.relationshipChange != 0)
            {
                int finalChange = CalculateRelationshipImpact(npcData, option);
                Debug.Log($"   Финальное изменение: {finalChange} (с учетом личности)");
            
                RelationshipManager.Instance.ModifyRelationship(npcData.npcID, playerData.playerID, finalChange);
                
                // ПОКАЗЫВАЕМ УВЕДОМЛЕНИЕ ИГРОКУ
                if (RelationshipNotificationUI.Instance != null)
                {
                    RelationshipNotificationUI.Instance.ShowRelationshipChange(
                        npcData.npcName, 
                        finalChange
                    );
                }
            
                // Проверяем результат
                int newRelationship = RelationshipManager.Instance.GetRelationshipWithPlayer(npcData.npcID);
                Debug.Log($"   Новые отношения: {newRelationship}/100");
            }
            else
            {
                Debug.Log($"   Изменение отношений равно 0!");
            }
        
            // Добавляем память
            if (!string.IsNullOrEmpty(option.memoryToAdd))
            {
                RelationshipManager.Instance.AddMemory(
                    npcData.npcID, 
                    option.memoryToAdd, 
                    option.relationshipChange
                );
                Debug.Log($"   Добавлена память: {option.memoryToAdd}");
            }
        
            // Запускаем квест
            if (!string.IsNullOrEmpty(option.questToStart))
            {
                QuestSystem.Instance?.AcceptQuest(option.questToStart);
                Debug.Log($"   Запущен квест: {option.questToStart}");
            }
        
            // Устанавливаем флаги
            if (option.setFlags is { Length: > 0 })
            {
                foreach (var flag in option.setFlags)
                {
                    playerData.SetDialogueFlag(npcData.npcID, flag);
                    Debug.Log($"   Установлен флаг: {flag}");
                }
            }
        
            // Сохраняем историю диалога
            playerData.RecordDialogueChoice(
                npcData.npcID, 
                _currentTree.treeName, 
                CurrentNode.nodeID, 
                option.GetLocalizedText(),
                option.setFlags
            );
        
            Debug.Log($"   Выбор обработан");
        }
    
        private static int CalculateRelationshipImpact(NpcData npc, DialogueOption option)
        {
            var baseImpact = option.relationshipChange;
        
            // Модификаторы на основе личности
            if (npc.personality == null || option.personalityPreference == null) return baseImpact;
            var preference = option.personalityPreference;
            
            // Проверка предпочтений по чертам
            if (preference.preferredTrait != Trait.None && npc.personality.HasTrait(preference.preferredTrait))
                baseImpact += 5;
                
            if (preference.dislikedTrait != Trait.None && npc.personality.HasTrait(preference.dislikedTrait))
                baseImpact -= 5;
                
            // Проверка предпочтений по открытости
            if (preference.preferredOpenness >= 0)
            {
                if (npc.personality.openness >= preference.preferredOpenness)
                    baseImpact += 2;
                else
                    baseImpact -= 2;
            }
            
            // Проверка предпочтений по дружелюбию
            if (preference.preferredFriendliness >= 0)
            {
                if (npc.personality.friendliness >= preference.preferredFriendliness)
                    baseImpact += 2;
                else
                    baseImpact -= 2;
            }
            
            // Проверка предпочтений по амбициозности
            if (preference.preferredAmbition < 0) return baseImpact;
            if (npc.personality.ambition >= preference.preferredAmbition)
                baseImpact += 2;
            else
                baseImpact -= 2;

            return baseImpact;
        }
    
        private void MoveToNode(string nodeID)
        {
            var nextNode = _currentTree.nodes.FirstOrDefault(n => n.nodeID == nodeID && CheckNodeConditions(n));
        
            if (nextNode != null)
            {
                CurrentNode = nextNode;
            
                // Устанавливаем флаги узла
                if (CurrentNode.setFlags != null)
                {
                    foreach (var flag in CurrentNode.setFlags)
                    {
                        PlayerData.Instance.SetDialogueFlag(_currentNpc.npcData.npcID, flag);
                    }
                }
            
                OnDialogueNodeChanged?.Invoke(CurrentNode);
            }
            else
            {
                EndDialogue();
            }
        }
    
        public void EndDialogue()
        {
            if (!IsInDialogue) return;
        
            IsInDialogue = false;
        
            var endedNpc = _currentNpc;
            _currentTree = null;
            CurrentNode = null;
            _currentNpc = null;
        
            OnDialogueEnded?.Invoke();
        
            // Уведомляем NPC о завершении диалога
            endedNpc?.OnDialogueEnded();
        }
    
        public DialogueOption[] GetCurrentOptions()
        {
            if (!IsInDialogue || CurrentNode?.playerOptions == null) 
                return Array.Empty<DialogueOption>();
        
            return CurrentNode.playerOptions.Where(option => 
                option != null && ConditionChecker.CheckConditions(option.conditions, _currentNpc.npcData)
            ).ToArray();
        }
    
        public bool IsInDialogue { get; private set; }

        public DialogueNode CurrentNode { get; private set; }

        public string CurrentNpcName => _currentNpc?.npcData.npcName ?? "";
        public NpcInteraction CurrentNpc => _currentNpc;
    }
}