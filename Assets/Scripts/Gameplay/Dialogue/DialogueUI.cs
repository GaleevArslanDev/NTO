using System.Collections;
using Core;
using Gameplay.Characters.NPC;
using TMPro;
using UI;
using UnityEngine;
using UnityEngine.UI;

namespace Gameplay.Dialogue
{
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
    
        private CanvasGroup _canvasGroup;
        private bool _isTyping;
        private Coroutine _typingCoroutine;
    
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
        
            _canvasGroup = dialoguePanel.GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
                _canvasGroup = dialoguePanel.AddComponent<CanvasGroup>();
            
            dialoguePanel.SetActive(false);

            if (DialogueManager.Instance == null) return;
            DialogueManager.Instance.OnDialogueStarted += OnDialogueStarted;
            DialogueManager.Instance.OnDialogueNodeChanged += OnDialogueNodeChanged;
            DialogueManager.Instance.OnDialogueEnded += OnDialogueEnded;
        }
    
        private void OnDialogueStarted(DialogueNode node, string npcName)
        {
            ShowDialogue();
            UpdateDialogueUI(node, npcName);
        }
    
        private void OnDialogueNodeChanged(DialogueNode node)
        {
            UpdateDialogueUI(node, DialogueManager.Instance.CurrentNpcName);
        }
    
        private void OnDialogueEnded()
        {
            // Выключаем состояние ожидания при завершении диалога
            SetNpcWaitingForResponse(false);
    
            HideDialogue();
        }
    
        public void ShowDialogue()
        {
            dialoguePanel.SetActive(true);
            StartCoroutine(FadeIn());
    
            // Используем UIManager
            if (UIManager.Instance != null)
                if (!DialogueManager.Instance.IsInDialogue)
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
            if (_canvasGroup == null) yield break;
            _canvasGroup.alpha = 0;
            while (_canvasGroup.alpha < 1)
            {
                _canvasGroup.alpha += Time.deltaTime / fadeDuration;
                yield return null;
            }
        }
    
        private IEnumerator FadeOut()
        {
            if (_canvasGroup != null)
            {
                while (_canvasGroup.alpha > 0)
                {
                    _canvasGroup.alpha -= Time.deltaTime / fadeDuration;
                    yield return null;
                }
            }
            dialoguePanel.SetActive(false);
        }
    
        private void UpdateDialogueUI(DialogueNode node, string npcName)
        {
            if (node == null) return;
    
            // Локализуем имя NPC
            string localizedNpcName = LocalizationManager.LocalizationManager.Instance.GetString($"npc_{npcName.ToLower()}");
            npcNameText.text = string.IsNullOrEmpty(localizedNpcName) ? npcName : localizedNpcName;
    
            SetEmotionIcon(node.emotion);

            if (_typingCoroutine != null)
                StopCoroutine(_typingCoroutine);
        
            // Используем локализованный текст
            string textToDisplay = node.GetLocalizedText();
            _typingCoroutine = StartCoroutine(TypeText(textToDisplay, node.typingSpeed));

            if (node.voiceLine != null && voiceSource != null)
            {
                voiceSource.PlayOneShot(node.voiceLine);
            }
    
            UpdateOptions();
        }
    
        private IEnumerator TypeText(string text, float speed)
        {
            _isTyping = true;
            dialogueText.text = "";
    
            var actualSpeed = speed > 0 ? speed : typeWriterSpeed;
    
            foreach (var c in text)
            {
                dialogueText.text += c;
                yield return new WaitForSeconds(actualSpeed);
            }
    
            _isTyping = false;
    
            // Включаем состояние ожидания ответа после завершения печати текста NPC
            SetNpcWaitingForResponse(true);
        }
        
        private void SetNpcWaitingForResponse(bool waiting)
        {
            if (DialogueManager.Instance == null || 
                DialogueManager.Instance.CurrentNpc == null) return;
    
            var npcBehaviour = DialogueManager.Instance.CurrentNpc.GetComponent<NpcBehaviour>();
            npcBehaviour?.SetWaitingForResponse(waiting);
        }

        private void SkipTyping()
        {
            if (!_isTyping || _typingCoroutine == null) return;
            StopCoroutine(_typingCoroutine);
            if (DialogueManager.Instance.CurrentNode != null)
            {
                dialogueText.text = DialogueManager.Instance.CurrentNode.GetLocalizedText();
            }
            _isTyping = false;
        }
        
        private void SetEmotionIcon(Emotion emotion)
        {
            if (emotionIcon == null) return;

            var icon = emotion switch
            {
                Emotion.Happy => happyIcon,
                Emotion.Sad => sadIcon,
                Emotion.Angry => angryIcon,
                Emotion.Surprised => surprisedIcon,
                Emotion.Confused => confusedIcon,
                Emotion.Thoughtful => thoughtfulIcon,
                _ => neutralIcon
            };

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
                for (var i = 0; i < options.Length; i++)
                {
                    CreateOptionButton(options[i], i);
                }
            }
        }
    
        private void CreateOptionButton(DialogueOption option, int index)
        {
            if (optionButtonPrefab == null) return;
    
            var buttonObj = Instantiate(optionButtonPrefab, optionsContainer);
            var button = buttonObj.GetComponent<Button>();
            var buttonText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
    
            if (buttonText != null)
                buttonText.text = option.GetLocalizedText(); // Используем локализованный текст
    
            SetupOptionColor(button, option);
    
            button.onClick.AddListener(() => OnOptionSelected(option));
    
            SetupButtonNavigation(button, index);
        }
    
        private void CreateExitOption()
        {
            if (optionButtonPrefab == null) return;
        
            var buttonObj = Instantiate(optionButtonPrefab, optionsContainer);
            var button = buttonObj.GetComponent<Button>();
            var buttonText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
        
            if (buttonText != null)
                buttonText.text = LocalizationManager.LocalizationManager.Instance.GetString("say-goodbye");
            button.onClick.AddListener(() => DialogueManager.Instance.EndDialogue());
        }
    
        private void SetupOptionColor(Button button, DialogueOption option)
        {
            if (button == null) return;
        
            var colors = button.colors;

            colors.normalColor = option.relationshipChange switch
            {
                > 0 => new Color(0.2f, 0.8f, 0.2f, 0.7f),
                < 0 => new Color(0.8f, 0.2f, 0.2f, 0.7f),
                _ => new Color(0.3f, 0.3f, 0.8f, 0.7f)
            };

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
            if (_isTyping) return;
    
            // Выключаем состояние ожидания перед выбором опции
            SetNpcWaitingForResponse(false);
    
            DialogueManager.Instance.SelectOption(option);
        }
    
        private void Update()
        {
            if (Input.GetMouseButtonDown(0) && _isTyping)
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
            if (DialogueManager.Instance == null) return;
            DialogueManager.Instance.OnDialogueStarted -= OnDialogueStarted;
            DialogueManager.Instance.OnDialogueNodeChanged -= OnDialogueNodeChanged;
            DialogueManager.Instance.OnDialogueEnded -= OnDialogueEnded;
        }
    }
}