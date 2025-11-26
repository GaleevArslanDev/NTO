using System.Collections.Generic;
using System.Linq;
using Gameplay.Systems;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class SaveLoadUI : MonoBehaviour
    {
        public static SaveLoadUI Instance;
        
        [Header("UI References")]
        public GameObject saveLoadPanel;
        public Transform savesContainer;
        public GameObject saveEntryPrefab;
        public TMP_InputField saveNameInput;
        public Button newSaveButton;
        public Button loadButton;
        public Button deleteButton;
        public Button closeButton;
        
        [Header("Display")]
        public TMP_Text selectedSaveText;
        public TMP_Text saveDetailsText;
        
        private List<SaveGameInfo> _currentSaves = new();
        private string _selectedSave;
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        private void Start()
        {
            newSaveButton.onClick.AddListener(CreateNewSave);
            loadButton.onClick.AddListener(LoadSelectedSave);
            deleteButton.onClick.AddListener(DeleteSelectedSave);
            closeButton.onClick.AddListener(ClosePanel);
            
            saveLoadPanel.SetActive(false);
        }
        
        public void ShowSaveLoadPanel()
        {
            saveLoadPanel.SetActive(true);
            RefreshSaveList();
            _selectedSave = null;
            UpdateSelectionDisplay();
            
            if (UIManager.Instance != null)
                UIManager.Instance.RegisterUIOpen();
        }
        
        public void ClosePanel()
        {
            saveLoadPanel.SetActive(false);
            
            if (UIManager.Instance != null)
                UIManager.Instance.RegisterUIClose();
        }
        
        private void RefreshSaveList()
        {
            // Очищаем контейнер
            foreach (Transform child in savesContainer)
            {
                Destroy(child.gameObject);
            }
            
            // Получаем список сохранений
            _currentSaves = SaveManager.Instance.GetSaveGames();
            
            // Создаем элементы UI
            foreach (var saveInfo in _currentSaves)
            {
                var entryObj = Instantiate(saveEntryPrefab, savesContainer);
                var entry = entryObj.GetComponent<SaveEntryUI>();
                if (entry != null)
                {
                    entry.Initialize(saveInfo, this);
                }
            }
        }
        
        public void SelectSave(string saveName)
        {
            _selectedSave = saveName;
            UpdateSelectionDisplay();
        }
        
        private void UpdateSelectionDisplay()
        {
            var saveInfo = _currentSaves.FirstOrDefault(s => s.saveName == _selectedSave);
            
            if (saveInfo != null)
            {
                selectedSaveText.text = $"Выбрано: {saveInfo.saveName}";
                saveDetailsText.text = $"Версия: {saveInfo.gameVersion}\n" +
                                     $"Уровень: {saveInfo.playerLevel}\n" +
                                     $"Время: {saveInfo.timestamp}\n" +
                                     $"Локация: {saveInfo.location}";
                
                loadButton.interactable = true;
                deleteButton.interactable = true;
            }
            else
            {
                selectedSaveText.text = "Выберите сохранение";
                saveDetailsText.text = "";
                loadButton.interactable = false;
                deleteButton.interactable = false;
            }
        }
        
        private void CreateNewSave()
        {
            var saveName = saveNameInput.text;
            if (string.IsNullOrWhiteSpace(saveName))
            {
                saveName = "Новое сохранение";
            }
            
            SaveManager.Instance.CreateManualSave(saveName);
            RefreshSaveList();
            saveNameInput.text = "";
        }
        
        private void LoadSelectedSave()
        {
            if (!string.IsNullOrEmpty(_selectedSave))
            {
                SaveManager.Instance.LoadGame(_selectedSave);
                ClosePanel();
            }
        }
        
        private void DeleteSelectedSave()
        {
            if (!string.IsNullOrEmpty(_selectedSave))
            {
                SaveManager.Instance.DeleteSave(_selectedSave);
                RefreshSaveList();
                _selectedSave = null;
                UpdateSelectionDisplay();
            }
        }
        
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape) && saveLoadPanel.activeInHierarchy)
            {
                ClosePanel();
            }
        }
    }
}