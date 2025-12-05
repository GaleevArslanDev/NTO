using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace Gameplay.Systems
{
    public class SimpleTeleport : MonoBehaviour
    {
        [Header("Настройки")]
        public KeyCode teleportKey = KeyCode.R;
        public Transform teleportTarget; // Куда телепортируемся
        public float chargeTime = 2f; // Время зарядки
        
        [Header("Визуальные эффекты")]
        public Image glitchOverlay; // UI Image для эффектов
        public Image chargeBar; // Полоска зарядки (опционально)
        
        private bool isCharging = false;
        private float currentCharge = 0f;
        private bool isTeleporting = false;
        private Vector3 originalCameraPosition;
        private Camera mainCamera;

        void Start()
        {
            mainCamera = Camera.main;
            
            // Скрываем UI элементы в начале
            if (glitchOverlay != null)
            {
                glitchOverlay.color = new Color(1, 1, 1, 0);
                glitchOverlay.gameObject.SetActive(false);
            }
            
            if (chargeBar != null)
            {
                chargeBar.fillAmount = 0;
                chargeBar.gameObject.SetActive(false);
            }
        }

        void Update()
        {
            if (isTeleporting) return;
            
            // Начало зарядки
            if (Input.GetKeyDown(teleportKey))
            {
                StartCharging();
            }
            
            // Процесс зарядки
            if (Input.GetKey(teleportKey) && isCharging)
            {
                currentCharge += Time.deltaTime;
                
                // Обновляем визуальные эффекты
                UpdateGlitchEffect();
                
                // Когда зарядка завершена
                if (currentCharge >= chargeTime)
                {
                    ExecuteTeleport();
                }
            }
            
            // Отмена зарядки
            if (Input.GetKeyUp(teleportKey) && isCharging)
            {
                CancelCharging();
            }
        }

        void StartCharging()
        {
            isCharging = true;
            currentCharge = 0f;
            
            // Активируем UI элементы
            if (glitchOverlay != null)
            {
                glitchOverlay.gameObject.SetActive(true);
            }
            
            if (chargeBar != null)
            {
                chargeBar.gameObject.SetActive(true);
            }
        }

        void UpdateGlitchEffect()
        {
            float progress = currentCharge / chargeTime;
            
            // Обновляем полоску зарядки
            if (chargeBar != null)
            {
                chargeBar.fillAmount = progress;
            }
            
            // Создаем эффект глитча
            if (glitchOverlay != null)
            {
                // Меняем прозрачность
                float alpha = Mathf.Lerp(0, 0.3f, progress);
                
                // Случайное смещение для эффекта глитча
                float randomOffset = Random.Range(-10f, 10f) * progress;
                
                // Меняем цвет (красный/синий каналы для эффекта)
                Color glitchColor = new Color(
                    Mathf.Clamp01(0.5f + Random.Range(-0.3f, 0.3f) * progress),
                    Mathf.Clamp01(0.5f + Random.Range(-0.2f, 0.2f) * progress),
                    Mathf.Clamp01(0.5f + Random.Range(-0.3f, 0.3f) * progress),
                    alpha
                );
                
                glitchOverlay.color = glitchColor;
                
                // Случайное масштабирование
                float scale = 1f + Random.Range(-0.02f, 0.02f) * progress;
                glitchOverlay.transform.localScale = new Vector3(scale, scale, 1);
                
                // Случайное вращение
                float rotation = Random.Range(-2f, 2f) * progress;
                glitchOverlay.transform.rotation = Quaternion.Euler(0, 0, rotation);
            }
            
            // Легкая тряска камеры
            if (mainCamera != null)
            {
                float shakeAmount = progress * 0.1f;
                mainCamera.transform.localPosition = new Vector3(
                    Random.Range(-shakeAmount, shakeAmount),
                    Random.Range(-shakeAmount, shakeAmount),
                    mainCamera.transform.localPosition.z
                );
            }
        }

        void CancelCharging()
        {
            isCharging = false;
            
            // Плавно скрываем эффекты
            StartCoroutine(FadeOutEffects());
            
            // Возвращаем камеру на место
            if (mainCamera != null)
            {
                mainCamera.transform.localPosition = Vector3.zero;
            }
        }

        void ExecuteTeleport()
        {
            if (teleportTarget == null) return;
            
            isTeleporting = true;
            isCharging = false;
            
            // Запускаем последовательность телепортации
            StartCoroutine(TeleportSequence());
        }

        IEnumerator TeleportSequence()
        {
            // 1. Максимальный эффект глитча
            if (glitchOverlay != null)
            {
                glitchOverlay.color = Color.white;
                glitchOverlay.transform.localScale = Vector3.one * 1.2f;
            }
            
            // 2. Сильная тряска
            if (mainCamera != null)
            {
                for (int i = 0; i < 5; i++)
                {
                    mainCamera.transform.localPosition = new Vector3(
                        Random.Range(-0.2f, 0.2f),
                        Random.Range(-0.2f, 0.2f),
                        mainCamera.transform.localPosition.z
                    );
                    yield return new WaitForSeconds(0.05f);
                }
            }
            
            // 3. Телепортация
            transform.position = teleportTarget.position;
            
            // 4. Пост-эффекты
            if (glitchOverlay != null)
            {
                glitchOverlay.color = new Color(1, 1, 1, 0.5f);
                yield return new WaitForSeconds(0.1f);
            }
            
            // 5. Возврат камеры
            if (mainCamera != null)
            {
                mainCamera.transform.localPosition = Vector3.zero;
            }
            
            // 6. Плавное исчезновение эффектов
            yield return StartCoroutine(FadeOutEffects());
            
            isTeleporting = false;
        }

        IEnumerator FadeOutEffects()
        {
            if (glitchOverlay == null) yield break;
            
            float fadeTime = 0.3f;
            float timer = 0f;
            Color startColor = glitchOverlay.color;
            
            while (timer < fadeTime)
            {
                timer += Time.deltaTime;
                float progress = timer / fadeTime;
                
                glitchOverlay.color = new Color(
                    startColor.r,
                    startColor.g,
                    startColor.b,
                    Mathf.Lerp(startColor.a, 0, progress)
                );
                
                // Возвращаем нормальный размер и вращение
                glitchOverlay.transform.localScale = Vector3.Lerp(
                    glitchOverlay.transform.localScale,
                    Vector3.one,
                    progress
                );
                
                glitchOverlay.transform.rotation = Quaternion.Lerp(
                    glitchOverlay.transform.rotation,
                    Quaternion.identity,
                    progress
                );
                
                yield return null;
            }
            
            // Скрываем UI элементы
            glitchOverlay.gameObject.SetActive(false);
            if (chargeBar != null)
            {
                chargeBar.gameObject.SetActive(false);
            }
        }

        // Возвращаем камеру на место, если игрок вышел из режима зарядки
        void LateUpdate()
        {
            if (!isCharging && !isTeleporting && mainCamera != null)
            {
                mainCamera.transform.localPosition = Vector3.Lerp(
                    mainCamera.transform.localPosition,
                    Vector3.zero,
                    Time.deltaTime * 10f
                );
            }
        }
    }
}