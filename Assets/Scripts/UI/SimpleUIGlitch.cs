using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace UI
{
    public class SimpleUIGlitch : MonoBehaviour
    {
        [SerializeField] private RectTransform[] uiElements;
        [SerializeField] private float glitchDuration = 1.5f;
        [SerializeField] private float maxOffset = 20f;
    
        private Vector2[] originalPositions;
    
        private void OnEnable()
        {
            PlayGlitch();
        }

        private void OnDisable()
        {
            PlayGlitch();
        }

        private void PlayGlitch()
        {
            originalPositions = new Vector2[uiElements.Length];
            for (int i = 0; i < uiElements.Length; i++)
            {
                originalPositions[i] = uiElements[i].anchoredPosition;
            }
        
            StartCoroutine(GlitchAnimation());
        }
    
        IEnumerator GlitchAnimation()
        {
            float timer = 0f;
        
            while (timer < glitchDuration)
            {
                float progress = timer / glitchDuration;
                float glitchStrength = Mathf.Lerp(1, 0, progress);
            
                for (int i = 0; i < uiElements.Length; i++)
                {
                    // Случайное смещение
                    Vector2 randomOffset = new Vector2(
                        Random.Range(-maxOffset, maxOffset) * glitchStrength,
                        Random.Range(-maxOffset, maxOffset) * glitchStrength
                    );
                
                    uiElements[i].anchoredPosition = originalPositions[i] + randomOffset;
                
                    // Случайное изменение масштаба
                    float scale = 1 + Random.Range(-0.1f, 0.1f) * glitchStrength;
                    uiElements[i].localScale = Vector3.one * scale;
                }
            
                timer += Time.deltaTime;
                yield return new WaitForSeconds(0.05f); // Частота глитча
            }
        
            // Возвращаем на исходные позиции
            for (int i = 0; i < uiElements.Length; i++)
            {
                uiElements[i].anchoredPosition = originalPositions[i];
                uiElements[i].localScale = Vector3.one;
            }
        }
    }
}