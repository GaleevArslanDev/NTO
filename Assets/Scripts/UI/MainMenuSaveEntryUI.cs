using Gameplay.Systems;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace UI
{
    public class MainMenuSaveEntryUI : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private TMP_Text saveNameText;
        [SerializeField] private TMP_Text timestampText;
        [SerializeField] private TMP_Text detailsText;
        [SerializeField] private Button selectButton;
        [SerializeField] private RawImage screenshotImage;
        [SerializeField] private GameObject corruptedIndicator;
        
        public void Initialize(SaveGameInfo saveInfo, UnityAction onSelectCall)
        {
            saveNameText.text = GetDisplayName(saveInfo.saveName);
            timestampText.text = saveInfo.timestamp;
            detailsText.text = LocalizationManager.LocalizationManager.Instance.GetString("save-entry_player-level-label", saveInfo.playerLevel.ToString());
            Debug.Log($"Screenshot data length: {saveInfo.screenshotData}");
            // Загрузка скриншота
            if (!string.IsNullOrEmpty(saveInfo.screenshotData))
            {
                Debug.Log("Loading screenshot from base64");
                var texture = SaveManager.Instance.LoadScreenshotFromBase64(saveInfo.screenshotData);
                if (texture != null)
                {
                    screenshotImage.texture = texture;
                    screenshotImage.color = Color.white;
                }
            }
            
            // Индикатор повреждения
            if (corruptedIndicator != null)
                corruptedIndicator.SetActive(saveInfo.isCorrupted);
                
            // Настройка кнопки
            selectButton.onClick.AddListener(onSelectCall);
        }
        
        private string GetDisplayName(string saveName)
        {
            if (saveName == "autosave") return LocalizationManager.LocalizationManager.Instance.GetString("save_autosave");
            if (saveName == "quicksave") return LocalizationManager.LocalizationManager.Instance.GetString("save_quicksave");
            if (saveName.StartsWith("manual_"))
            {
                return saveName.Replace("manual_", LocalizationManager.LocalizationManager.Instance.GetString("save_manual"));
            }
            return saveName;
        }
    }
}