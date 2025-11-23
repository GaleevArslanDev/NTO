using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;
    
    private int openUICount = 0;
    
    void Awake()
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
        openUICount++;
        UpdateCursorState();
    }
    
    public void RegisterUIClose()
    {
        openUICount--;
        if (openUICount < 0) openUICount = 0;
        UpdateCursorState();
    }
    
    private void UpdateCursorState()
    {
        if (openUICount > 0)
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
        return openUICount > 0 || 
               (TechTreeUI.Instance != null && TechTreeUI.Instance.isUIOpen) ||
               (TownHallUI.Instance != null && TownHallUI.Instance.isUiOpen) ||
               (QuestBoardUI.Instance != null && QuestBoardUI.Instance.isUIOpen) ||
               (DialogueManager.Instance != null && DialogueManager.Instance.IsInDialogue);
    }
}