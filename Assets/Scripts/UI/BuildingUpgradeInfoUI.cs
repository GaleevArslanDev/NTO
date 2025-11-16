using UnityEngine;
using TMPro;

public class BuildingUpgradeInfoUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _buildingNameText;
    [SerializeField] private TextMeshProUGUI _levelText;
    [SerializeField] private TextMeshProUGUI _descriptionText;

    public void Set(string buildingName, int currentLevel, int newLevel, string description)
    {
        _buildingNameText.text = buildingName;
        _levelText.text = $"{currentLevel} → {newLevel}";
        _descriptionText.text = description;
    }
}