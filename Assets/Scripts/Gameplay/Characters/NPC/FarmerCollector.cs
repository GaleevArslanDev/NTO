using System.Collections.Generic;
using Core;
using Gameplay.Buildings;
using Gameplay.Items;
using Gameplay.Systems;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UI;

namespace Gameplay.Characters.NPC
{
    public class FarmerCollector : MonoBehaviour
    {
        [Header("Настройки сбора")]
        [SerializeField] private KeyCode collectKey = KeyCode.F;
        [SerializeField] private float collectionRange = 5f;
        
        [Header("UI Элементы")]
        [SerializeField] private GameObject collectionPanel;
        [SerializeField] private TextMeshProUGUI resourcesText;
        [SerializeField] private Button collectButton;
        [SerializeField] private TextMeshProUGUI collectButtonText;
        [SerializeField] private Image collectButtonIcon;
        [SerializeField] private Sprite collectSprite;
        [SerializeField] private Sprite emptySprite;
        
        [Header("Анимации")]
        [SerializeField] private Animator farmerAnimator;
        [SerializeField] private string collectAnimation = "Collect";
        [SerializeField] private AudioClip collectSound;
        
        private Transform _player;
        private bool _isCollectionAvailable = false;
        private Dictionary<ItemType, int> _availableResources = new Dictionary<ItemType, int>();
        
        private void Start()
        {
            _player = GameObject.FindGameObjectWithTag("Player")?.transform;
            
            if (collectionPanel != null)
            {
                collectionPanel.SetActive(false);
            }
            
            if (collectButton != null)
            {
                collectButton.onClick.AddListener(CollectResources);
            }
        }
        
        private void Update()
        {
            if (_player == null) return;
            
            float distance = Vector3.Distance(transform.position, _player.position);
            bool wasAvailable = _isCollectionAvailable;
            _isCollectionAvailable = distance <= collectionRange;
            
            // Обновляем доступные ресурсы при приближении игрока
            if (_isCollectionAvailable)
            {
                UpdateAvailableResources();
                
                if (!wasAvailable)
                {
                    ShowCollectionUI();
                }
            }
            else if (wasAvailable)
            {
                HideCollectionUI();
            }
            
            // Обработка нажатия клавиши
            if (_isCollectionAvailable && Input.GetKeyDown(collectKey))
            {
                CollectResources();
            }
        }
        
        private void UpdateAvailableResources()
        {
            if (FarmManager.Instance != null)
            {
                _availableResources = FarmManager.Instance.GetAvailableResources();
                UpdateResourcesUI();
            }
        }
        
        private void UpdateResourcesUI()
        {
            if (resourcesText == null) return;
            
            if (_availableResources.Count == 0)
            {
                resourcesText.text = LocalizationManager.LocalizationManager.Instance.GetString("farmer-no-resources");
                
                if (collectButton != null)
                {
                    collectButton.interactable = false;
                    
                    if (collectButtonText != null)
                    {
                        collectButtonText.text = LocalizationManager.LocalizationManager.Instance.GetString("farmer-nothing-to-collect");
                    }
                    
                    if (collectButtonIcon != null && emptySprite != null)
                    {
                        collectButtonIcon.sprite = emptySprite;
                    }
                }
            }
            else
            {
                string resourcesString = "";
                int totalAmount = 0;
                
                foreach (var resource in _availableResources)
                {
                    resourcesString += $"{LocalizationManager.LocalizationManager.Instance.GetString(resource.Key.ToString())}: {resource.Value}\n";
                    totalAmount += resource.Value;
                }
                
                resourcesText.text = resourcesString;
                
                if (collectButton != null)
                {
                    collectButton.interactable = true;
                    
                    if (collectButtonText != null)
                    {
                        collectButtonText.text = LocalizationManager.LocalizationManager.Instance.GetString("farmer-collect-resources", totalAmount.ToString());
                    }
                    
                    if (collectButtonIcon != null && collectSprite != null)
                    {
                        collectButtonIcon.sprite = collectSprite;
                    }
                }
            }
        }
        
        private void ShowCollectionUI()
        {
            if (collectionPanel != null)
            {
                collectionPanel.SetActive(true);
                
                if (UIManager.Instance != null)
                {
                    UIManager.Instance.RegisterUIOpen();
                }
            }
        }
        
        private void HideCollectionUI()
        {
            if (collectionPanel != null)
            {
                collectionPanel.SetActive(false);
                
                if (UIManager.Instance != null)
                {
                    UIManager.Instance.RegisterUIClose();
                }
            }
        }
        
        public void CollectResources()
        {
            if (FarmManager.Instance == null) return;
            
            var collectedResources = FarmManager.Instance.CollectAllAccumulatedResources();
            
            if (collectedResources.Count == 0)
            {
                // Можно показать сообщение "Нет ресурсов для сбора"
                Debug.Log("Нет ресурсов для сбора");
                return;
            }
            
            // Анимация сбора
            if (farmerAnimator != null)
            {
                farmerAnimator.SetTrigger(collectAnimation);
            }
            
            // Звук сбора
            if (collectSound != null && SoundManager.Instance != null)
            {
                SoundManager.Instance.PlayOneShot(collectSound);
            }
            
            // Добавляем ресурсы в инвентарь игрока
            int totalCollected = 0;
            foreach (var resource in collectedResources)
            {
                Inventory.Instance.AddItem(resource.Key, resource.Value);
                totalCollected += resource.Value;
                
                // Уведомляем AI Assistant
                if (AIAssistant.Instance != null)
                {
                    AIAssistant.Instance.OnResourceCollected(resource.Key, resource.Value);
                }
            }
            
            // Обновляем UI
            UpdateAvailableResources();
            
            // Показываем уведомление
            if (RelationshipNotificationUI.Instance != null)
            {
                RelationshipNotificationUI.Instance.ShowTemporaryMessage(
                    LocalizationManager.LocalizationManager.Instance.GetString("farmer-collected", totalCollected.ToString()),
                    2f
                );
            }
            
            // Автосохранение
            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.AutoSave();
            }
        }
        
        // Метод для принудительного сбора (можно вызвать из других скриптов)
        public void ForceCollectResources()
        {
            CollectResources();
        }
        
        private void OnDrawGizmosSelected()
        {
            // Визуализация радиуса сбора
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, collectionRange);
        }
    }
}