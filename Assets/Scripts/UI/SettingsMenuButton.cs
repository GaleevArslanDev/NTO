using Gameplay.Characters.Player;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class SettingsMenuButton : MonoBehaviour
    {
        [SerializeField] private Button settingsButton;
        [SerializeField] private KeyCode hotkey = KeyCode.Escape;
        [SerializeField] private GameObject settingsPanel;

        private bool _isSettingsOpen;

        private void Start()
        {
            if (settingsButton != null)
            {
                settingsButton.onClick.AddListener(ToggleSettings);
            }

            if (settingsPanel != null)
            {
                settingsPanel.SetActive(false);
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(hotkey))
            {
                ToggleSettings();
            }
        }

        public void ToggleSettings()
        {
            _isSettingsOpen = !_isSettingsOpen;

            if (settingsPanel != null)
            {
                settingsPanel.SetActive(_isSettingsOpen);
                
                if (_isSettingsOpen)
                {
                    // Инициализируем SettingsUI
                    var settingsUI = settingsPanel.GetComponent<SettingsUI>();
                    if (settingsUI != null)
                    {
                        settingsUI.ShowSettings();
                    }
                    
                    // Блокируем управление игроком
                    if (PlayerController.Instance != null)
                    {
                        PlayerController.Instance.SetControlEnabled(false);
                    }
                }
                else
                {
                    // Восстанавливаем управление
                    if (PlayerController.Instance != null)
                    {
                        PlayerController.Instance.SetControlEnabled(true);
                    }
                }
            }
        }

        public void CloseSettings()
        {
            _isSettingsOpen = false;
            if (settingsPanel != null)
            {
                settingsPanel.SetActive(false);
            }
            
            // Восстанавливаем управление
            if (PlayerController.Instance != null)
            {
                PlayerController.Instance.SetControlEnabled(true);
            }
        }

        private void OnDestroy()
        {
            if (settingsButton != null)
            {
                settingsButton.onClick.RemoveListener(ToggleSettings);
            }
        }
    }
}