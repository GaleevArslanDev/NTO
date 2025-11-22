using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance;
    
    [Header("Settings")]
    public float defaultTypingSpeed = 0.05f;
    
    private Dictionary<string, DialogueTree> loadedTrees = new Dictionary<string, DialogueTree>();
    private DialogueTree currentTree;
    private DialogueNode currentNode;
    private NPCInteraction currentNPC;
    private bool inDialogue = false;
    
    public System.Action<DialogueNode, string> OnDialogueStarted;
    public System.Action<DialogueNode> OnDialogueNodeChanged;
    public System.Action OnDialogueEnded;
    
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
        if (tree != null && !loadedTrees.ContainsKey(tree.treeName))
        {
            loadedTrees[tree.treeName] = tree;
        }
    }
    
    public void UnloadDialogueTree(string treeName)
    {
        if (loadedTrees.ContainsKey(treeName))
        {
            loadedTrees.Remove(treeName);
        }
    }
    
    public bool StartDialogue(NPCInteraction npc, string treeName = null)
    {
        if (inDialogue || npc == null || npc.NPCData == null) return false;
        
        currentNPC = npc;
        DialogueTree treeToUse = null;
        
        // Если указано конкретное дерево, используем его
        if (!string.IsNullOrEmpty(treeName) && loadedTrees.ContainsKey(treeName))
        {
            treeToUse = loadedTrees[treeName];
        }
        else
        {
            // Ищем подходящее дерево по приоритету и условиям
            treeToUse = FindSuitableTree(npc);
        }
        
        if (treeToUse == null) return false;
        
        currentTree = treeToUse;
        inDialogue = true;
        
        // Устанавливаем флаги при старте
        if (currentTree.setFlagsOnStart != null)
        {
            foreach (string flag in currentTree.setFlagsOnStart)
            {
                PlayerData.Instance.SetDialogueFlag(npc.NPCData.npcID, flag);
            }
        }
        
        // Находим стартовый узел
        currentNode = FindStartNode(currentTree);
        if (currentNode == null)
        {
            EndDialogue();
            return false;
        }
        
        OnDialogueStarted?.Invoke(currentNode, npc.NPCData.npcName);
        return true;
    }
    
    private DialogueTree FindSuitableTree(NPCInteraction npc)
    {
        var suitableTrees = new List<DialogueTree>();
        
        foreach (var tree in loadedTrees.Values)
        {
            if (CheckTreeConditions(tree, npc.NPCData))
            {
                suitableTrees.Add(tree);
            }
        }
        
        // Возвращаем дерево с наивысшим приоритетом
        return suitableTrees.OrderByDescending(t => t.priority).FirstOrDefault();
    }
    
    private bool CheckTreeConditions(DialogueTree tree, NPCData npcData)
    {
        // Проверяем глобальные условия
        if (tree.globalConditions != null && !ConditionChecker.CheckConditions(tree.globalConditions, npcData))
            return false;
            
        // Проверяем требуемые флаги
        if (tree.requiredFlags != null)
        {
            foreach (string flag in tree.requiredFlags)
            {
                if (!PlayerData.Instance.HasDialogueFlag(npcData.npcID, flag))
                    return false;
            }
        }
        
        return true;
    }
    
    private DialogueNode FindStartNode(DialogueTree tree)
    {
        // Сначала ищем узел с ID "start"
        var startNode = tree.nodes.FirstOrDefault(n => n.nodeID == "start" && CheckNodeConditions(n));
        if (startNode != null) return startNode;
        
        // Ищем любой подходящий узел
        return tree.nodes.FirstOrDefault(n => CheckNodeConditions(n));
    }
    
    private bool CheckNodeConditions(DialogueNode node)
    {
        return node != null && ConditionChecker.CheckConditions(node.conditions, currentNPC.NPCData);
    }
    
    public void SelectOption(DialogueOption option)
    {
        if (!inDialogue || currentNPC == null) return;
        
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
    var npcData = currentNPC.NPCData;
    var playerData = PlayerData.Instance;
    
    // ОТЛАДКА: Выводим информацию о выборе
    Debug.Log($"   Обработка выбора диалога:");
    Debug.Log($"   NPC: {npcData.npcName} (ID: {npcData.npcID})");
    Debug.Log($"   Опция: '{option.optionText}'");
    Debug.Log($"   Изменение отношений: {option.relationshipChange}");
    
    // Изменяем отношения
    if (option.relationshipChange != 0)
    {
        int finalChange = CalculateRelationshipImpact(npcData, option, playerData);
        Debug.Log($"   Финальное изменение: {finalChange} (с учетом личности)");
        
        RelationshipManager.Instance.ModifyRelationship(npcData.npcID, playerData.playerID, finalChange);
        
        // Проверяем результат
        int newRelationship = RelationshipManager.Instance.GetRelationshipWithPlayer(npcData.npcID);
        Debug.Log($"   Новые отношения: {newRelationship}/100");
    }
    else
    {
        Debug.Log($"   Изменение отношений равно 0!");
    }
    
    // Добавляем память - ИСПРАВЛЕННЫЙ ВЫЗОВ (3 параметра)
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
        QuestManager.Instance?.StartQuest(option.questToStart);
        Debug.Log($"   Запущен квест: {option.questToStart}");
    }
    
    // Устанавливаем флаги
    if (option.setFlags != null && option.setFlags.Length > 0)
    {
        foreach (string flag in option.setFlags)
        {
            playerData.SetDialogueFlag(npcData.npcID, flag);
            Debug.Log($"   Установлен флаг: {flag}");
        }
    }
    
    // Сохраняем историю диалога
    playerData.RecordDialogueChoice(
        npcData.npcID, 
        currentTree.treeName, 
        currentNode.nodeID, 
        option.optionText,
        option.setFlags
    );
    
    Debug.Log($"   Выбор обработан");
}
    
    private int CalculateRelationshipImpact(NPCData npc, DialogueOption option, PlayerData player)
    {
        int baseImpact = option.relationshipChange;
        
        // Модификаторы на основе личности
        if (npc.personality != null && option.personalityPreference != null)
        {
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
            if (preference.preferredAmbition >= 0)
            {
                if (npc.personality.ambition >= preference.preferredAmbition)
                    baseImpact += 2;
                else
                    baseImpact -= 2;
            }
        }
            
        return baseImpact;
    }
    
    private void MoveToNode(string nodeID)
    {
        var nextNode = currentTree.nodes.FirstOrDefault(n => n.nodeID == nodeID && CheckNodeConditions(n));
        
        if (nextNode != null)
        {
            currentNode = nextNode;
            
            // Устанавливаем флаги узла
            if (currentNode.setFlags != null)
            {
                foreach (string flag in currentNode.setFlags)
                {
                    PlayerData.Instance.SetDialogueFlag(currentNPC.NPCData.npcID, flag);
                }
            }
            
            OnDialogueNodeChanged?.Invoke(currentNode);
        }
        else
        {
            EndDialogue();
        }
    }
    
    public void EndDialogue()
    {
        if (!inDialogue) return;
        
        inDialogue = false;
        
        var endedNPC = currentNPC;
        currentTree = null;
        currentNode = null;
        currentNPC = null;
        
        OnDialogueEnded?.Invoke();
        
        // Уведомляем NPC о завершении диалога
        endedNPC?.OnDialogueEnded();
    }
    
    public DialogueOption[] GetCurrentOptions()
    {
        if (!inDialogue || currentNode == null || currentNode.playerOptions == null) 
            return new DialogueOption[0];
        
        return currentNode.playerOptions.Where(option => 
            option != null && ConditionChecker.CheckConditions(option.conditions, currentNPC.NPCData)
        ).ToArray();
    }
    
    public bool IsInDialogue => inDialogue;
    public DialogueNode CurrentNode => currentNode;
    public string CurrentNPCName => currentNPC?.NPCData.npcName ?? "";
}