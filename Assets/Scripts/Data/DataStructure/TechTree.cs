using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Tech/TechTree")]
public class TechTree : ScriptableObject
{
    public string treeName;
    public string description;
    public List<TechNode> nodes = new List<TechNode>();
    public int maxTier = 5;
}

[System.Serializable]
public class TechNode
{
    public string nodeId;
    public string nodeName;
    public string description;
    public int tier;
    public List<ResourceCost> unlockCost;
    public List<string> prerequisiteNodes; // IDs узлов, которые должны быть открыты до этого
    public bool isUnlocked;
    public TechEffect[] effects;
    
    [Header("UI Settings")]
    public Vector2 graphPosition;
    public Sprite icon;
}

[System.Serializable]
public class TechEffect
{
    public enum EffectType
    {
        FireRateMultiplier,
        DamageMultiplier,
        MiningSpeedMultiplier,
        InventoryCapacity,
        CollectionRangeMultiplier,
        PassiveIncome,
        UnlockBuilding,
        UnlockUpgradeTier
    }
    
    public EffectType effectType;
    public float floatValue;
    public int intValue;
    public string stringValue; // Для названий зданий и т.д.
}