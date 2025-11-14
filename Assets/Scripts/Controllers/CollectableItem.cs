using System.Collections;
using UnityEngine;

public class CollectableItem : MonoBehaviour
{
    public ItemData Data;
    private bool isCollected;
    private Rigidbody rb;
    private Collider coll;
    
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        coll = GetComponent<Collider>();
        
        // Автоматически настраиваем цвет материала, если это кристалл
        if (Data != null && Data.Type.ToString().Contains("Crystal"))
        {
            var renderer = GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = Data.ParticleColor;
            }
        }
    }
    
    public void StartCollection(Transform target)
    {
        if (isCollected) return;
        isCollected = true;
        
        // Отключаем физику при сборе
        if (rb != null) rb.isKinematic = true;
        if (coll != null) coll.enabled = false;
        
        StartCoroutine(CollectionRoutine(target));
    }
    
    private IEnumerator CollectionRoutine(Transform target)
    {
        float duration = 0.5f;
        float time = 0;
        Vector3 startPos = transform.position;
        
        Vector3 randomOffset = Random.insideUnitSphere * 0.3f;
        
        while (time < duration)
        {
            // Плавное движение с подпрыгивающим эффектом
            float progress = time / duration;
            Vector3 targetPos = target.position + randomOffset;
            transform.position = Vector3.Lerp(startPos, targetPos, progress);
            
            // Вращение предмета при движении
            transform.Rotate(0, 180 * Time.deltaTime, 0);
            
            // Уменьшение размера к концу анимации
            transform.localScale = Vector3.one * (1 - progress * 0.5f);
            
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
}