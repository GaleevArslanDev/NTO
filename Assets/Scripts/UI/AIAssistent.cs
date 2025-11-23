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
    
    private Dictionary<string, List<string>> commentsDict = new Dictionary<string, List<string>>();
    private List<string> recentlySpoken = new List<string>();
    private const int memorySize = 5;
    private Coroutine currentSpeechCoroutine;
    private bool isSpeaking = false;
    
    // События для комментариев
    public System.Action<string> OnAssistantSpeak;
    
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
        
        InitializeComments();
    }
    
    void Start()
    {
        assistantPanel.SetActive(false);
        speechBubble.alpha = 0;
        
        // Запускаем случайные комментарии
        StartCoroutine(RandomCommentsRoutine());
    }
    
    public void SetMood(AssistantMood newMood)
    {
        currentMood = newMood;
        // Можно менять цвет текста или анимацию в зависимости от настроения
    }
    
    private void InitializeComments()
    {
        foreach (var category in commentCategories)
        {
            commentsDict[category.categoryName] = category.comments;
        }
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
    
    public void SpeakRandomFromCategory(string category)
    {
        if (commentsDict.ContainsKey(category) && commentsDict[category].Count > 0)
        {
            Speak(GetUniqueComment(category));
        }
    }
    
    private IEnumerator SpeechRoutine(string text)
    {
        isSpeaking = true;
        
        // Показываем пузырь
        assistantPanel.SetActive(true);
        yield return StartCoroutine(FadeSpeechBubble(0f, 1f, fadeDuration));
        
        // Печатаем текст
        speechText.text = "";
        foreach (char c in text)
        {
            speechText.text += c;
            yield return new WaitForSeconds(typeSpeed);
        }
        
        // Ждем
        yield return new WaitForSeconds(showDuration);
        
        // Скрываем пузырь
        yield return StartCoroutine(FadeSpeechBubble(1f, 0f, fadeDuration));
        assistantPanel.SetActive(false);
        
        isSpeaking = false;
        
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
        
        SpeakRandomFromCategory("LowHealth");
    }
    
    public void OnPlayerRich()
    {
        if (isSpeaking) return;
        
        if (Random.value < 0.2f) // 20% шанс
        {
            SpeakRandomFromCategory("Wealth");
        }
    }
    
    private bool IsAnyImportantUIOpen()
    {
        return (DialogueManager.Instance != null && DialogueManager.Instance.IsInDialogue) ||
               (TechTreeUI.Instance != null && TechTreeUI.Instance.isUIOpen) ||
               (TownHallUI.Instance != null && TownHallUI.Instance.isUiOpen);
    }
    
    // Принудительно остановить речь
    public void StopSpeaking()
    {
        if (currentSpeechCoroutine != null)
        {
            StopCoroutine(currentSpeechCoroutine);
            isSpeaking = false;
            assistantPanel.SetActive(false);
        }
    }
}