using System.Collections;
using UI;
using UnityEngine;
using UnityEngine.UI;

namespace Gameplay.Characters.Player
{
    public class PlayerHealth : MonoBehaviour
    {
        public static PlayerHealth Instance;
        
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
    
        private AudioSource _audioSource;
        private bool _isDead;
        private PlayerController _playerController;
        private CharacterController _characterController;

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
        }
        
        private void Start()
        {
            currentHealth = maxHealth;
            _audioSource = GetComponent<AudioSource>();
            _playerController = GetComponent<PlayerController>();
            _characterController = GetComponent<CharacterController>();
        
            if (deathScreen != null)
            {
                deathScreen.color = new Color(0, 0, 0, 0);
                deathScreen.gameObject.SetActive(false);
            }
        
            UpdateHealthUI();
        }
    
        public void TakeDamage(float damage)
        {
            if (_isDead) return;
        
            currentHealth -= damage;
            currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        
            if (damageSound != null && _audioSource != null)
            {
                _audioSource.PlayOneShot(damageSound);
            }
        
            // Проверяем низкое здоровье
            if (currentHealth < maxHealth * 0.3f)
            {
                AIAssistant.Instance?.OnPlayerLowHealth();
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
            if (healthFill == null) yield break;
            var originalColor = healthFill.color;
            healthFill.color = Color.white;
            
            yield return new WaitForSeconds(damageEffectDuration);
            
            healthFill.color = originalColor;
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
            _isDead = true;
        
            if (_playerController != null)
                _playerController.enabled = false;
        
            if (_characterController != null)
                _characterController.enabled = false;
        
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
        
            if (_characterController != null)
                _characterController.enabled = true;
        
            if (_playerController != null)
                _playerController.enabled = true;
        
            _isDead = false;
        }
    
        private IEnumerator FadeScreen(float fromAlpha, float toAlpha, float duration)
        {
            var timer = 0f;
            while (timer < duration)
            {
                timer += Time.deltaTime;
                var alpha = Mathf.Lerp(fromAlpha, toAlpha, timer / duration);
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
}