using Gameplay.Systems;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class SaveEntryUI : MonoBehaviour
    {
        [Header("UI Elements")]
        public TMP_Text saveNameText;
        public TMP_Text timestampText;
        public TMP_Text detailsText;
        public Button selectButton;
        
        private SaveGameInfo _saveInfo;
        private SaveLoadUI _parentUI;
        
        public void Initialize(SaveGameInfo saveInfo, SaveLoadUI parentUI)
        {
            _saveInfo = saveInfo;
            _parentUI = parentUI;
            
            saveNameText.text = GetDisplayName(saveInfo.saveName);
            timestampText.text = saveInfo.timestamp;
            detailsText.text = $"Ур. {saveInfo.playerLevel} | {saveInfo.location}";
            
            selectButton.onClick.AddListener(OnSelect);
        }
        
        private string GetDisplayName(string saveName)
        {
            if (saveName == "autosave") return "Автосохранение";
            if (saveName == "quicksave") return "Быстрое сохранение";
            if (saveName.StartsWith("manual_"))
            {
                return saveName.Replace("manual_", "Сохранение ");
            }
            return saveName;
        }
        
        private void OnSelect()
        {
            _parentUI.SelectSave(_saveInfo.saveName);
        }
    }
}