using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public float maxHealth = 100f;
    public float currentHealth;
    
    [Header("Respawn Settings")]
    public Transform respawnPoint;
    public float fadeDuration = 1.5f;
    
    [Header("UI References")]
    public Slider healthSlider;
    public Image healthFill;
    public Image deathScreen;
    public Color fullHealthColor = Color.green;
    public Color lowHealthColor = Color.red;
    
    [Header("Damage Effects")]
    public AudioClip damageSound;
    public float damageEffectDuration = 0.3f;
    
    private AudioSource audioSource;
    private bool isDead = false;
    private PlayerController playerController;
    private CharacterController characterController;

    void Start()
    {
        currentHealth = maxHealth;
        audioSource = GetComponent<AudioSource>();
        playerController = GetComponent<PlayerController>();
        characterController = GetComponent<CharacterController>();
        
        if (deathScreen != null)
        {
            deathScreen.color = new Color(0, 0, 0, 0);
            deathScreen.gameObject.SetActive(false);
        }
        
        UpdateHealthUI();
    }
    
    public void TakeDamage(float damage)
    {
        if (isDead) return;
        
        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        
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
    
    private IEnumerator DamageEffect()
    {
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
        
        if (playerController != null)
            playerController.enabled = false;
        
        if (characterController != null)
            characterController.enabled = false;
        
        StartCoroutine(DeathSequence());
    }
    
    private IEnumerator DeathSequence()
    {
        if (deathScreen != null)
        {
            deathScreen.gameObject.SetActive(true);
            yield return StartCoroutine(FadeScreen(0f, 1f, fadeDuration));
        }
        
        if (respawnPoint != null)
        {
            transform.position = respawnPoint.position;
            transform.rotation = respawnPoint.rotation;
        }
        
        currentHealth = maxHealth;
        UpdateHealthUI();
        
        yield return new WaitForSeconds(0.5f);
        
        if (deathScreen != null)
        {
            yield return StartCoroutine(FadeScreen(1f, 0f, fadeDuration));
            deathScreen.gameObject.SetActive(false);
        }
        
        if (characterController != null)
            characterController.enabled = true;
        
        if (playerController != null)
            playerController.enabled = true;
        
        isDead = false;
    }
    
    private IEnumerator FadeScreen(float fromAlpha, float toAlpha, float duration)
    {
        float timer = 0f;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            float alpha = Mathf.Lerp(fromAlpha, toAlpha, timer / duration);
            deathScreen.color = new Color(0, 0, 0, alpha);
            yield return null;
        }
    }
    
    public void Heal(float healAmount)
    {
        currentHealth += healAmount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        UpdateHealthUI();
    }

    public void SetRespawnPoint(Transform newRespawnPoint)
    {
        respawnPoint = newRespawnPoint;
    }
}