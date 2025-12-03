using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using Gameplay.Systems;
using System;

namespace UI
{
    public class SaveMenuUI : MonoBehaviour
    {
        [Header("Элементы UI")]
        [SerializeField] private TMP_InputField saveNameInput;
        [SerializeField] private Button createSaveButton;
        [SerializeField] private Button backButton;
        [SerializeField] private Transform savesContainer;
        [SerializeField] private GameObject saveEntryPrefab;
        [SerializeField] private GameObject noSavesText;
        [SerializeField] private TextMeshProUGUI saveCountText;
        [SerializeField] private Button quickSaveButton;
        
        [Header("Настройки")]
        [SerializeField] private int maxDisplaySaves = 10;
        [SerializeField] private string defaultSaveName = "Ручное сохранение";
        
        private List<GameObject> _saveEntries = new List<GameObject>();
        private PauseMenuController _pauseMenu;
        
        private void Start()
        {
            InitializeButtons();
            _pauseMenu = FindObjectOfType<PauseMenuController>();
            
            // Автозаполнение имени сохранения по умолчанию
            if (saveNameInput != null)
            {
                saveNameInput.text = $"{defaultSaveName} {DateTime.Now:HH:mm}";
                saveNameInput.onEndEdit.AddListener(OnSaveNameChanged);
            }
        }
        
        private void OnEnable()
        {
            RefreshSaveList();
        }
        
        private void InitializeButtons()
        {
            if (createSaveButton != null)
                createSaveButton.onClick.AddListener(CreateManualSave);
            
            if (backButton != null)
                backButton.onClick.AddListener(ReturnToPauseMenu);
            
            if (quickSaveButton != null)
                quickSaveButton.onClick.AddListener(QuickSave);
        }
        
        public void RefreshSaveList()
        {
            ClearSaveEntries();
            
            if (SaveManager.Instance == null)
            {
                Debug.LogError("SaveManager не найден!");
                return;
            }
            
            var saves = SaveManager.Instance.GetSaveGames();
            
            // Фильтруем только ручные сохранения
            var manualSaves = new List<SaveGameInfo>();
            var otherSaves = new List<SaveGameInfo>();
            
            foreach (var save in saves)
            {
                if (save.saveName.StartsWith("manual_"))
                    manualSaves.Add(save);
                else if (save.saveName == "quicksave")
                    otherSaves.Add(save);
            }
            
            // Сортируем ручные сохранения по времени (новые сначала)
            manualSaves.Sort((a, b) => string.Compare(b.timestamp, a.timestamp));
            
            // Показываем количество сохранений
            if (saveCountText != null)
            {
                saveCountText.text = $"Ручных сохранений: {manualSaves.Count}/{SaveManager.Instance.maxManualSaves}";
            }
            
            // Отображаем сохранения
            int displayCount = Mathf.Min(manualSaves.Count, maxDisplaySaves);
            
            for (int i = 0; i < displayCount; i++)
            {
                CreateSaveEntry(manualSaves[i]);
            }
            
            // Показываем текст если нет сохранений
            if (noSavesText != null)
            {
                noSavesText.SetActive(manualSaves.Count == 0);
            }
        }
        
        private void CreateSaveEntry(SaveGameInfo saveInfo)
        {
            if (saveEntryPrefab == null || savesContainer == null) return;
            
            var entryObj = Instantiate(saveEntryPrefab, savesContainer);
            _saveEntries.Add(entryObj);
            
            var entry = entryObj.GetComponent<SaveMenuEntryUI>();
            if (entry != null)
            {
                entry.Initialize(saveInfo, OnSaveSelected, OnSaveDeleted);
            }
        }
        
        private void ClearSaveEntries()
        {
            foreach (var entry in _saveEntries)
            {
                Destroy(entry);
            }
            _saveEntries.Clear();
        }
        
        private void OnSaveSelected(SaveGameInfo saveInfo)
        {
            // При выборе существующего сохранения спрашиваем подтверждение перезаписи
            ShowOverwriteConfirmation(saveInfo.saveName);
        }
        
        private void OnSaveDeleted(SaveGameInfo saveInfo)
        {
            // Подтверждение удаления
            if (SaveManager.Instance != null)
            {
                // Можно добавить диалог подтверждения здесь
                SaveManager.Instance.DeleteSave(saveInfo.saveName);
                RefreshSaveList();
            }
        }
        
        private void CreateManualSave()
        {
            if (SaveManager.Instance == null) return;
            
            string saveName = saveNameInput?.text;
            if (string.IsNullOrWhiteSpace(saveName))
            {
                saveName = $"{defaultSaveName} {DateTime.Now:HH:mm:ss}";
            }
            
            SaveManager.Instance.CreateManualSave(saveName);
            
            // Очищаем поле ввода
            if (saveNameInput != null)
            {
                saveNameInput.text = "";
            }
            
            // Обновляем список
            RefreshSaveList();
            
            // Показываем сообщение об успешном сохранении
            ShowSaveNotification($"Игра сохранена: {saveName}");
        }
        
        private void QuickSave()
        {
            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.QuickSave();
                ShowSaveNotification("Быстрое сохранение выполнено");
                RefreshSaveList();
            }
        }
        
        private void ShowOverwriteConfirmation(string existingSaveName)
        {
            // Здесь можно реализовать диалог подтверждения
            // Для простоты сразу перезаписываем
            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.CreateManualSave(existingSaveName.Replace("manual_", ""));
                ShowSaveNotification($"Сохранение перезаписано: {existingSaveName}");
                RefreshSaveList();
            }
        }
        
        private void ShowSaveNotification(string message)
        {
            Debug.Log(message);
            // Здесь можно добавить всплывающее уведомление
        }
        
        private void OnSaveNameChanged(string newName)
        {
            // Можно добавить валидацию имени сохранения
            if (string.IsNullOrWhiteSpace(newName))
            {
                if (saveNameInput != null)
                    saveNameInput.text = $"{defaultSaveName} {DateTime.Now:HH:mm}";
            }
        }
        
        private void ReturnToPauseMenu()
        {
            if (_pauseMenu != null)
            {
                _pauseMenu.CloseSubMenu();
            }
        }
        
        private void OnDestroy()
        {
            if (createSaveButton != null)
                createSaveButton.onClick.RemoveListener(CreateManualSave);
            
            if (backButton != null)
                backButton.onClick.RemoveListener(ReturnToPauseMenu);
            
            if (quickSaveButton != null)
                quickSaveButton.onClick.RemoveListener(QuickSave);
            
            if (saveNameInput != null)
                saveNameInput.onEndEdit.RemoveListener(OnSaveNameChanged);
        }
    }
}