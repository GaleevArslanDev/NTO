using TMPro;
using UnityEngine;
using System.Collections;

namespace UI
{
    public class RelationshipNotificationUI : MonoBehaviour
    {
        public static RelationshipNotificationUI Instance { get; private set; }
        
        [Header("UI Elements")]
        [SerializeField] private GameObject notificationPanel;
        [SerializeField] private TextMeshProUGUI notificationText;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private RectTransform panelRectTransform;
        
        [Header("Animation Settings")]
        [SerializeField] private float fadeInDuration = 0.3f;
        [SerializeField] private float displayDuration = 2f;
        [SerializeField] private float fadeOutDuration = 0.5f;
        [SerializeField] private float verticalMovement = 50f; // На сколько пикселей поднимается
        [SerializeField] private AnimationCurve movementCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        
        private Coroutine _currentNotificationRoutine;
        private bool _isShowing;
        private Vector2 _originalPosition; // Начальная позиция панели
        
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
            
            // Инициализация
            if (canvasGroup == null)
                canvasGroup = notificationPanel.GetComponent<CanvasGroup>();
            
            if (panelRectTransform == null)
                panelRectTransform = notificationPanel.GetComponent<RectTransform>();
            
            // Сохраняем оригинальную позицию
            _originalPosition = panelRectTransform.anchoredPosition;
            
            HideNotification();
        }
        
        public void ShowRelationshipChange(string npcName, int change)
        {
            // Если уже показываем уведомление, прерываем его
            if (_currentNotificationRoutine != null)
            {
                StopCoroutine(_currentNotificationRoutine);
            }
            
            // Формируем текст сообщения
            string message;
            if (change > 0)
            {
                message = LocalizationManager.LocalizationManager.Instance.GetString("relationship_change_positive", change.ToString(), npcName);
            }
            else if (change < 0)
            {
                message = LocalizationManager.LocalizationManager.Instance.GetString(
                    "relationship_notification_negative",
                    change.ToString(),
                    npcName
                );
            }
            else
            {
                message = LocalizationManager.LocalizationManager.Instance.GetString(
                    "relationship_notification_neutral",
                    npcName
                );
            }
            
            // Запускаем корутину показа уведомления
            _currentNotificationRoutine = StartCoroutine(ShowNotificationRoutine(message));
        }
        
        private IEnumerator ShowNotificationRoutine(string message)
        {
            _isShowing = true;
            
            // Устанавливаем текст
            notificationText.text = message;
            notificationPanel.SetActive(true);
            
            // Устанавливаем начальную позицию (ниже на verticalMovement пикселей)
            Vector2 startPosition = _originalPosition - new Vector2(0, verticalMovement);
            panelRectTransform.anchoredPosition = startPosition;
            
            // Плавное появление и движение
            canvasGroup.alpha = 0f;
            float timer = 0f;
            
            while (timer < fadeInDuration)
            {
                timer += Time.deltaTime;
                float t = timer / fadeInDuration;
                float curveValue = movementCurve.Evaluate(t);
                
                // Интерполяция позиции и прозрачности
                panelRectTransform.anchoredPosition = Vector2.Lerp(
                    startPosition, 
                    _originalPosition, 
                    curveValue
                );
                canvasGroup.alpha = Mathf.Lerp(0f, 1f, t);
                yield return null;
            }
            
            panelRectTransform.anchoredPosition = _originalPosition;
            canvasGroup.alpha = 1f;
            
            // Ждем указанное время (неподвижно)
            yield return new WaitForSeconds(displayDuration);
            
            // Плавное исчезновение и дальнейшее движение вверх
            timer = 0f;
            Vector2 endPosition = _originalPosition + new Vector2(0, verticalMovement);
            
            while (timer < fadeOutDuration)
            {
                timer += Time.deltaTime;
                float t = timer / fadeOutDuration;
                float curveValue = movementCurve.Evaluate(t);
                
                // Интерполяция позиции и прозрачности
                panelRectTransform.anchoredPosition = Vector2.Lerp(
                    _originalPosition, 
                    endPosition, 
                    curveValue
                );
                canvasGroup.alpha = Mathf.Lerp(1f, 0f, t);
                yield return null;
            }
            
            panelRectTransform.anchoredPosition = _originalPosition; // Возвращаем на исходную
            canvasGroup.alpha = 0f;
            notificationPanel.SetActive(false);
            
            _isShowing = false;
            _currentNotificationRoutine = null;
        }
        
        public void ShowTemporaryMessage(string message, float duration = 2f)
        {
            if (_currentNotificationRoutine != null)
            {
                StopCoroutine(_currentNotificationRoutine);
            }
            
            _currentNotificationRoutine = StartCoroutine(ShowTemporaryMessageRoutine(message, duration));
        }
        
        private IEnumerator ShowTemporaryMessageRoutine(string message, float duration)
        {
            _isShowing = true;
            
            // Устанавливаем текст и начальную позицию
            notificationText.text = message;
            notificationPanel.SetActive(true);
            
            Vector2 startPosition = _originalPosition - new Vector2(0, verticalMovement);
            panelRectTransform.anchoredPosition = startPosition;
            canvasGroup.alpha = 0f;
            
            // Быстрое появление с движением
            float timer = 0f;
            float quickFadeIn = 0.2f;
            
            while (timer < quickFadeIn)
            {
                timer += Time.deltaTime;
                float t = timer / quickFadeIn;
                
                panelRectTransform.anchoredPosition = Vector2.Lerp(
                    startPosition, 
                    _originalPosition, 
                    t
                );
                canvasGroup.alpha = Mathf.Lerp(0f, 1f, t);
                yield return null;
            }
            
            panelRectTransform.anchoredPosition = _originalPosition;
            canvasGroup.alpha = 1f;
            
            // Ждем
            yield return new WaitForSeconds(duration);
            
            // Быстрое исчезновение
            timer = 0f;
            Vector2 endPosition = _originalPosition + new Vector2(0, verticalMovement);
            
            while (timer < fadeOutDuration)
            {
                timer += Time.deltaTime;
                float t = timer / fadeOutDuration;
                
                panelRectTransform.anchoredPosition = Vector2.Lerp(
                    _originalPosition, 
                    endPosition, 
                    t
                );
                canvasGroup.alpha = Mathf.Lerp(1f, 0f, t);
                yield return null;
            }
            
            panelRectTransform.anchoredPosition = _originalPosition;
            canvasGroup.alpha = 0f;
            notificationPanel.SetActive(false);
            
            _isShowing = false;
            _currentNotificationRoutine = null;
        }
        
        private void HideNotification()
        {
            if (canvasGroup != null)
                canvasGroup.alpha = 0f;
            
            if (notificationPanel != null)
                notificationPanel.SetActive(false);
            
            if (panelRectTransform != null)
                panelRectTransform.anchoredPosition = _originalPosition;
            
            _isShowing = false;
        }
        
        public bool IsShowing => _isShowing;
    }
}