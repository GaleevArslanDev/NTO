using System.Linq;
using UnityEngine;

public static class ConditionChecker
{
    public static bool CheckConditions(Condition[] conditions, NPCData npcData)
    {
        if (conditions == null || conditions.Length == 0) return true;
        
        foreach (var condition in conditions)
        {
            if (!CheckCondition(condition, npcData)) return false;
        }
        return true;
    }
    
    public static bool CheckCondition(Condition condition, NPCData npcData)
    {
        if (npcData == null) return false;
        
        switch (condition.type)
        {
            case ConditionType.Relationship:
                return CheckRelationshipCondition(condition, npcData);
                
            case ConditionType.Memory:
                return npcData.memories?.Exists(m => m.memoryText.Contains(condition.stringValue)) == true;
                
            case ConditionType.QuestCompleted:
                return PlayerData.Instance.completedQuests.Contains(condition.stringValue);
                
            case ConditionType.TimeOfDay:
                return WorldTime.Instance.GetTimeOfDay() == condition.timeValue;
                
            case ConditionType.Flag:
                if (!string.IsNullOrEmpty(condition.stringValue))
                {
                    bool hasFlag = PlayerData.Instance.HasDialogueFlag(npcData.npcID, condition.stringValue);
                    return condition.comparison == ComparisonOperator.Equal ? hasFlag : !hasFlag;
                }
                return true;
                
            case ConditionType.DialogueCount:
                return CheckComparison(
                    PlayerData.Instance.GetDialogueCountWithNPC(npcData.npcID),
                    condition.intValue,
                    condition.comparison
                );
                
            case ConditionType.ItemOwned:
                if (condition.intValue > 0)
                {
                    return Inventory.Instance.GetItemCount((ItemType)condition.intValue) >= condition.intValue2;
                }
                else
                {
                    return Inventory.Instance.GetItemCount((ItemType)condition.intValue) > 0;
                }
                
            case ConditionType.PlayerLevel:
                return CheckComparison(PlayerData.Instance.playerLevel, condition.intValue, condition.comparison);
                
            default:
                return true;
        }
    }
    
    private static bool CheckRelationshipCondition(Condition condition, NPCData npcData)
    {
        int relationship = npcData.GetRelationship(PlayerData.Instance.playerID);
        return CheckComparison(relationship, condition.intValue, condition.comparison);
    }
    
    private static bool CheckComparison(int value, int target, ComparisonOperator op)
    {
        switch (op)
        {
            case ComparisonOperator.Equal: return value == target;
            case ComparisonOperator.NotEqual: return value != target;
            case ComparisonOperator.Greater: return value > target;
            case ComparisonOperator.Less: return value < target;
            case ComparisonOperator.GreaterOrEqual: return value >= target;
            case ComparisonOperator.LessOrEqual: return value <= target;
            default: return true;
        }
    }
}