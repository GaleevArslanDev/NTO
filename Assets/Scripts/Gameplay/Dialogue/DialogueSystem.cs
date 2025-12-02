using Core;
using UnityEngine;

namespace Gameplay.Dialogue
{
    [CreateAssetMenu(menuName = "Dialogue/Dialogue Tree")]
    public class DialogueTree : ScriptableObject
    {
        public string treeName;
        public DialogueNode[] nodes;
        public Condition[] globalConditions;
        public string[] requiredFlags;
        public string[] setFlagsOnStart;
        public int priority;
    }

    [System.Serializable]
    public class DialogueNode
    {
        public string nodeID;
        [Tooltip("Ключ локализации для текста NPC")]
        public string localizationKey;
        [TextArea(3, 5)] 
        public DialogueOption[] playerOptions;
        public string[] triggers;
        public Condition[] conditions;
        public string[] setFlags;
        public Emotion emotion = Emotion.Neutral;
        public AudioClip voiceLine;
        public float typingSpeed = 0.05f;
        
        // Метод для получения локализованного текста
        public string GetLocalizedText()
        {
            if (!string.IsNullOrEmpty(localizationKey) && LocalizationManager.LocalizationManager.Instance != null)
            {
                return LocalizationManager.LocalizationManager.Instance.GetString(localizationKey);
            }
            return "Localization failed"; // Fallback
        }
    }

    [System.Serializable]
    public class DialogueOption
    {
        [Tooltip("Ключ локализации для текста опции")]
        public string localizationKey;
        public string nextNodeID;
        public int relationshipChange;
        public string memoryToAdd;
        public string questToStart;
        public ItemType[] requiredItems;
        public Condition[] conditions;
        public string[] setFlags;
        public PersonalityPreference personalityPreference;
        public bool isExitOption;
        
        // Метод для получения локализованного текста
        public string GetLocalizedText()
        {
            if (!string.IsNullOrEmpty(localizationKey) && LocalizationManager.LocalizationManager.Instance != null)
            {
                return LocalizationManager.LocalizationManager.Instance.GetString(localizationKey);
            }
            return "Localization failed"; // Fallback
        }
    }


    [System.Serializable]
    public class Condition
    {
        public ConditionType type;
        public string stringValue;
        public int intValue;
        public int intValue2;
        public TimeOfDay timeValue;
        public ComparisonOperator comparison = ComparisonOperator.GreaterOrEqual;
    }

    [System.Serializable]
    public class PersonalityPreference
    {
        public Trait preferredTrait;
        public Trait dislikedTrait;
        public int preferredOpenness = -1;
        public int preferredFriendliness = -1;
        public int preferredAmbition = -1;
    }
}