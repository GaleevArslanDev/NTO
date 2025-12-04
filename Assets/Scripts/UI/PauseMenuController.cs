using UnityEngine;
using UnityEngine.UI;
using Gameplay.Characters.Player;
using Gameplay.Systems;
using UnityEngine.SceneManagement;
using TMPro;

namespace UI
{
    public class PauseMenuController : MonoBehaviour
    {
        [Header("Панели меню")]
        [SerializeField] private GameObject pauseMenuPanel;
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private GameObject saveMenuPanel;
        [SerializeField] private GameObject loadMenuPanel;
        
        [Header("Кнопки основного меню")]
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button saveButton;
        [SerializeField] private Button loadButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button mainMenuButton;
        [SerializeField] private Button exitButton;
        
        [Header("Горячие клавиши")]
        [SerializeField] private KeyCode pauseKey = KeyCode.Escape;
        [SerializeField] private bool allowPauseInGame = true;
        
        private bool _isPaused;
        private GameObject _currentSubMenu;
        
        private void Start()
        {
            InitializeButtons();
            HideAllPanels();
            
            // Подписываемся на события завершения загрузки сцены
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        
        private void InitializeButtons()
        {
            if (resumeButton != null)
                resumeButton.onClick.AddListener(ResumeGame);
            
            if (saveButton != null)
                saveButton.onClick.AddListener(OpenSaveMenu);
            
            if (loadButton != null)
                loadButton.onClick.AddListener(OpenLoadMenu);
            
            if (settingsButton != null)
                settingsButton.onClick.AddListener(OpenSettings);
            
            if (mainMenuButton != null)
                mainMenuButton.onClick.AddListener(ReturnToMainMenu);
            
            if (exitButton != null)
                exitButton.onClick.AddListener(ExitGame);
        }
        
        private void Update()
        {
            if (!allowPauseInGame) return;
            
            if (Input.GetKeyDown(pauseKey))
            {
                if (_isPaused)
                {
                    // Если открыто подменю, закрываем его
                    if (_currentSubMenu != null && _currentSubMenu.activeSelf)
                    {
                        CloseSubMenu();
                    }
                    else
                    {
                        ResumeGame();
                    }
                }
                else
                {
                    PauseGame();
                }
            }
        }
        
        public void PauseGame()
        {
            if (_isPaused) return;
            
            _isPaused = true;
            Time.timeScale = 0f;
            
            if (pauseMenuPanel != null)
            {
                pauseMenuPanel.SetActive(true);
            }
            
            // Блокируем управление игроком
            if (PlayerController.Instance != null)
            {
                PlayerController.Instance.SetControlEnabled(false);
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            
            Debug.Log("Игра поставлена на паузу");
        }
        
        public void ResumeGame()
        {
            if (!_isPaused) return;
            
            _isPaused = false;
            Time.timeScale = 1f;
            
            HideAllPanels();
            
            // Восстанавливаем управление игроком
            if (PlayerController.Instance != null)
            {
                PlayerController.Instance.SetControlEnabled(true);
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            
            Debug.Log("Игра возобновлена");
        }
        
        private void OpenSaveMenu()
        {
            if (saveMenuPanel == null) return;
            
            // Отключаем основное меню паузы
            if (pauseMenuPanel != null)
                pauseMenuPanel.SetActive(false);
            
            // Включаем меню сохранения
            saveMenuPanel.SetActive(true);
            _currentSubMenu = saveMenuPanel;
            
            // Обновляем список сохранений
            var saveMenu = saveMenuPanel.GetComponent<SaveMenuUI>();
            if (saveMenu != null)
            {
                saveMenu.RefreshSaveList();
            }
        }
        
        private void OpenLoadMenu()
        {
            if (loadMenuPanel == null) return;
            
            // Отключаем основное меню паузы
            if (pauseMenuPanel != null)
                pauseMenuPanel.SetActive(false);
            
            // Включаем меню загрузки
            loadMenuPanel.SetActive(true);
            _currentSubMenu = loadMenuPanel;
            
            // Обновляем список сохранений
            var loadMenu = loadMenuPanel.GetComponent<LoadMenuUI>();
            if (loadMenu != null)
            {
                loadMenu.RefreshSaveList();
            }
        }
        
        private void OpenSettings()
        {
            if (settingsPanel == null) return;
            
            // Отключаем основное меню паузы
            if (pauseMenuPanel != null)
                pauseMenuPanel.SetActive(false);
            
            // Включаем настройки
            settingsPanel.SetActive(true);
            _currentSubMenu = settingsPanel;
            
            // Инициализируем настройки
            var settingsUI = settingsPanel.GetComponent<SettingsUI>();
            if (settingsUI != null)
            {
                settingsUI.ShowSettings();
            }
        }
        
        public void CloseSubMenu()
        {
            if (_currentSubMenu != null)
            {
                _currentSubMenu.SetActive(false);
                _currentSubMenu = null;
            }
            
            // Возвращаем основное меню паузы
            if (pauseMenuPanel != null)
                pauseMenuPanel.SetActive(true);
        }
        
        private void HideAllPanels()
        {
            if (pauseMenuPanel != null)
                pauseMenuPanel.SetActive(false);
            
            if (settingsPanel != null)
                settingsPanel.SetActive(false);
            
            if (saveMenuPanel != null)
                saveMenuPanel.SetActive(false);
            
            if (loadMenuPanel != null)
                loadMenuPanel.SetActive(false);
            
            _currentSubMenu = null;
        }
        
        private void ReturnToMainMenu()
        {
            // Восстанавливаем время
            Time.timeScale = 1f;
            
            // Загружаем главное меню
            SceneManager.LoadScene("MainMenu");
        }
        
        private void ExitGame()
        {
            ExitGameManager.Instance.OnWantsToQuit();
        }
        
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // При загрузке игровой сцены убеждаемся, что игра не на паузе
            if (scene.name != "MainMenu")
            {
                _isPaused = false;
                Time.timeScale = 1f;
                HideAllPanels();
            }
        }
        
        public bool IsPaused => _isPaused;
        
        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            
            // Отписываемся от событий кнопок
            if (resumeButton != null)
                resumeButton.onClick.RemoveListener(ResumeGame);
            
            if (saveButton != null)
                saveButton.onClick.RemoveListener(OpenSaveMenu);
            
            if (loadButton != null)
                loadButton.onClick.RemoveListener(OpenLoadMenu);
            
            if (settingsButton != null)
                settingsButton.onClick.RemoveListener(OpenSettings);
            
            if (mainMenuButton != null)
                mainMenuButton.onClick.RemoveListener(ReturnToMainMenu);
            
            if (exitButton != null)
                exitButton.onClick.RemoveListener(ExitGame);
        }
    }
}