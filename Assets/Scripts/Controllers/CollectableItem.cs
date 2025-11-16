using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class CollectableItem : MonoBehaviour
{
    public ItemData Data;
    
    private bool isCollected;
    private bool isBeingBroken;
    private float currentBreakProgress;
    private Rigidbody rb;
    private Collider coll;
    private Vector3 originalPosition;
    
    [Header("UI References")]
    [SerializeField] private GameObject breakProgressUI;
    [SerializeField] private Slider breakProgressSlider;
    [SerializeField] private Canvas breakCanvas;
    
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        coll = GetComponent<Collider>();
        originalPosition = transform.position;
        
        if (breakCanvas != null)
        {
            breakCanvas.worldCamera = Camera.main;
            breakProgressUI.SetActive(false);
        }
        
        if (Data != null && Data.Type.ToString().Contains("Crystal"))
        {
            var renderer = GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = Data.ParticleColor;
            }
        }
    }
    
    void Update()
    {
        if (isBeingBroken && !isCollected)
        {
            UpdateBreakingEffects();
        }
    }
    
    public void StartBreaking()
    {
        if (isCollected || isBeingBroken) return;
        
        isBeingBroken = true;
        currentBreakProgress = 0f;
        
        if (rb != null) 
        {
            rb.isKinematic = true;
            rb.velocity = Vector3.zero;
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
        if (!isBeingBroken) return;
        
        isBeingBroken = false;
        currentBreakProgress = 0f;
        
        if (breakProgressUI != null)
        {
            breakProgressUI.SetActive(false);
        }
        
        // Плавно возвращаем на место
        StopAllCoroutines();
        StartCoroutine(ReturnToOriginalPosition());
    }
    
    private IEnumerator BreakingRoutine()
    {
        float timer = 0f;
        
        while (timer < Data.BreakTime && isBeingBroken)
        {
            timer += Time.deltaTime;
            currentBreakProgress = timer / Data.BreakTime;
            
            if (breakProgressSlider != null)
            {
                breakProgressSlider.value = currentBreakProgress;
            }
            
            yield return null;
        }
        
        if (isBeingBroken && currentBreakProgress >= 1f)
        {
            // Предмет полностью сломан - готов к сбору
            CompleteBreaking();
        }
    }
    
    private void UpdateBreakingEffects()
    {
        // Тряска с увеличением интенсивности
        float shakeProgress = currentBreakProgress;
        float shakeIntensity = shakeProgress * Data.ShakeIntensity;
        
        Vector3 shakeOffset = new Vector3(
            Mathf.PerlinNoise(Time.time * 10f, 0) * 2f - 1f,
            Mathf.PerlinNoise(0, Time.time * 10f) * 2f - 1f,
            Mathf.PerlinNoise(Time.time * 10f, Time.time * 10f) * 2f - 1f
        ) * shakeIntensity;
        
        // Подъем с прогрессом
        float floatProgress = Mathf.Pow(currentBreakProgress, 2f); // Квадрат для более плавного подъема
        float currentFloatHeight = floatProgress * Data.FloatHeight;
        
        transform.position = originalPosition + 
                           Vector3.up * currentFloatHeight + 
                           shakeOffset * 0.1f; // Уменьшаем тряску для позиции
    }
    
    private IEnumerator ReturnToOriginalPosition()
    {
        Vector3 startPos = transform.position;
        float returnTime = 0.5f;
        float timer = 0f;
        
        while (timer < returnTime)
        {
            timer += Time.deltaTime;
            float progress = timer / returnTime;
            transform.position = Vector3.Lerp(startPos, originalPosition, progress);
            yield return null;
        }
        
        transform.position = originalPosition;
    }
    
    private void CompleteBreaking()
    {
        isBeingBroken = false;

        if (breakProgressUI != null)
        {
            breakProgressUI.SetActive(false);
        }

        // Сразу начинаем сбор после завершения ломания
        if (MTB.Instance != null)
        {
            MTB.Instance.StartVacuuming(this);
        }
    }
    
    public void StartCollection(Transform target)
    {
        if (isCollected) return;
        isCollected = true;
        
        if (coll != null) coll.enabled = false;
        
        StartCoroutine(CollectionRoutine(target));
    }
    
    private IEnumerator CollectionRoutine(Transform target)
    {
        float duration = 0.7f;
        float time = 0;
        Vector3 startPos = transform.position;
        Vector3 startScale = transform.localScale;
        
        Vector3 randomOffset = Random.insideUnitSphere * 0.1f;
        
        while (time < duration)
        {
            float progress = time / duration;
            Vector3 targetPos = target.position + randomOffset;
            transform.position = Vector3.Lerp(startPos, targetPos, progress);
            
            float scaleFactor = Mathf.SmoothStep(1f, 0f, progress);
            transform.localScale = startScale * scaleFactor;
            
            transform.Rotate(0, 180 * Time.deltaTime, 0);
            
            time += Time.deltaTime;
            yield return null;
        }
        
        if (Inventory.Instance != null)
        {
            Inventory.Instance.AddItem(Data.Type);
        }
        else
        {
            Debug.LogWarning("Inventory instance not found!");
        }
        
        Destroy(gameObject);
    }
    
    public bool CanBeCollected => currentBreakProgress >= 1f;
    public bool IsBeingBroken => isBeingBroken;
}