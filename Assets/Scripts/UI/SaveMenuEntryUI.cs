using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Gameplay.Systems;
using System;

namespace UI
{
    public class SaveMenuEntryUI : MonoBehaviour
    {
        [Header("Элементы UI")]
        [SerializeField] private TextMeshProUGUI saveNameText;
        [SerializeField] private TextMeshProUGUI timestampText;
        [SerializeField] private TextMeshProUGUI playerLevelText;
        [SerializeField] private TextMeshProUGUI locationText;
        [SerializeField] private RawImage screenshotImage;
        [SerializeField] private Button selectButton;
        [SerializeField] private Button deleteButton;
        [SerializeField] private GameObject corruptedIndicator;
        
        private SaveGameInfo _saveInfo;
        private Action<SaveGameInfo> _onSelectCallback;
        private Action<SaveGameInfo> _onDeleteCallback;
        
        public void Initialize(SaveGameInfo saveInfo, Action<SaveGameInfo> onSelect, Action<SaveGameInfo> onDelete)
        {
            _saveInfo = saveInfo;
            _onSelectCallback = onSelect;
            _onDeleteCallback = onDelete;
            
            UpdateUI();
            
            if (selectButton != null)
                selectButton.onClick.AddListener(OnSelectClicked);
            
            if (deleteButton != null)
                deleteButton.onClick.AddListener(OnDeleteClicked);
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
            
            // Показываем индикатор повреждения
            if (corruptedIndicator != null)
            {
                corruptedIndicator.SetActive(_saveInfo.isCorrupted);
            }
        }
        
        private string GetDisplayName(string saveName)
        {
            if (saveName.StartsWith("manual_"))
            {
                // Извлекаем имя из названия файла
                string cleanName = saveName.Replace("manual_", "");
                
                // Пытаемся удалить временную метку
                int lastUnderscore = cleanName.LastIndexOf('_');
                if (lastUnderscore > 0)
                {
                    // Проверяем, является ли часть после последнего подчеркивания временной меткой
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
        
        private void OnSelectClicked()
        {
            _onSelectCallback?.Invoke(_saveInfo);
        }
        
        private void OnDeleteClicked()
        {
            _onDeleteCallback?.Invoke(_saveInfo);
        }
        
        private void OnDestroy()
        {
            if (selectButton != null)
                selectButton.onClick.RemoveListener(OnSelectClicked);
            
            if (deleteButton != null)
                deleteButton.onClick.RemoveListener(OnDeleteClicked);
        }
    }
}