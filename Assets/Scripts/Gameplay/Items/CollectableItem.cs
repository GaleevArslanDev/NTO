using System.Collections;
using Data.Inventory;
using Gameplay.Systems;
using UI;
using UnityEngine;
using UnityEngine.UI;

namespace Gameplay.Items
{
    public class CollectableItem : MonoBehaviour
    {
        public ItemData data;
    
        [Header("UI References")]
        [SerializeField] private GameObject breakProgressUI;
        [SerializeField] private CollectableSlider breakProgressSlider;
        [SerializeField] private Canvas breakCanvas;
        
        [Header("Persistence")]
        [SerializeField] private string resourceId; // Уникальный ID для сохранения
        
        private bool _isCollected;
        private bool _isBeingBroken;
        private float _currentBreakProgress;
        private Rigidbody _rb;
        private Collider _coll;
        private Vector3 _originalPosition;
        private bool _hasCheckedCollection = false;

        private void Start()
        {
            if (string.IsNullOrEmpty(resourceId))
            {
                resourceId = $"{transform.position.x}_{transform.position.y}_{transform.position.z}";
            }
            
            _rb = GetComponent<Rigidbody>();
            _coll = GetComponent<Collider>();
            _originalPosition = transform.position;
            
            if (breakCanvas != null)
            {
                breakCanvas.worldCamera = Camera.main;
                breakProgressUI.SetActive(false);
            }

            // Подписываемся на событие применения данных
            if (ResourceManager.Instance != null)
            {
                ResourceManager.Instance.OnDataApplied += OnResourceDataApplied;
            }

            // Проверяем статус сразу или ждем применения данных
            CheckResourceStatus();
        }

        private void Update()
        {
            if (_isBeingBroken && !_isCollected)
            {
                UpdateBreakingEffects();
            }
        }
    
        public void StartBreaking()
        {
            if (_isCollected || _isBeingBroken) return;
        
            _isBeingBroken = true;
            _currentBreakProgress = 0f;
        
            if (_rb != null) 
            {
                _rb.isKinematic = true;
                _rb.velocity = Vector3.zero;
            }
        
            if (breakProgressUI != null)
            {
                breakProgressUI.SetActive(true);
                breakProgressSlider.UpdateSlider(0);
            }
        
            StartCoroutine(BreakingRoutine());
        }
    
        public void StopBreaking()
        {
            if (!_isBeingBroken) return;
        
            _isBeingBroken = false;
            _currentBreakProgress = 0f;
        
            if (breakProgressUI != null)
            {
                breakProgressUI.SetActive(false);
            }
        
            StopAllCoroutines();
            StartCoroutine(ReturnToOriginalPosition());
        }
    
        private IEnumerator BreakingRoutine()
        {
            var timer = 0f;
        
            while (timer < data.breakTime && _isBeingBroken)
            {
                timer += Time.deltaTime;
                _currentBreakProgress = timer / data.breakTime;
            
                if (breakProgressSlider != null)
                {
                    breakProgressSlider.UpdateSlider(_currentBreakProgress);
                }
            
                yield return null;
            }
        
            if (_isBeingBroken && _currentBreakProgress >= 1f)
            {
                CompleteBreaking();
            }
        }
    
        private void UpdateBreakingEffects()
        {
            var shakeProgress = _currentBreakProgress;
            var shakeIntensity = shakeProgress * data.shakeIntensity;
        
            var shakeOffset = new Vector3(
                Mathf.PerlinNoise(Time.time * 10f, 0) * 2f - 1f,
                Mathf.PerlinNoise(0, Time.time * 10f) * 2f - 1f,
                Mathf.PerlinNoise(Time.time * 10f, Time.time * 10f) * 2f - 1f
            ) * shakeIntensity;
        
            var floatProgress = Mathf.Pow(_currentBreakProgress, 2f); // Квадрат для более плавного подъема
            var currentFloatHeight = floatProgress * data.floatHeight;
        
            transform.position = _originalPosition + 
                                 Vector3.up * currentFloatHeight + 
                                 shakeOffset * 0.1f; // Уменьшаем тряску для позиции
        }
    
