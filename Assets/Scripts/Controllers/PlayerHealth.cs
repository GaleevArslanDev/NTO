using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public float maxHealth = 100f;
    public float currentHealth;
    
    [Header("UI References")]
    public Slider healthSlider;
    public Image healthFill;
    public Color fullHealthColor = Color.green;
    public Color lowHealthColor = Color.red;
    
    [Header("Damage Effects")]
    public AudioClip damageSound;
    public float damageEffectDuration = 0.3f;
    
    private AudioSource audioSource;
    private bool isDead = false;
    
    void Start()
    {
        currentHealth = maxHealth;
        audioSource = GetComponent<AudioSource>();
        
        UpdateHealthUI();
    }
    
    public void TakeDamage(float damage)
    {
        if (isDead) return;
        
        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        
        // Эффекты получения урона
        if (damageSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(damageSound);
        }
        
        StartCoroutine(DamageEffect());
        
        UpdateHealthUI();
        
        if (currentHealth <= 0)
        {
            Die();
        }
    }
    
    private System.Collections.IEnumerator DamageEffect()
    {
        // Можно добавить мигание экрана или другие эффекты
        // Например, временное изменение цвета UI здоровья
        
        if (healthFill != null)
        {
            Color originalColor = healthFill.color;
            healthFill.color = Color.white;
            
            yield return new WaitForSeconds(damageEffectDuration);
            
            healthFill.color = originalColor;
        }
    }
    
    private void UpdateHealthUI()
    {
        if (healthSlider != null)
        {
            healthSlider.value = currentHealth / maxHealth;
        }
        
        if (healthFill != null)
        {
            healthFill.color = Color.Lerp(lowHealthColor, fullHealthColor, currentHealth / maxHealth);
        }
    }
    
    private void Die()
    {
        isDead = true;
        
        // Отключаем управление
        PlayerController playerController = GetComponent<PlayerController>();
        if (playerController != null)
        {
            playerController.enabled = false;
        }
        
        // Показываем экран смерти или перезапускаем уровень
        Debug.Log("Игрок умер!");
        
        // Здесь можно добавить перезагрузку уровня или меню смерти
    }
    
    public void Heal(float healAmount)
    {
        currentHealth += healAmount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        UpdateHealthUI();
    }
}