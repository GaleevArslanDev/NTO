using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class DialogueUI : MonoBehaviour
{
    public static DialogueUI Instance;
    
    [Header("Main References")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TextMeshProUGUI npcNameText;
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private Transform optionsContainer;
    [SerializeField] private GameObject optionButtonPrefab;
    [SerializeField] private Image npcPortrait;
    [SerializeField] private Image emotionIcon;
    
    [Header("Animation Settings")]
    [SerializeField] private float typeWriterSpeed = 0.05f;
    [SerializeField] private float fadeDuration = 0.3f;
    
    [Header("Emotion Icons")]
    [SerializeField] private Sprite neutralIcon;
    [SerializeField] private Sprite happyIcon;
    [SerializeField] private Sprite sadIcon;
    [SerializeField] private Sprite angryIcon;
    [SerializeField] private Sprite surprisedIcon;
    [SerializeField] private Sprite confusedIcon;
    [SerializeField] private Sprite thoughtfulIcon;
    
    [Header("Audio")]
    [SerializeField] private AudioSource voiceSource;
    
    private CanvasGroup canvasGroup;
    private bool isTyping = false;
    private Coroutine typingCoroutine;
    
    private void Start()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
        
        canvasGroup = dialoguePanel.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = dialoguePanel.AddComponent<CanvasGroup>();
            
        dialoguePanel.SetActive(false);
        
        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.OnDialogueStarted += OnDialogueStarted;
            DialogueManager.Instance.OnDialogueNodeChanged += OnDialogueNodeChanged;
            DialogueManager.Instance.OnDialogueEnded += OnDialogueEnded;
        }
    }
    
    private void OnDialogueStarted(DialogueNode node, string npcName)
    {
        ShowDialogue();
        UpdateDialogueUI(node, npcName);
    }
    
    private void OnDialogueNodeChanged(DialogueNode node)
    {
        UpdateDialogueUI(node, DialogueManager.Instance.CurrentNPCName);
    }
    
    private void OnDialogueEnded()
    {
        HideDialogue();
    }
    
    public void ShowDialogue()
    {
        dialoguePanel.SetActive(true);
        StartCoroutine(FadeIn());
    
        // Используем UIManager
        if (UIManager.Instance != null)
            UIManager.Instance.RegisterUIOpen();
    }

    public void HideDialogue()
    {
        StartCoroutine(FadeOut());
    
        // Используем UIManager
        if (UIManager.Instance != null)
            UIManager.Instance.RegisterUIClose();
    }
    
    private IEnumerator FadeIn()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0;
            while (canvasGroup.alpha < 1)
            {
                canvasGroup.alpha += Time.deltaTime / fadeDuration;
                yield return null;
            }
        }
    }
    
    private IEnumerator FadeOut()
    {
        if (canvasGroup != null)
        {
            while (canvasGroup.alpha > 0)
            {
                canvasGroup.alpha -= Time.deltaTime / fadeDuration;
                yield return null;
            }
        }
        dialoguePanel.SetActive(false);
    }
    
    private void UpdateDialogueUI(DialogueNode node, string npcName)
    {
        if (node == null) return;
        
        npcNameText.text = npcName;
        SetEmotionIcon(node.emotion);
        
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);
            
        typingCoroutine = StartCoroutine(TypeText(node.npcText, node.typingSpeed));
        
        if (node.voiceLine != null && voiceSource != null)
        {
            voiceSource.PlayOneShot(node.voiceLine);
        }
        
        UpdateOptions();
    }
    
    private IEnumerator TypeText(string text, float speed)
    {
        isTyping = true;
        dialogueText.text = "";
        
        float actualSpeed = speed > 0 ? speed : typeWriterSpeed;
        
        foreach (char c in text)
        {
            dialogueText.text += c;
            yield return new WaitForSeconds(actualSpeed);
        }
        
        isTyping = false;
    }
    
    public void SkipTyping()
    {
        if (isTyping && typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            if (DialogueManager.Instance.CurrentNode != null)
            {
                dialogueText.text = DialogueManager.Instance.CurrentNode.npcText;
            }
            isTyping = false;
        }
    }
    
    private void SetEmotionIcon(Emotion emotion)
    {
        if (emotionIcon == null) return;
        
        Sprite icon = neutralIcon;
        
        switch (emotion)
        {
            case Emotion.Happy: icon = happyIcon; break;
            case Emotion.Sad: icon = sadIcon; break;
            case Emotion.Angry: icon = angryIcon; break;
            case Emotion.Surprised: icon = surprisedIcon; break;
            case Emotion.Confused: icon = confusedIcon; break;
            case Emotion.Thoughtful: icon = thoughtfulIcon; break;
        }
        
        emotionIcon.sprite = icon;
        emotionIcon.gameObject.SetActive(icon != null);
    }
    
    private void UpdateOptions()
    {
        foreach (Transform child in optionsContainer)
        {
            Destroy(child.gameObject);
        }
        
        var options = DialogueManager.Instance.GetCurrentOptions();
        
        if (options.Length == 0)
        {
            CreateExitOption();
        }
        else
        {
            for (int i = 0; i < options.Length; i++)
            {
                CreateOptionButton(options[i], i);
            }
        }
    }
    
    private void CreateOptionButton(DialogueOption option, int index)
    {
        if (optionButtonPrefab == null) return;
        
        GameObject buttonObj = Instantiate(optionButtonPrefab, optionsContainer);
        Button button = buttonObj.GetComponent<Button>();
        TextMeshProUGUI buttonText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
        
        if (buttonText != null)
            buttonText.text = option.optionText;
        
        SetupOptionColor(button, option);
        
        button.onClick.AddListener(() => OnOptionSelected(option));
        
        SetupButtonNavigation(button, index);
    }
    
    private void CreateExitOption()
    {
        if (optionButtonPrefab == null) return;
        
        GameObject buttonObj = Instantiate(optionButtonPrefab, optionsContainer);
        Button button = buttonObj.GetComponent<Button>();
        TextMeshProUGUI buttonText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
        
        if (buttonText != null)
            buttonText.text = "[Попрощаться]";
        button.onClick.AddListener(() => DialogueManager.Instance.EndDialogue());
    }
    
    private void SetupOptionColor(Button button, DialogueOption option)
    {
        if (button == null) return;
        
        var colors = button.colors;
        
        if (option.relationshipChange > 0)
        {
            colors.normalColor = new Color(0.2f, 0.8f, 0.2f, 0.7f);
        }
        else if (option.relationshipChange < 0)
        {
            colors.normalColor = new Color(0.8f, 0.2f, 0.2f, 0.7f);
        }
        else
        {
            colors.normalColor = new Color(0.3f, 0.3f, 0.8f, 0.7f);
        }
        
        button.colors = colors;
    }
    
    private void SetupButtonNavigation(Button button, int index)
    {
        if (button == null) return;
        
        var navigation = button.navigation;
        navigation.mode = Navigation.Mode.Explicit;
        
        if (index > 0)
        {
            navigation.selectOnUp = optionsContainer.GetChild(index - 1).GetComponent<Button>();
        }
        if (index < optionsContainer.childCount - 1)
        {
            navigation.selectOnDown = optionsContainer.GetChild(index + 1).GetComponent<Button>();
        }
        
        button.navigation = navigation;
    }
    
    private void OnOptionSelected(DialogueOption option)
    {
        if (isTyping) return;
        
        DialogueManager.Instance.SelectOption(option);
    }
    
    private void Update()
    {
        if (Input.GetMouseButtonDown(0) && isTyping)
        {
            SkipTyping();
        }
        
        if (Input.GetKeyDown(KeyCode.Escape) && DialogueManager.Instance.IsInDialogue)
        {
            DialogueManager.Instance.EndDialogue();
        }
    }
    
    private void OnDestroy()
    {
        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.OnDialogueStarted -= OnDialogueStarted;
            DialogueManager.Instance.OnDialogueNodeChanged -= OnDialogueNodeChanged;
            DialogueManager.Instance.OnDialogueEnded -= OnDialogueEnded;
        }
    }
}