using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Gameplay.Systems;
using System;

namespace UI
{
    public class LoadMenuEntryUI : MonoBehaviour
    {
        [Header("Элементы UI")]
        [SerializeField] private TextMeshProUGUI saveNameText;
        [SerializeField] private TextMeshProUGUI timestampText;
        [SerializeField] private TextMeshProUGUI playerLevelText;
        [SerializeField] private TextMeshProUGUI locationText;
        [SerializeField] private RawImage screenshotImage;
        [SerializeField] private Button loadButton;
        [SerializeField] private GameObject corruptedIndicator;
        [SerializeField] private GameObject autosaveIndicator;
        [SerializeField] private GameObject quicksaveIndicator;
        
        private SaveGameInfo _saveInfo;
        private Action<SaveGameInfo> _onLoadCallback;
        
        public void Initialize(SaveGameInfo saveInfo, Action<SaveGameInfo> onLoad)
        {
            _saveInfo = saveInfo;
            _onLoadCallback = onLoad;
            
            UpdateUI();
            
            if (loadButton != null)
                loadButton.onClick.AddListener(OnLoadClicked);
        }
        
        private void UpdateUI()
        {
            if (_saveInfo == null) return;
            
            // Отображаем имя сохранения
            if (saveNameText != null)
            {
                saveNameText.text = GetDisplayName(_saveInfo.saveName);
            }
            
            // Отображаем время сохранения
            if (timestampText != null)
            {
                timestampText.text = _saveInfo.timestamp;
            }
            
            // Отображаем уровень игрока
            if (playerLevelText != null)
            {
                playerLevelText.text = $"Ур. {_saveInfo.playerLevel}";
            }
            
            // Отображаем локацию
            if (locationText != null)
            {
                locationText.text = _saveInfo.location ?? "Неизвестно";
            }
            
            // Загружаем и отображаем скриншот
            if (screenshotImage != null && SaveManager.Instance != null)
            {
                var screenshotTexture = SaveManager.Instance.LoadScreenshotFromBase64(_saveInfo.screenshotData);
                if (screenshotTexture != null)
                {
                    screenshotImage.texture = screenshotTexture;
                }
            }
            
            // Показываем специальные индикаторы
            if (autosaveIndicator != null)
            {
                autosaveIndicator.SetActive(_saveInfo.saveName == "autosave");
            }
            
            if (quicksaveIndicator != null)
            {
                quicksaveIndicator.SetActive(_saveInfo.saveName == "quicksave");
            }
            
            if (corruptedIndicator != null)
            {
                corruptedIndicator.SetActive(_saveInfo.isCorrupted);
            }
            
            // Блокируем кнопку загрузки если сохранение повреждено
            if (loadButton != null)
            {
                loadButton.interactable = !_saveInfo.isCorrupted;
            }
        }
        
        private string GetDisplayName(string saveName)
        {
            if (saveName == "autosave") return "Автосохранение";
            if (saveName == "quicksave") return "Быстрое сохранение";
            
            if (saveName.StartsWith("manual_"))
            {
                string cleanName = saveName.Replace("manual_", "");
                
                int lastUnderscore = cleanName.LastIndexOf('_');
                if (lastUnderscore > 0)
                {
                    string timestampPart = cleanName.Substring(lastUnderscore + 1);
                    if (timestampPart.Length == 6 && timestampPart.Contains(":") ||
                        timestampPart.Length == 8 && timestampPart.Contains(":"))
                    {
                        cleanName = cleanName.Substring(0, lastUnderscore);
                    }
                }
                
                return string.IsNullOrEmpty(cleanName) ? "Ручное сохранение" : cleanName;
            }
            
            return saveName;
        }
        
        private void OnLoadClicked()
        {
            _onLoadCallback?.Invoke(_saveInfo);
        }
        
        private void OnDestroy()
        {
            if (loadButton != null)
                loadButton.onClick.RemoveListener(OnLoadClicked);
        }
    }
}