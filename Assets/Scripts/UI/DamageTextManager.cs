using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class DamageTextManager : MonoBehaviour
{
    public static DamageTextManager Instance;
    
    [SerializeField] private GameObject damageTextPrefab;
    [SerializeField] private int poolSize = 20;
    
    private Queue<GameObject> damageTextPool = new Queue<GameObject>();
    
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
        
        InitializePool();
    }
    
    private void InitializePool()
    {
        for (int i = 0; i < poolSize; i++)
        {
            GameObject textObj = Instantiate(damageTextPrefab, transform);
            textObj.SetActive(false);
            damageTextPool.Enqueue(textObj);
        }
    }
    
    public void ShowDamageText(Vector3 position, float damage)
    {
        if (damageTextPool.Count == 0) return;
        
        GameObject textObj = damageTextPool.Dequeue();
        textObj.SetActive(true);
        
        // Настройка текста
        TextMeshPro textMesh = textObj.GetComponent<TextMeshPro>();
        if (textMesh != null)
        {
            textMesh.text = $"-{damage}";
            
            // Цвет текста в зависимости от урона
            if (damage >= 50)
                textMesh.color = Color.red;
            else if (damage >= 20)
                textMesh.color = Color.yellow;
            else
                textMesh.color = Color.white;
        }
        
        // Запуск анимации
        StartCoroutine(AnimateDamageText(textObj, position));
    }
    
    private System.Collections.IEnumerator AnimateDamageText(GameObject textObj, Vector3 startPosition)
    {
        float duration = 1f;
        float elapsed = 0f;
        Vector3 endPos = startPosition + Vector3.up * 2f;
        
        TextMeshPro textMesh = textObj.GetComponent<TextMeshPro>();
        Color startColor = textMesh.color;
        Color endColor = new Color(startColor.r, startColor.g, startColor.b, 0f);
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;
            
            // Поднимаем текст вверх
            textObj.transform.position = Vector3.Lerp(startPosition, endPos, progress);
            
            // Плавно исчезаем
            textMesh.color = Color.Lerp(startColor, endColor, progress);
            
            // Немного увеличиваем размер
            float scale = Mathf.Lerp(1f, 1.5f, progress);
            textObj.transform.localScale = Vector3.one * scale;
            
            yield return null;
        }
        
        // Возвращаем в пул
        textObj.SetActive(false);
        damageTextPool.Enqueue(textObj);
    }
}