using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace UI
{
    public class DamageTextManager : MonoBehaviour
    {
        public static DamageTextManager Instance;
    
        [SerializeField] private GameObject damageTextPrefab;
        [SerializeField] private int poolSize = 20;
    
        private Queue<GameObject> _damageTextPool = new();

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
        
            InitializePool();
        }
    
        private void InitializePool()
        {
            for (var i = 0; i < poolSize; i++)
            {
                var textObj = Instantiate(damageTextPrefab, transform);
                textObj.SetActive(false);
                _damageTextPool.Enqueue(textObj);
            }
        }
    
        public void ShowDamageText(Vector3 position, float damage)
        {
            if (_damageTextPool.Count == 0) return;
        
            var textObj = _damageTextPool.Dequeue();
            textObj.SetActive(true);
        
            // Настройка текста
            var textMesh = textObj.GetComponent<TextMeshPro>();
            if (textMesh != null)
            {
                textMesh.text = $"-{damage}";

                textMesh.color = damage switch
                {
                    // Цвет текста в зависимости от урона
                    >= 50 => Color.red,
                    >= 20 => Color.yellow,
                    _ => Color.white
                };
            }
        
            // Запуск анимации
            StartCoroutine(AnimateDamageText(textObj, position));
        }
    
        private System.Collections.IEnumerator AnimateDamageText(GameObject textObj, Vector3 startPosition)
        {
            const float duration = 1f;
            var elapsed = 0f;
            var endPos = startPosition + Vector3.up * 2f;
        
            var textMesh = textObj.GetComponent<TextMeshPro>();
            var startColor = textMesh.color;
            var endColor = new Color(startColor.r, startColor.g, startColor.b, 0f);
        
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var progress = elapsed / duration;
            
                // Поднимаем текст вверх
                textObj.transform.position = Vector3.Lerp(startPosition, endPos, progress);
            
                // Плавно исчезаем
                textMesh.color = Color.Lerp(startColor, endColor, progress);
            
                // Немного увеличиваем размер
                var scale = Mathf.Lerp(1f, 1.5f, progress);
                textObj.transform.localScale = Vector3.one * scale;
            
                yield return null;
            }
        
            // Возвращаем в пул
            textObj.SetActive(false);
            _damageTextPool.Enqueue(textObj);
        }
    }
}