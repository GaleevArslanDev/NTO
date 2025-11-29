namespace Core
{
    public enum ItemType
    {
        CrystalRed,
        CrystalBlue,
        Stone,
        Wood,
        Metal
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

    public enum ActivityType
    {
        Work,
        Social,
        Leisure,
        Home,
        Special,
        Eating,
        Sleeping,
        Traveling
    }

    public enum TimeOfDay
    {
        Morning,
        Noon,
        Afternoon,
        Evening,
        Night
    }

    public enum ConditionType
    {
        Relationship,
        Memory,
        QuestCompleted,
        TimeOfDay,
        Location,
        PlayerLevel,
        ItemOwned,
        Flag,
        DialogueCount
    }

    public enum NpcState
    {
        Idle,
        Walking,
        Talking,
        Working,
        Socializing,
        Sleeping,
        Eating,
        Traveling,
        Waiting,
        Leisure
    }
    
    public enum EffectType
    {
        FireRateMultiplier,
        DamageMultiplier,
        MiningSpeedMultiplier,
        InventoryCapacity,
        CollectionRangeMultiplier,
        PassiveIncome,
        UnlockBuilding,
        UnlockUpgradeTier,
        CollectedAmountMultiplier
    }
    
    public enum Trait
    {
        None,
        Honest,
        Deceptive,
        Kind, 
        Friendly,
        Aggressive,
        Lazy,
        Ambitious,
        Romantic,
        Materialistic,
        Spiritual,
        Pragmatic,
        Shy,
        Outgoing,
        Patient,
        Impulsive,
        Generous,
        Greedy
    }
    
    public enum QuestType
    {
        GatherResources,
        KillEnemies,
        UpgradeBuilding,
        UnlockTech
    }

    public enum AssistantMood
    {
        Neutral, 
        Happy, 
        Excited, 
        Worried, 
        Sarcastic
    }
    
    public enum NpcType
    {
        Mayor,      // Зол - Ратуша
        Blacksmith, // Брук - Кузница  
        Farmer,     // Горк - Ферма
        QuestGiver  // Лип - Квесты
    }

    public enum CommentPriority
    {
        Low, 
        Normal, 
        High, 
        Critical
    }
}