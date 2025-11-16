using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class Enemy : MonoBehaviour
{
    [Header("Enemy Settings")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private GameObject deathEffect;
    [SerializeField] private GameObject damageTextPrefab;
    [SerializeField] private Slider healthBar;
    [SerializeField] private Canvas healthBarCanvas;
    
    private float currentHealth;
    private Vector3 healthBarOffset = new Vector3(0, 2f, 0);
    
    void Start()
    {
        currentHealth = maxHealth;
        
        // Настройка health bar
        if (healthBar != null)
        {
            healthBar.maxValue = maxHealth;
            healthBar.value = currentHealth;
        }
        
        if (healthBarCanvas != null)
        {
            healthBarCanvas.worldCamera = Camera.main;
        }
    }
    
    void Update()
    {
        // Обновляем позицию health bar над врагом
        if (healthBarCanvas != null)
        {
            healthBarCanvas.transform.position = transform.position + healthBarOffset;
            healthBarCanvas.transform.rotation = Camera.main.transform.rotation;
        }
    }
    
    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
    
        // Обновляем health bar
        if (healthBar != null)
        {
            healthBar.value = currentHealth;
        }
    
        // Показываем текст урона через менеджер
        if (DamageTextManager.Instance != null)
        {
            DamageTextManager.Instance.ShowDamageText(transform.position + Vector3.up * 2.5f, damage);
        }
    
        // Визуальная обратная связь о получении урона
        StartCoroutine(DamageFlash());
    
        if (currentHealth <= 0)
        {
            Die();
        }
    }
    
    private void ShowDamageText(float damage)
    {
        if (damageTextPrefab != null)
        {
            GameObject damageText = Instantiate(damageTextPrefab, transform.position + healthBarOffset + Vector3.up * 0.5f, Quaternion.identity);
            TextMeshPro textMesh = damageText.GetComponent<TextMeshPro>();
            
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
                    
                // Запускаем анимацию
                StartCoroutine(AnimateDamageText(damageText));
            }
        }
    }
    
    private System.Collections.IEnumerator AnimateDamageText(GameObject textObj)
    {
        float duration = 1f;
        float elapsed = 0f;
        Vector3 startPos = textObj.transform.position;
        Vector3 endPos = startPos + Vector3.up * 2f;
        
        TextMeshPro textMesh = textObj.GetComponent<TextMeshPro>();
        Color startColor = textMesh.color;
        Color endColor = new Color(startColor.r, startColor.g, startColor.b, 0f);
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;
            
            // Поднимаем текст вверх
            textObj.transform.position = Vector3.Lerp(startPos, endPos, progress);
            
            // Плавно исчезаем
            textMesh.color = Color.Lerp(startColor, endColor, progress);
            
            // Немного увеличиваем размер
            float scale = Mathf.Lerp(1f, 1.5f, progress);
            textObj.transform.localScale = Vector3.one * scale;
            
            yield return null;
        }
        
        Destroy(textObj);
    }
    
    private System.Collections.IEnumerator DamageFlash()
    {
        var renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            Color originalColor = renderer.material.color;
            renderer.material.color = Color.red;
            yield return new WaitForSeconds(0.1f);
            renderer.material.color = originalColor;
        }
    }
    
    private void Die()
    {
        // Эффект смерти
        if (deathEffect != null)
        {
            Instantiate(deathEffect, transform.position, transform.rotation);
        }
        
        Debug.Log("Враг уничтожен!");
        Destroy(gameObject);
    }
}