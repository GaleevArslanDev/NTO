using TMPro;
using UnityEngine;

namespace UI
{
    public class BuildingUpgradeInfoUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI buildingNameText;
        [SerializeField] private TextMeshProUGUI levelText;
        [SerializeField] private TextMeshProUGUI descriptionText;

        public void Set(string buildingName, int currentLevel, int newLevel, string description)
        {
            buildingNameText.text = buildingName;
            levelText.text = $"{currentLevel} → {newLevel}";
            descriptionText.text = description;
        }
    }
}