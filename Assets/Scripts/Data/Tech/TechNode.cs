using System.Collections.Generic;
using Data.Game;
using UnityEngine;

namespace Data.Tech
{
    [System.Serializable]
    public class TechNode
    {
        public string nodeId;
        [Tooltip("Ключ локализации для названия узла")]
        public string nameLocalizationKey;
        [Tooltip("Ключ локализации для описания узла")]
        public string descriptionLocalizationKey;
    
        public int tier;
        public List<ResourceCost> unlockCost;
        public List<string> prerequisiteNodes;
        public bool isUnlocked;
        public TechEffect[] effects;
    
        [Header("UI Settings")]
        public Vector2 graphPosition;
        public Sprite icon;
    
        // Методы для получения локализованного текста
        public string GetLocalizedName()
        {
            if (!string.IsNullOrEmpty(nameLocalizationKey) && LocalizationManager.LocalizationManager.Instance != null)
            {
                return LocalizationManager.LocalizationManager.Instance.GetString(nameLocalizationKey);
            }
            return "Localization failed";
        }
    
        public string GetLocalizedDescription()
        {
            if (!string.IsNullOrEmpty(descriptionLocalizationKey) && LocalizationManager.LocalizationManager.Instance != null)
            {
                return LocalizationManager.LocalizationManager.Instance.GetString(descriptionLocalizationKey);
            }
            return "Localization failed";
        }
    }
}