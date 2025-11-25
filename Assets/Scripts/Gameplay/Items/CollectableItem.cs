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
        [SerializeField] private Slider breakProgressSlider;
        [SerializeField] private Canvas breakCanvas;
        
        private bool _isCollected;
        private bool _isBeingBroken;
        private float _currentBreakProgress;
        private Rigidbody _rb;
        private Collider _coll;
        private Vector3 _originalPosition;

        private void Start()
        {
            _rb = GetComponent<Rigidbody>();
            _coll = GetComponent<Collider>();
            _originalPosition = transform.position;

            if (breakCanvas == null) return;
            breakCanvas.worldCamera = Camera.main;
            breakProgressUI.SetActive(false);
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
                breakProgressSlider.value = 0;
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
                    breakProgressSlider.value = _currentBreakProgress;
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
    
        public bool CanBeCollected => _currentBreakProgress >= 1f;
        public bool IsBeingBroken => _isBeingBroken;
    }
}