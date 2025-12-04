using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using Gameplay.Systems;
using UnityEngine.SceneManagement;

namespace UI
{
    public class LoadMenuUI : MonoBehaviour
    {
        [Header("Элементы UI")]
        [SerializeField] private Transform savesContainer;
        [SerializeField] private GameObject saveEntryPrefab;
        [SerializeField] private GameObject noSavesText;
        [SerializeField] private Button backButton;
        [SerializeField] private Button loadAutosaveButton;
        [SerializeField] private Button loadQuicksaveButton;
        
        private List<GameObject> _saveEntries = new List<GameObject>();
        private PauseMenuController _pauseMenu;
        
        private void Start()
        {
            InitializeButtons();
            _pauseMenu = FindObjectOfType<PauseMenuController>();
        }
        
        private void OnEnable()
        {
            RefreshSaveList();
        }
        
        private void InitializeButtons()
        {
            if (backButton != null)
                backButton.onClick.AddListener(ReturnToPauseMenu);
            
            if (loadAutosaveButton != null)
                loadAutosaveButton.onClick.AddListener(LoadAutosave);
            
            if (loadQuicksaveButton != null)
                loadQuicksaveButton.onClick.AddListener(LoadQuicksave);
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
            
            // Сортируем по времени (новые сначала)
            saves.Sort((a, b) => string.Compare(b.timestamp, a.timestamp));
            
            // Отображаем все сохранения
            foreach (var saveInfo in saves)
            {
                CreateSaveEntry(saveInfo);
            }
            
            // Показываем текст если нет сохранений
            if (noSavesText != null)
            {
                noSavesText.SetActive(saves.Count == 0);
            }
        }
        
        private void CreateSaveEntry(SaveGameInfo saveInfo)
        {
            if (saveEntryPrefab == null || savesContainer == null) return;
            
            var entryObj = Instantiate(saveEntryPrefab, savesContainer);
            _saveEntries.Add(entryObj);
            
            var entry = entryObj.GetComponent<LoadMenuEntryUI>();
            if (entry != null)
            {
                entry.Initialize(saveInfo, OnSaveSelected);
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
            LoadSave(saveInfo.saveName);
        }
        
        private void LoadAutosave()
        {
            LoadSave("autosave");
        }
        
        private void LoadQuicksave()
        {
            LoadSave("quicksave");
        }
        
        private void LoadSave(string saveName)
        {
            if (SaveManager.Instance == null) return;
            
            if (SaveManager.Instance.SaveExists(saveName))
            {
                // Закрываем меню паузы
                if (_pauseMenu != null)
                {
                    _pauseMenu.ResumeGame();
                }
                
                // Загружаем сохранение
                SaveManager.Instance.LoadGame(saveName);
                
                Debug.Log($"Загружено сохранение: {saveName}");
            }
            else
            {
                Debug.LogWarning($"Сохранение не найдено: {saveName}");
                // Можно показать сообщение об ошибке
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
            if (backButton != null)
                backButton.onClick.RemoveListener(ReturnToPauseMenu);
            
            if (loadAutosaveButton != null)
                loadAutosaveButton.onClick.RemoveListener(LoadAutosave);
            
            if (loadQuicksaveButton != null)
                loadQuicksaveButton.onClick.RemoveListener(LoadQuicksave);
        }
    }
}