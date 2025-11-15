using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Vacuum : MonoBehaviour
{
    public static Vacuum Instance;
    
    [Header("Vacuum Settings")]
    [SerializeField] private float vacuumRadius = 5f;
    [SerializeField] private LayerMask itemLayer = 1;
    [SerializeField] private ParticleSystem vacuumParticles;
    
    [Header("Animation")]
    [SerializeField] private float collectionDelay = 0.1f;
    
    private bool isVacuuming = false;
    private CollectableItem currentTargetItem;
    
    void Awake()
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
    
    void Update()
    {
        HandleItemTargeting();
    }
    
    private void HandleItemTargeting()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        
        CollectableItem newTarget = null;
        
        if (Physics.Raycast(ray, out hit, vacuumRadius, itemLayer))
        {
            newTarget = hit.collider.gameObject.GetComponent<CollectableItem>();
        }
        
        // Если цель изменилась
        if (newTarget != currentTargetItem)
        {
            // Останавливаем ломание предыдущей цели
            if (currentTargetItem != null)
            {
                currentTargetItem.StopBreaking();
            }
            
            currentTargetItem = newTarget;
        }
        
        // Управление ломанием предмета
        if (currentTargetItem != null && !currentTargetItem.CanBeCollected)
        {
            if (Input.GetMouseButton(1)) // Зажата ПКМ
            {
                if (!currentTargetItem.IsBeingBroken)
                {
                    currentTargetItem.StartBreaking();
                }
            }
            else if (Input.GetMouseButtonUp(1))
            {
                currentTargetItem.StopBreaking();
            }
        }
    }
    
    private IEnumerator VacuumRoutine(CollectableItem item)
    {
        isVacuuming = true;
    
        // Настройка цвета частиц в зависимости от предмета
        SetParticleColor(item.Data.ParticleColor);
        vacuumParticles.Play();
            
        // Запуск сбора предмета
        item.StartCollection(transform);
            
        // Задержка между сбором предметов
        yield return new WaitForSeconds(collectionDelay);
    
        isVacuuming = false;
    }
    
    public void StartVacuuming(CollectableItem item)
    {
        if (!isVacuuming)
        {
            StartCoroutine(VacuumRoutine(item));
        }
    }
    
    private void SetParticleColor(Color color)
    {
        var main = vacuumParticles.main;
        main.startColor = color;
    }
}