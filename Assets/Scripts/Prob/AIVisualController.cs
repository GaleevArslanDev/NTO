using UnityEngine;
using System.Collections;

public class AIVisualController : MonoBehaviour
{
    [Header("Спрайты для ИИ")]
    [SerializeField] private Sprite normalSprite;
    [SerializeField] private Sprite talkingSprite;
    [SerializeField] private float mouthMovementSpeed = 0.1f;

    [Header("Ссылки")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private AudioSource audioSource; // для синхронизации с речью

    private Coroutine talkingRoutine;
    private bool isTalking = false;

    void Start()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }

    // Вызывайте этот метод, когда ИИ начинает говорить
    public void StartTalking()
    {
        if (isTalking) return;

        isTalking = true;
        talkingRoutine = StartCoroutine(TalkingAnimation());
    }

    // Вызывайте этот метод, когда ИИ заканчивает говорить
    public void StopTalking()
    {
        if (!isTalking) return;

        isTalking = false;

        if (talkingRoutine != null)
        {
            StopCoroutine(talkingRoutine);
        }

        // Вернуть обычный спрайт
        spriteRenderer.sprite = normalSprite;
    }

    private IEnumerator TalkingAnimation()
    {
        while (isTalking)
        {
            // Синхронизация с аудио (если есть)
            if (audioSource != null && audioSource.isPlaying)
            {
                // Меняем спрайт на "говорящий"
                spriteRenderer.sprite = talkingSprite;

                // Можно добавить анимацию "открывания рта" в такт речи
                yield return new WaitForSeconds(mouthMovementSpeed);

                // Возвращаем обычный спрайт
                spriteRenderer.sprite = normalSprite;

                yield return new WaitForSeconds(mouthMovementSpeed);
            }
            else
            {
                // Простая анимация без аудио
                spriteRenderer.sprite = talkingSprite;
                yield return new WaitForSeconds(0.15f);
                spriteRenderer.sprite = normalSprite;
                yield return new WaitForSeconds(0.15f);
            }
        }
    }

    // Для синхронизации с системой диалогов
    public void OnDialogueStart(string message)
    {
        StartTalking();
        // Можно добавить логику для разных эмоций
    }

    public void OnDialogueEnd()
    {
        StopTalking();
    }
}