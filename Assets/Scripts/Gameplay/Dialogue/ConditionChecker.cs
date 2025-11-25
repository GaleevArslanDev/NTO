using System.Linq;
using Core;
using Data.Game;
using Data.NPC;
using Gameplay.Items;
using Gameplay.Systems;

namespace Gameplay.Dialogue
{
    public static class ConditionChecker
    {
        public static bool CheckConditions(Condition[] conditions, NpcData npcData)
        {
            if (conditions == null || conditions.Length == 0) return true;

            return conditions.All(condition => CheckCondition(condition, npcData));
        }

        private static bool CheckCondition(Condition condition, NpcData npcData)
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
                    if (string.IsNullOrEmpty(condition.stringValue)) return true;
                    var hasFlag = PlayerData.Instance.HasDialogueFlag(npcData.npcID, condition.stringValue);
                    return condition.comparison == ComparisonOperator.Equal ? hasFlag : !hasFlag;

                case ConditionType.DialogueCount:
                    return CheckComparison(
                        PlayerData.Instance.GetDialogueCountWithNpc(npcData.npcID),
                        condition.intValue,
                        condition.comparison
                    );
                
                case ConditionType.ItemOwned:
                    if (condition.intValue > 0)
                    {
                        return Inventory.Instance.GetItemCount((ItemType)condition.intValue) >= condition.intValue2;
                    }
                    return Inventory.Instance.GetItemCount((ItemType)condition.intValue) > 0;
                
                case ConditionType.PlayerLevel:
                    return CheckComparison(PlayerData.Instance.playerLevel, condition.intValue, condition.comparison);
                
                case ConditionType.Location:
                default:
                    return true;
            }
        }
    
        private static bool CheckRelationshipCondition(Condition condition, NpcData npcData)
        {
            var relationship = npcData.GetRelationship(PlayerData.Instance.playerID);
            return CheckComparison(relationship, condition.intValue, condition.comparison);
        }
    
        private static bool CheckComparison(int value, int target, ComparisonOperator op)
        {
            return op switch
            {
                ComparisonOperator.Equal => value == target,
                ComparisonOperator.NotEqual => value != target,
                ComparisonOperator.Greater => value > target,
                ComparisonOperator.Less => value < target,
                ComparisonOperator.GreaterOrEqual => value >= target,
                ComparisonOperator.LessOrEqual => value <= target,
                _ => true
            };
        }
    }
}