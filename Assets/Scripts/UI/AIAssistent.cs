using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class AIAssistant : MonoBehaviour
{
    public static AIAssistant Instance;
    
    [Header("UI References")]
    [SerializeField] private GameObject assistantPanel;
    [SerializeField] private TextMeshProUGUI speechText;
    [SerializeField] private CanvasGroup speechBubble;
    [SerializeField] private RectTransform assistantRectTransform;
    
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 100f;
    [SerializeField] private float bounceHeight = 10f;
    [SerializeField] private float bounceSpeed = 2f;
    [SerializeField] private float screenBoundsPaddingX = 50f;
    [SerializeField] private float screenBoundsPaddingY = 50f;
    
    [Header("Animation Settings")]
    [SerializeField] private float fadeDuration = 0.3f;
    [SerializeField] private float showDuration = 3f;
    [SerializeField] private float typeSpeed = 0.05f;
    
    [Header("Comment Settings")]
    [SerializeField] private float minCommentDelay = 10f;
    [SerializeField] private float maxCommentDelay = 30f;
    
    [Header("Chances")]
    [SerializeField] private float combatCommentChance = 0.3f;
    [SerializeField] private float resourceCommentChance = 0.5f;
    [SerializeField] private float jumpChance = 0.1f;
    
    public enum AssistantMood { Neutral, Happy, Excited, Worried, Sarcastic }

    private AssistantMood currentMood = AssistantMood.Neutral;
    
    [System.Serializable]
    public class CommentCategory
    {
        public string categoryName;
        public List<string> comments;
    }
    
    [Header("Comment Database")]
    public List<CommentCategory> commentCategories;
    
    // Components
    private Animator animator;
    private Canvas parentCanvas;
    private Camera mainCamera;
    
    // Movement
    private Vector2 movementDirection;
    private float currentBounceOffset;
    private bool isSpeaking = false;
    private bool shouldMove = true;
    private float nextDirectionChangeTime;
    private float directionChangeInterval = 3f;
    
    // Speech
    private Dictionary<string, List<string>> commentsDict = new Dictionary<string, List<string>>();
    private List<string> recentlySpoken = new List<string>();
    private const int memorySize = 5;
    private Coroutine currentSpeechCoroutine;
    private Coroutine bounceCoroutine;
    private Coroutine movementCoroutine;
    
    // Player tracking
    private Vector3 lastPlayerWorldPosition;
    private Vector2 targetFollowPosition;
    
    // Система приоритетов
    public enum CommentPriority { Low, Normal, High, Critical }
    
    // События для комментариев
    public System.Action<string> OnAssistantSpeak;
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        
        animator = GetComponent<Animator>();
        parentCanvas = GetComponentInParent<Canvas>();
        mainCamera = Camera.main;
        
        if (assistantRectTransform == null)
            assistantRectTransform = GetComponent<RectTransform>();
        
        InitializeComments();
    }
    
    void Start()
    {
        assistantPanel.SetActive(false);
        speechBubble.alpha = 0;
        
        // Запускаем случайные комментарии и движение
        StartCoroutine(RandomCommentsRoutine());
        movementCoroutine = StartCoroutine(MovementRoutine());
        StartCoroutine(IdleActionsRoutine());
    }
    
    void Update()
    {
        HandleMovement();
        HandleBounceAnimation();
    }
    
    private void InitializeComments()
    {
        foreach (var category in commentCategories)
        {
            commentsDict[category.categoryName] = category.comments;
        }
    }
    
    private IEnumerator MovementRoutine()
    {
        while (true)
        {
            if (!isSpeaking && shouldMove)
            {
                // Случайно меняем направление
                movementDirection = new Vector2(Random.Range(-1f, 1f), 0).normalized;
                nextDirectionChangeTime = Time.time + Random.Range(directionChangeInterval * 0.5f, directionChangeInterval * 1.5f);
            }
            
            yield return new WaitForSeconds(1f);
        }
    }
    
    private IEnumerator IdleActionsRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(5f, 15f));
            
            if (!isSpeaking && shouldMove)
            {
                // Случайные анимации
                if (Random.value < 0.3f)
                {
                    animator.SetTrigger("Roll");
                }
                else if (Random.value < 0.2f)
                {
                    StartCoroutine(JumpRoutine());
                }
            }
        }
    }
    
    private void HandleMovement()
    {
        if (!shouldMove || isSpeaking) return;
        
        if (Time.time >= nextDirectionChangeTime)
        {
            movementDirection = new Vector2(Random.Range(-1f, 1f), 0).normalized;
            nextDirectionChangeTime = Time.time + Random.Range(directionChangeInterval * 0.5f, directionChangeInterval * 1.5f);
        }
        
        // Движение RectTransform
        Vector2 newPosition = assistantRectTransform.anchoredPosition + movementDirection * moveSpeed * Time.deltaTime;
        assistantRectTransform.anchoredPosition = newPosition;
        
        // Поворот спрайта в направлении движения
        if (movementDirection.x > 0)
            assistantRectTransform.localScale = new Vector3(1, 1, 1);
        else if (movementDirection.x < 0)
            assistantRectTransform.localScale = new Vector3(-1, 1, 1);
            
        animator.SetBool("IsMoving", movementDirection.magnitude > 0.1f);
        
        HandleScreenBounds();
    }
    
    private void HandleScreenBounds()
    {
        if (parentCanvas == null) return;
        
        Rect canvasRect = parentCanvas.pixelRect;
        Vector2 currentPos = assistantRectTransform.anchoredPosition;
        bool changedDirection = false;
        
        // Проверяем границы с учетом паддинга
        if (currentPos.x > screenBoundsPaddingX)
        {
            movementDirection.x = Mathf.Abs(movementDirection.x);
            changedDirection = true;
            currentPos.x = screenBoundsPaddingX;
        }
        else if (currentPos.x < canvasRect.width - screenBoundsPaddingX)
        {
            movementDirection.x = -Mathf.Abs(movementDirection.x);
            changedDirection = true;
            currentPos.x = canvasRect.width - screenBoundsPaddingX;
        }
        
        if (currentPos.y < screenBoundsPaddingY)
        {
            movementDirection.y = Mathf.Abs(movementDirection.y);
            changedDirection = true;
            currentPos.y = screenBoundsPaddingY;
        }
        else if (currentPos.y > canvasRect.height - screenBoundsPaddingY)
        {
            movementDirection.y = -Mathf.Abs(movementDirection.y);
            changedDirection = true;
            currentPos.y = canvasRect.height - screenBoundsPaddingY;
        }
        
        assistantRectTransform.anchoredPosition = currentPos;
        
        if (changedDirection)
        {
            nextDirectionChangeTime = Time.time + directionChangeInterval;
        }
    }
    
    private void HandleBounceAnimation()
    {
        if (isSpeaking && bounceCoroutine == null)
        {
            bounceCoroutine = StartCoroutine(BounceRoutine());
        }
        else if (!isSpeaking && bounceCoroutine != null)
        {
            StopCoroutine(bounceCoroutine);
            bounceCoroutine = null;
            // Плавно возвращаем на исходную позицию
            Vector2 currentPos = assistantRectTransform.anchoredPosition;
            assistantRectTransform.anchoredPosition = new Vector2(currentPos.x, currentPos.y - currentBounceOffset);
            currentBounceOffset = 0f;
        }
    }
    
    private IEnumerator BounceRoutine()
    {
        Vector2 startPos = assistantRectTransform.anchoredPosition;
        float time = 0f;
        
        while (isSpeaking)
        {
            currentBounceOffset = Mathf.Sin(time * bounceSpeed) * bounceHeight;
            assistantRectTransform.anchoredPosition = new Vector2(startPos.x, startPos.y + currentBounceOffset);
            time += Time.deltaTime;
            yield return null;
        }
    }
    
    private IEnumerator JumpRoutine()
    {
        Vector2 startPos = assistantRectTransform.anchoredPosition;
        float jumpHeight = 30f;
        float jumpDuration = 0.6f;
        
        // Прыжок вверх
        float elapsed = 0f;
        while (elapsed < jumpDuration / 2)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / (jumpDuration / 2);
            float yOffset = Mathf.Sin(progress * Mathf.PI) * jumpHeight;
            assistantRectTransform.anchoredPosition = new Vector2(startPos.x, startPos.y + yOffset);
            yield return null;
        }
        
        // Падение вниз
        elapsed = 0f;
        while (elapsed < jumpDuration / 2)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / (jumpDuration / 2);
            float yOffset = Mathf.Sin((1 - progress) * Mathf.PI) * jumpHeight;
            assistantRectTransform.anchoredPosition = new Vector2(startPos.x, startPos.y + yOffset);
            yield return null;
        }
        
        assistantRectTransform.anchoredPosition = startPos;
    }
    
    public void SetMood(AssistantMood newMood)
    {
        currentMood = newMood;
        animator.SetInteger("Mood", (int)newMood);
    }
    
    public void TriggerMoodChange(AssistantMood newMood, float duration = 0f)
    {
        SetMood(newMood);
        
        if (duration > 0)
        {
            StartCoroutine(RevertMoodAfterDelay(duration));
        }
    }
    
    private IEnumerator RevertMoodAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        SetMood(AssistantMood.Neutral);
    }
    
    private IEnumerator RandomCommentsRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(minCommentDelay, maxCommentDelay));
            
            if (!isSpeaking && !IsAnyImportantUIOpen())
            {
                SpeakRandomFromCategory("Random");
            }
        }
    }
    
    private string GetUniqueComment(string category)
    {
        if (!commentsDict.ContainsKey(category)) return "Category not found!";
        
        var availableComments = new List<string>(commentsDict[category]);
    
        // Убираем недавно сказанные комментарии
        foreach (var spoken in recentlySpoken)
        {
            availableComments.Remove(spoken);
        }
    
        if (availableComments.Count == 0)
        {
            // Если все сказали, сбрасываем память
            recentlySpoken.Clear();
            availableComments = new List<string>(commentsDict[category]);
        }
    
        string selected = availableComments[Random.Range(0, availableComments.Count)];
    
        // Добавляем в память
        recentlySpoken.Add(selected);
        if (recentlySpoken.Count > memorySize)
        {
            recentlySpoken.RemoveAt(0);
        }
    
        return selected;
    }
    
    public void Speak(string text)
    {
        if (isSpeaking) return;
        
        if (currentSpeechCoroutine != null)
            StopCoroutine(currentSpeechCoroutine);
            
        currentSpeechCoroutine = StartCoroutine(SpeechRoutine(text));
    }
    
    public void SpeakWithPriority(string text, CommentPriority priority = CommentPriority.Normal)
    {
        if (isSpeaking)
        {
            if (priority <= CommentPriority.Normal) return;
            StopSpeaking();
        }
        
        StartCoroutine(PrioritySpeechRoutine(text, priority));
    }
    
    private IEnumerator PrioritySpeechRoutine(string text, CommentPriority priority)
    {
        float duration = priority == CommentPriority.Critical ? 5f : showDuration;
        float speed = priority == CommentPriority.Critical ? 0.02f : typeSpeed;
        
        // Визуальные эффекты для важных сообщений
        if (priority == CommentPriority.Critical)
        {
            // Можно добавить мигание или изменение цвета
            TriggerMoodChange(AssistantMood.Worried, duration);
        }
        
        yield return StartCoroutine(SpeechRoutine(text, duration, speed));
    }
    
    private IEnumerator SpeechRoutine(string text, float duration = 0f, float speed = 0f)
    {
        if (duration == 0) duration = showDuration;
        if (speed == 0) speed = typeSpeed;
        
        isSpeaking = true;
        shouldMove = false;
        
        // Останавливаем движение
        animator.SetBool("IsMoving", false);
        animator.SetBool("IsSpeaking", true);
        
        // Показываем пузырь
        assistantPanel.SetActive(true);
        yield return StartCoroutine(FadeSpeechBubble(0f, 1f, fadeDuration));
        
        // Печатаем текст
        speechText.text = "";
        foreach (char c in text)
        {
            speechText.text += c;
            yield return new WaitForSeconds(speed);
        }
        
        // Ждем
        yield return new WaitForSeconds(duration);
        
        // Скрываем пузырь
        yield return StartCoroutine(FadeSpeechBubble(1f, 0f, fadeDuration));
        assistantPanel.SetActive(false);
        
        // Возобновляем движение
        animator.SetBool("IsSpeaking", false);
        isSpeaking = false;
        shouldMove = true;
        
        // Уведомляем подписчиков
        OnAssistantSpeak?.Invoke(text);
    }
    
    private IEnumerator FadeSpeechBubble(float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            speechBubble.alpha = Mathf.Lerp(from, to, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        speechBubble.alpha = to;
    }
    
    // Методы для вызова из других систем
    public void OnResourceCollected(ItemType itemType, int amount)
    {
        if (isSpeaking) return;

        if (Random.value < resourceCommentChance)
        {
            switch (itemType)
            {
                case ItemType.Crystal_Red:
                case ItemType.Crystal_Blue:
                    SpeakRandomFromCategory("CrystalCollection");
                    break;
                case ItemType.Metal:
                    SpeakRandomFromCategory("MetalCollection");
                    break;
                case ItemType.Wood:
                    SpeakRandomFromCategory("WoodCollection");
                    break;
                case ItemType.Stone:
                    SpeakRandomFromCategory("StoneCollection");
                    break;
            }
        }
    }
    
    public void OnEnemyKilled(string enemyType)
    {
        if (isSpeaking) return;
        
        if (Random.value < combatCommentChance)
        {
            SpeakRandomFromCategory("Combat");
        }
    }
    
    public void OnTechUnlocked(string techName)
    {
        if (isSpeaking) return;
        
        SpeakRandomFromCategory("Technology");
    }
    
    public void OnBuildingUpgraded(string buildingName)
    {
        if (isSpeaking) return;
        
        SpeakRandomFromCategory("Construction");
    }
    
    public void OnPlayerLowHealth()
    {
        if (isSpeaking) return;
        
        SpeakWithPriority(GetUniqueComment("LowHealth"), CommentPriority.High);
    }
    
    public void OnPlayerRich()
    {
        if (isSpeaking) return;
        
        if (Random.value < 0.2f)
        {
            SpeakRandomFromCategory("Wealth");
        }
    }
    
    public void OnPlayerEnterNewArea(string areaName)
    {
        if (isSpeaking) return;
        SpeakRandomFromCategory("NewArea");
    }
    
    public void OnTimeOfDayChanged(bool isNight)
    {
        if (isSpeaking) return;
        
        if (isNight && Random.value < 0.4f)
        {
            SpeakRandomFromCategory("NightTime");
        }
    }
    
    public void SpeakRandomFromCategory(string category)
    {
        if (commentsDict.ContainsKey(category) && commentsDict[category].Count > 0)
        {
            Speak(GetUniqueComment(category));
        }
    }
    
    private bool IsAnyImportantUIOpen()
    {
        // Проверка основных UI окон (нужно адаптировать под вашу игру)
        return false;
    }
    
    // Принудительно остановить речь
    public void StopSpeaking()
    {
        if (currentSpeechCoroutine != null)
        {
            StopCoroutine(currentSpeechCoroutine);
            isSpeaking = false;
            shouldMove = true;
            assistantPanel.SetActive(false);
            animator.SetBool("IsSpeaking", false);
            
            if (bounceCoroutine != null)
            {
                StopCoroutine(bounceCoroutine);
                bounceCoroutine = null;
            }
        }
    }
    
    public void SetMovementEnabled(bool enabled)
    {
        shouldMove = enabled;
        if (!enabled)
        {
            animator.SetBool("IsMoving", false);
        }
    }
    
    // Для сохранения/загрузки
    [System.Serializable]
    public class AssistantData
    {
        public AssistantMood currentMood;
        public List<string> spokenComments;
        public Vector2 position;
    }
    
    public AssistantData GetSaveData()
    {
        return new AssistantData
        {
            currentMood = currentMood,
            spokenComments = recentlySpoken,
            position = assistantRectTransform.anchoredPosition
        };
    }
    
    public void LoadData(AssistantData data)
    {
        if (data == null) return;
        
        SetMood(data.currentMood);
        recentlySpoken = data.spokenComments ?? new List<string>();
        assistantRectTransform.anchoredPosition = data.position;
    }
}