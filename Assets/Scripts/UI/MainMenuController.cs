using Gameplay.Systems;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

namespace UI
{
    public class MainMenuController : MonoBehaviour
    {
        [Header("Scene Names")]
        [SerializeField] private string gameSceneName = "GameScene";
        
        [Header("UI References")]
        [SerializeField] private Button newGameButton;
        [SerializeField] private Button loadGameButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button exitButton;
        [SerializeField] private GameObject noSavesText;
        
        [Header("Save Selection")]
        [SerializeField] private GameObject saveSelectionPanel;
        [SerializeField] private Transform savesContainer;
        [SerializeField] private GameObject saveEntryPrefab;
        [SerializeField] private Button closeSaveSelectionButton;
        
        [Header("Settings")]
        [SerializeField] private GameObject settingsPanel;
        
        private void Start()
        {
            // Настройка кнопок
            newGameButton.onClick.AddListener(StartNewGame);
            loadGameButton.onClick.AddListener(ShowSaveSelection);
            settingsButton.onClick.AddListener(ShowSettings);
            exitButton.onClick.AddListener(ExitGame);
            closeSaveSelectionButton.onClick.AddListener(HideSaveSelection);
            
            // Инициализация
            saveSelectionPanel.SetActive(false);
            
            // Создаем SaveManager если его нет
            EnsureSaveManagerExists();
            //LocalizationManager.LocalizationManager.Instance.Initialize();
        }
        
        private void EnsureSaveManagerExists()
        {
            if (SaveManager.Instance == null)
            {
                GameObject saveManagerObj = new GameObject("SaveManager");
                saveManagerObj.AddComponent<SaveManager>();
                // Настройки по умолчанию можно установить здесь
            }
        }
        
        private void StartNewGame()
        {
            // Очищаем данные о загрузке сохранения
            Core.StaticSaveData.Clear();
            
            // Загружаем игровую сцену
            SceneManager.LoadScene(gameSceneName);
        }
        
        private void ShowSaveSelection()
        {
            saveSelectionPanel.SetActive(true);
            RefreshSaveList();
        }
        
        private void HideSaveSelection()
        {
            saveSelectionPanel.SetActive(false);
        }
        
        private void RefreshSaveList()
        {
            // Очищаем контейнер
            foreach (Transform child in savesContainer)
            {
                Destroy(child.gameObject);
            }
            
            // Получаем список сохранений
            var saves = SaveManager.Instance.GetSaveGames();
            
            if (saves.Count == 0)
            {
                if (noSavesText != null)
                {
                    noSavesText.SetActive(true);
                }
            }
            else
            {
                if (noSavesText != null) noSavesText.SetActive(false);
            }
            
            // Создаем элементы списка
            foreach (var saveInfo in saves)
            {
                var entryObj = Instantiate(saveEntryPrefab, savesContainer);
                var entry = entryObj.GetComponent<MainMenuSaveEntryUI>();
                if (entry != null)
                {
                    // Используем адаптированную версию для главного меню
                    entry.Initialize(saveInfo, () => SelectSaveForLoad(saveInfo.saveName));
                }
            }
        }
        
        private void SelectSaveForLoad(string saveName)
        {
            // Сохраняем имя сохранения для загрузки
            Core.StaticSaveData.SaveToLoad = saveName;
            
            // Скрываем панель выбора
            HideSaveSelection();
            
            // Загружаем игровую сцену
            SceneManager.LoadScene(gameSceneName);
            
            Debug.Log($"Save selected for load: {saveName}");
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
        
        private void ShowSettings()
        {
            if (settingsPanel != null)
            {
                settingsPanel.SetActive(true);
        
                // Инициализируем SettingsUI если он есть в панели
                var settingsUI = settingsPanel.GetComponent<SettingsUI>();
                if (settingsUI != null)
                {
                    settingsUI.ShowSettings();
                }
            }
        }
        
        private void ExitGame()
        {
            Application.Quit();
            
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }
    }
}