using Gameplay.Characters.Player;
using Gameplay.Dialogue;
using UnityEngine;

namespace UI
{
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance;
    
        private int _openUICount;

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
    
        public void RegisterUIOpen()
        {
            _openUICount++;
            UpdateCursorState();
        }
    
        public void RegisterUIClose()
        {
            _openUICount--;
            if (_openUICount < 0) _openUICount = 0;
            UpdateCursorState();
        }
    
        private void UpdateCursorState()
        {
            if (_openUICount > 0)
            {
                // Есть открытые UI - разблокируем курсор
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            
                // Блокируем управление игроком
                if (PlayerController.Instance != null)
                    PlayerController.Instance.SetControlEnabled(false);
            }
            else
            {
                // Нет открытых UI - блокируем курсор
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            
                // Разблокируем управление игроком
                if (PlayerController.Instance != null)
                    PlayerController.Instance.SetControlEnabled(true);
            }
        }
    
        public bool IsAnyUIOpen()
        {
            return _openUICount > 0 || 
                   (TechTreeUI.Instance != null && TechTreeUI.Instance.isUIOpen) ||
                   (TownHallUI.Instance != null && TownHallUI.Instance.isUiOpen) ||
                   (QuestBoardUI.Instance != null && QuestBoardUI.Instance.isUIOpen) ||
                   (DialogueManager.Instance != null && DialogueManager.Instance.IsInDialogue) ||
                   (ExitGameManager.Instance != null && ExitGameManager.Instance.exitConfirmationPanel.activeInHierarchy);
        }
    }
}