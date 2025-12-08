using UnityEngine;
using TMPro; // Если используете TextMeshPro
using UnityEngine.UI;

public class StartTutorialMenu : MonoBehaviour
{
    [Header("Настройки меню")]
    [SerializeField] private GameObject tutorialPanel;
    [SerializeField] private TextMeshProUGUI tutorialText; // или Text если используете обычный UI
    [SerializeField] private string localizationKey = "full_tutorial";
    [SerializeField] private bool showOnlyOnce = true;
    [SerializeField] private KeyCode closeKey1 = KeyCode.Escape;
    [SerializeField] private KeyCode closeKey2 = KeyCode.E;

    [Header("Настройки игры")]
    [SerializeField] private bool pauseGameWhenOpen = true;

    private bool hasBeenShown = false;
    private bool isActive = false;

    void Start()
    {
        // Проверяем, показывали ли уже меню
        if (showOnlyOnce)
        {
            hasBeenShown = PlayerPrefs.GetInt("TutorialShown", 0) == 1;
        }

        // Если меню еще не показывали (или показываем всегда) - показываем
        if (!hasBeenShown || !showOnlyOnce)
        {
            ShowTutorial();
        }
        else
        {
            // Если уже показывали - сразу скрываем панель
            if (tutorialPanel != null)
                tutorialPanel.SetActive(false);
        }
    }

    void Update()
    {
        // Если меню активно - проверяем нажатие клавиш закрытия
        if (isActive && (Input.GetKeyDown(closeKey1) || Input.GetKeyDown(closeKey2)))
        {
            HideTutorial();
        }
    }

    void ShowTutorial()
    {
        // Активируем панель
        if (tutorialPanel != null)
        {
            tutorialPanel.SetActive(true);
            isActive = true;

            // Загружаем текст из локализации
            LoadTutorialText();

            // Пауза игры если нужно
            if (pauseGameWhenOpen)
            {
                Time.timeScale = 0f;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

            // Сохраняем факт показа
            if (showOnlyOnce)
            {
                PlayerPrefs.SetInt("TutorialShown", 1);
                PlayerPrefs.Save();
            }
        }
    }

    void LoadTutorialText()
    {
        if (tutorialText == null) return;

        
        else
        {
            // Если нет локализации, можно задать текст прямо здесь
            string defaultText = "Эй, вставай! Нет времени объяснять. Валим отсюда, пока здесь всё не обвалилось. Видишь инструмент в твоей правой руке? Это МТБ — мультитул. Он добывает ресурсы, отстреливается и переводит речь инопланетян.\n\nУправление:\nWASD - передвижение\nCTRL - присесть\nSPACE - прыжок\nE - диалог с NPC\nF - прокачка";

            // Или загрузить из ресурсов
            TextAsset textAsset = Resources.Load<TextAsset>("Tutorials/tutorial_text");
            if (textAsset != null)
            {
                tutorialText.text = textAsset.text;
            }
            else
            {
                tutorialText.text = defaultText;
            }
        }
    }

    public void HideTutorial()
    {
        if (tutorialPanel != null)
        {
            tutorialPanel.SetActive(false);
            isActive = false;

            // Возобновляем игру
            if (pauseGameWhenOpen)
            {
                Time.timeScale = 1f;
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
    }

    // Метод для принудительного показа (например, из другого скрипта)
    public void ForceShowTutorial()
    {
        ShowTutorial();
    }

    // Метод для сброса состояния (для тестирования или в настройках)
    public void ResetTutorial()
    {
        PlayerPrefs.DeleteKey("TutorialShown");
        hasBeenShown = false;
    }

    void OnDestroy()
    {
        // При уничтожении объекта убедимся, что время вернулось в нормальное состояние
        if (isActive && pauseGameWhenOpen)
        {
            Time.timeScale = 1f;
        }
    }
}