using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace UI
{
    [RequireComponent(typeof(RectTransform))]
    public class SimpleUIGlitch : MonoBehaviour
    {
        [SerializeField] private float glitchDuration = 1.5f;
        [SerializeField] private float maxOffset = 20f;
    
        private Vector2 originalPosition;
        
        private RectTransform _uiElement;

        private void Awake()
        {
            _uiElement = GetComponent<RectTransform>();
        }
    
        private void OnEnable()
        {
            PlayGlitch();
        }

        private void PlayGlitch()
        {
            originalPosition = _uiElement.anchoredPosition;
            Debug.Log("Glitch started");
        
            StartCoroutine(GlitchAnimation());
        }
    
        IEnumerator GlitchAnimation()
        {
            float timer = 0f;
        
            while (timer < glitchDuration)
            {
                float progress = timer / glitchDuration;
                float glitchStrength = Mathf.Lerp(1, 0, progress);
            
                // Случайное смещение
                Vector2 randomOffset = new Vector2(
                    Random.Range(-maxOffset, maxOffset) * glitchStrength,
                    Random.Range(-maxOffset, maxOffset) * glitchStrength
                    );
                
                _uiElement.anchoredPosition = originalPosition + randomOffset;
                
                // Случайное изменение масштаба
                float scale = 1 + Random.Range(-0.1f, 0.1f) * glitchStrength;
                _uiElement.localScale = Vector3.one * scale;
            
                timer += Time.deltaTime;
                yield return new WaitForSeconds(0.05f); // Частота глитча
            }
        
            _uiElement.anchoredPosition = originalPosition;
            _uiElement.localScale = Vector3.one;
        }
    }
}