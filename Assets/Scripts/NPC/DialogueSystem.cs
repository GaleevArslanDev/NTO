using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Dialogue/Dialogue Tree")]
public class DialogueTree : ScriptableObject
{
    public string treeName;
    public DialogueNode[] nodes;
    public Condition[] globalConditions;
    public string[] requiredFlags;
    public string[] setFlagsOnStart;
    public int priority = 0;
}

[System.Serializable]
public class DialogueNode
{
    public string nodeID;
    [TextArea(3, 5)] public string npcText;
    public DialogueOption[] playerOptions;
    public string[] triggers;
    public Condition[] conditions;
    public string[] setFlags;
    public Emotion emotion = Emotion.Neutral;
    public AudioClip voiceLine;
    public float typingSpeed = 0.05f;
}

[System.Serializable]
public class DialogueOption
{
    [TextArea(1, 2)] public string optionText;
    public string nextNodeID;
    public int relationshipChange;
    public string memoryToAdd;
    public string questToStart;
    public ItemType[] requiredItems;
    public Condition[] conditions;
    public string[] setFlags;
    public PersonalityPreference personalityPreference;
    public bool isExitOption = false;
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

public enum Emotion
{
    Neutral,
    Happy,
    Sad,
    Angry,
    Surprised,
    Confused,
    Thoughtful
}

public enum ComparisonOperator
{
    Equal,
    NotEqual,
    Greater,
    Less,
    GreaterOrEqual,
    LessOrEqual
}