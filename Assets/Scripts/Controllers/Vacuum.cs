using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Vacuum : MonoBehaviour
{
    [Header("Vacuum Settings")]
    [SerializeField] private float vacuumRadius = 5f;
    [SerializeField] private LayerMask itemLayer = 1;
    [SerializeField] private ParticleSystem vacuumParticles;
    [SerializeField] private KeyCode vacuumKey = KeyCode.E;
    
    [Header("Animation")]
    [SerializeField] private float collectionDelay = 0.1f;
    
    private bool isVacuuming = false;
    private CollectableItem itemLoockingAt;
    
    void Update()
    {
        // Активация пылесоса
        if (Input.GetKeyDown(vacuumKey) && !isVacuuming)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            
            if (Physics.Raycast(ray, out hit))
            {
                itemLoockingAt = hit.collider.gameObject.GetComponent<CollectableItem>();
                if (itemLoockingAt != null)
                    StartCoroutine(VacuumRoutine());
            }
        }
    }
    
    private IEnumerator VacuumRoutine()
    {
        isVacuuming = true;
        
        // Настройка цвета частиц в зависимости от предмета
        SetParticleColor(itemLoockingAt.Data.ParticleColor);
        vacuumParticles.Play();
                
        // Запуск сбора предмета
        itemLoockingAt.StartCollection(transform);
                
        // Задержка между сбором предметов
        yield return new WaitForSeconds(collectionDelay);
        
        isVacuuming = false;
    }
    
    private void SetParticleColor(Color color)
    {
        var main = vacuumParticles.main;
        main.startColor = color;
    }
}