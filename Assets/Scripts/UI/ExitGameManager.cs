using Gameplay.Systems;
using UnityEngine;

namespace UI
{
    public class ExitGameManager : MonoBehaviour
    {
        public static ExitGameManager Instance;
        
        public GameObject exitConfirmationPanel;
        [SerializeField] private float saveDelay = 0.5f;
        
        private bool _isExiting = false;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                
                // Подписываемся на событие выхода
                Application.wantsToQuit += OnWantsToQuit;
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        private void OnDestroy()
        {
            Application.wantsToQuit -= OnWantsToQuit;
        }
        
        public bool OnWantsToQuit()
        {
            // Если уже выходим - разрешаем
            if (_isExiting) return true;
            
            // Если диалог уже показан - блокируем
            if (exitConfirmationPanel != null && exitConfirmationPanel.activeInHierarchy)
                return false;
            
            Debug.Log("Quit request intercepted - showing confirmation dialog");
            
            // Показываем диалог подтверждения
            ShowExitConfirmation();
            
            // Блокируем выход
            return false;
        }
        
        public void ShowExitConfirmation()
        {
            if (exitConfirmationPanel != null && !exitConfirmationPanel.activeInHierarchy)
            {
                exitConfirmationPanel.SetActive(true);
                
                if (UIManager.Instance != null)
                    UIManager.Instance.RegisterUIOpen();
            }
        }
        
        public void ConfirmExit()
        {
            _isExiting = true;
            Debug.Log("User confirmed exit - performing final operations");
            
            // Запускаем корутину для безопасного выхода
            StartCoroutine(ExitRoutine());
        }
        
        public void CancelExit()
        {
            if (exitConfirmationPanel != null)
            {
                exitConfirmationPanel.SetActive(false);
                
                if (UIManager.Instance != null)
                    UIManager.Instance.RegisterUIClose();
                    
                Debug.Log("Exit cancelled by user");
            }
        }
        
        private System.Collections.IEnumerator ExitRoutine()
        {
            // Показываем индикатор сохранения если есть
            if (SaveManager.Instance != null && SaveManager.Instance.saveIndicator != null)
            {
                SaveManager.Instance.saveIndicator.SetActive(true);
            }
            
            // Выполняем финальное сохранение
            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.SaveGame("autosave");
            }
            
            // Ждем завершения сохранения
            yield return new WaitForSeconds(saveDelay);
            
            // Выходим из приложения
            QuitApplication();
        }
        
        private void QuitApplication()
        {
            Debug.Log("Quitting application...");
            
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #else
            Application.Quit();
            #endif
        }
    }
}