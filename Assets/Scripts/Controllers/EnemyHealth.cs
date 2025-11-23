using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class EnemyHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public float maxHealth = 30f;
    public float currentHealth;
    
    [Header("UI References")]
    public GameObject healthBarCanvas;
    public Slider healthSlider;
    public Image healthFill;
    public Color fullHealthColor = Color.green;
    public Color lowHealthColor = Color.red;
    
    [Header("Damage Effects")]
    public GameObject damageTextPrefab;
    public Transform damageTextSpawnPoint;
    public float textFloatHeight = 2f;
    public float textDuration = 1f;
    
    [Header("Hit Effects")]
    public Renderer enemyRenderer;
    public Color hitColor = Color.red;
    public float hitFlashDuration = 0.2f;
    public GameObject hitParticlePrefab;
    
    private Material[] originalMaterials;
    private Color[] originalColors;
    private bool isDead = false;
    private EnemyDeath enemyDeath;
    
    // Событие для уведомления о смерти
    public System.Action<EnemyHealth> OnEnemyDied;
    public System.Action<float> OnHealthChanged;
    
    void Start()
    {
        currentHealth = maxHealth;
        enemyDeath = GetComponent<EnemyDeath>();
        
        if (enemyRenderer != null)
        {
            originalMaterials = enemyRenderer.materials;
            originalColors = new Color[originalMaterials.Length];
            for (int i = 0; i < originalMaterials.Length; i++)
            {
                originalColors[i] = originalMaterials[i].color;
            }
        }
        
        if (damageTextSpawnPoint == null)
        {
            damageTextSpawnPoint = transform;
        }
        
        UpdateHealthUI();
        
        // Скрываем полоску здоровья если полное здоровье
        if (healthBarCanvas != null)
        {
            healthBarCanvas.SetActive(false);
        }
    }
    
    public void TakeDamage(float damage, Vector3 hitPoint = default(Vector3))
    {
        if (isDead) return;
        
        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        
        // Показываем полоску здоровья при получении урона
        if (healthBarCanvas != null)
        {
            healthBarCanvas.SetActive(true);
        }
        
        // Создаем текст урона
        ShowDamageText(damage, hitPoint);
        
        // Эффект мигания красным
        StartCoroutine(HitFlashEffect());
        
        // Партиклы попадания
        if (hitParticlePrefab != null && hitPoint != default(Vector3))
        {
            GameObject hitParticle = Instantiate(hitParticlePrefab, hitPoint, Quaternion.identity);
            Destroy(hitParticle, 2f); // Уничтожаем через 2 секунды
        }
        
        // Обновляем UI
        UpdateHealthUI();
        
        // Уведомляем о изменении здоровья
        OnHealthChanged?.Invoke(currentHealth);
        
        if (currentHealth <= 0)
        {
            Die();
        }
    }
    
    private void UpdateHealthUI()
    {
        if (healthSlider != null)
        {
            float healthPercent = currentHealth / maxHealth;
            healthSlider.value = healthPercent;
            
            // Плавное изменение цвета
            if (healthFill != null)
            {
                healthFill.color = Color.Lerp(lowHealthColor, fullHealthColor, healthPercent);
            }
        }
    }
    
    private void ShowDamageText(float damage, Vector3 hitPoint)
    {
        if (damageTextPrefab == null) return;
        
        // Определяем позицию для текста
        Vector3 spawnPosition = hitPoint != default(Vector3) ? hitPoint : damageTextSpawnPoint.position;
        spawnPosition += Vector3.up * textFloatHeight;
        
        // Создаем текст урона
        GameObject damageText = Instantiate(damageTextPrefab, spawnPosition, Quaternion.identity);
        
        // Направляем текст к камере
        damageText.transform.LookAt(2 * damageText.transform.position - Camera.main.transform.position);
        
        DamageText textComponent = damageText.GetComponent<DamageText>();
        if (textComponent != null)
        {
            textComponent.Initialize(damage, textDuration);
        }
        else
        {
            // Если нет компонента, просто уничтожаем через время
            Destroy(damageText, textDuration);
        }
    }
    
    private IEnumerator HitFlashEffect()
    {
        if (enemyRenderer == null) yield break;
        
        // Меняем цвет на красный
        foreach (Material mat in enemyRenderer.materials)
        {
            mat.color = hitColor;
        }
        
        yield return new WaitForSeconds(hitFlashDuration);
        
        // Возвращаем оригинальные цвета
        for (int i = 0; i < enemyRenderer.materials.Length && i < originalColors.Length; i++)
        {
            enemyRenderer.materials[i].color = originalColors[i];
        }
    }
    
    private void Die()
    {
        if (isDead) return;
        isDead = true;
        
        // Отключаем UI
        if (healthBarCanvas != null)
        {
            healthBarCanvas.SetActive(false);
        }
        
        // Запускаем мгновенную смерть с партиклами
        if (enemyDeath != null)
        {
            enemyDeath.InstantDeathWithParticles();
        }
        else
        {
            // Резервный вариант - просто уничтожаем
            Destroy(gameObject);
        }
        
        // Уведомляем о смерти
        OnEnemyDied?.Invoke(this);
        
        // Отключаем скрипт
        enabled = false;
    }
    
    public void Heal(float healAmount)
    {
        if (isDead) return;
        
        currentHealth += healAmount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        UpdateHealthUI();
        
        OnHealthChanged?.Invoke(currentHealth);
    }
}