        private IEnumerator ReturnToOriginalPosition()
        {
            var startPos = transform.position;
            const float returnTime = 0.5f;
            var timer = 0f;
        
            while (timer < returnTime)
            {
                timer += Time.deltaTime;
                var progress = timer / returnTime;
                transform.position = Vector3.Lerp(startPos, _originalPosition, progress);
                yield return null;
            }
        
            transform.position = _originalPosition;
        }
    
        private void CompleteBreaking()
        {
            _isBeingBroken = false;

            if (breakProgressUI != null)
            {
                breakProgressUI.SetActive(false);
            }
            
            if (ResourceManager.Instance != null)
            {
                ResourceManager.Instance.MarkResourceCollected(resourceId);
            }
        
            if (AIAssistant.Instance != null && data != null)
            {
                AIAssistant.Instance.OnResourceCollected(data.type, 1);
            }

            if (Mtb.Instance != null)
            {
                Mtb.Instance.StartVacuuming(this);
            }
        }
    
        public void StartCollection(Transform target)
        {
            if (_isCollected) return;
            _isCollected = true;
        
            if (_coll != null) _coll.enabled = false;
        
            StartCoroutine(CollectionRoutine(target));
        }
    
        private IEnumerator CollectionRoutine(Transform target)
        {
            const float duration = 0.7f;
            var time = 0f;
            var startPos = transform.position;
            var startScale = transform.localScale;
    
            var randomOffset = Random.insideUnitSphere * 0.1f;
    
            while (time < duration)
            {
                var progress = time / duration;
                var targetPos = target.position + randomOffset;
                transform.position = Vector3.Lerp(startPos, targetPos, progress);
        
                var scaleFactor = Mathf.SmoothStep(1f, 0f, progress);
                transform.localScale = startScale * scaleFactor;
        
                transform.Rotate(0, 180 * Time.deltaTime, 0);
        
                time += Time.deltaTime;
                yield return null;
            }
    
            if (EnemySpawnManager.Instance != null && data != null)
            {
                EnemySpawnManager.Instance.TrySpawnEnemy(data, startPos);
            }
    
            if (Inventory.Instance != null)
            {
                Inventory.Instance.AddItem(data.type);
            }
            else
            {
                Debug.LogWarning("Inventory instance not found!");
            }
    
            Destroy(gameObject);
        }
        
        private void CheckResourceStatus()
        {
            if (_hasCheckedCollection) return;

            if (ResourceManager.Instance == null)
            {
                Debug.LogWarning("ResourceManager not found, cannot check resource status");
                return;
            }

            // Если данные уже применены, проверяем сразу
            if (ResourceManager.Instance.IsDataApplied)
            {
                CheckIfCollected();
            }
            // Иначе ждем события OnDataApplied
        }
        
        private void OnResourceDataApplied()
        {
            CheckIfCollected();
        }
        
        private void CheckIfCollected()
        {
            if (_hasCheckedCollection) return;

            if (ResourceManager.Instance != null && 
                ResourceManager.Instance.IsResourceCollected(resourceId))
            {
                // Ресурс уже собран - уничтожаем
                Debug.Log($"Resource {resourceId} was already collected, destroying");
                Destroy(gameObject);
            }
            else
            {
                // Ресурс не собран - активируем
                _hasCheckedCollection = true;
            }
        }
        
        private void OnDestroy()
        {
            if (ResourceManager.Instance != null)
            {
                ResourceManager.Instance.OnDataApplied -= OnResourceDataApplied;
            }
        }
    
        public bool CanBeCollected => _currentBreakProgress >= 1f;
        public bool IsBeingBroken => _isBeingBroken;
        public string GetResourceId() => resourceId;
    }
}