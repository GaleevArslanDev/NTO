using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MTB : MonoBehaviour
{
    public static MTB Instance;
    
    [Header("Vacuum Settings")]
    [SerializeField] private float vacuumRadius = 5f;
    [SerializeField] private LayerMask itemLayer = 1;
    [SerializeField] private ParticleSystem vacuumParticles;
    
    [Header("Animation")]
    [SerializeField] private float collectionDelay = 0.1f;
    
    [Header("Weapon Settings")]
    [SerializeField] private float weaponRange = 50f;
    [SerializeField] private float fireRate = 0.5f;
    [SerializeField] private float weaponDamage = 10f;
    [SerializeField] private LayerMask enemyLayer = 1;
    
    [Header("Laser Beam Settings")]
    [SerializeField] private LineRenderer laserBeam;
    [SerializeField] private float laserDuration = 0.1f;
    [SerializeField] private Color laserColor = Color.red;
    [SerializeField] private float laserWidth = 0.1f;
    
    [Header("UI References")]
    [SerializeField] private Slider reloadSlider;
    [SerializeField] private Image reloadFill;
    [SerializeField] private Color reloadingColor = Color.red;
    [SerializeField] private Color readyColor = Color.green;
    
    private bool isVacuuming = false;
    private CollectableItem currentTargetItem;
    private float nextFireTime;
    private bool isReloading = false;
    
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
    
    void Start()
    {
        // Настройка лазерного луча
        if (laserBeam != null)
        {
            laserBeam.startColor = laserColor;
            laserBeam.endColor = laserColor;
            laserBeam.startWidth = laserWidth;
            laserBeam.endWidth = laserWidth;
            laserBeam.positionCount = 2;
            laserBeam.enabled = false;
        }
        
        // Настройка индикатора перезарядки
        if (reloadSlider != null)
        {
            reloadSlider.gameObject.SetActive(true);
            reloadSlider.value = 1f;
            UpdateReloadUI(1f);
        }
    }
    
    void Update()
    {
        HandleItemTargeting();
        HandleWeapon();
        UpdateReloadIndicator();
    }
    
    private void UpdateReloadIndicator()
    {
        if (reloadSlider == null) return;
        
        float reloadProgress = 0f;
        
        if (Time.time < nextFireTime)
        {
            // Идет перезарядка
            float timeSinceLastShot = Time.time - (nextFireTime - fireRate);
            reloadProgress = timeSinceLastShot / fireRate;
            isReloading = true;
        }
        else
        {
            // Готов к стрельбе
            reloadProgress = 1f;
            isReloading = false;
        }
        
        reloadSlider.value = reloadProgress;
        UpdateReloadUI(reloadProgress);
    }
    
    private void UpdateReloadUI(float progress)
    {
        if (reloadFill != null)
        {
            reloadFill.color = Color.Lerp(reloadingColor, readyColor, progress);
        }
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
        
        // Управление ломанием предмета (ПКМ)
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
    
    private void HandleWeapon()
    {
        // Стрельба по ЛКМ
        if (Input.GetMouseButtonDown(0) && Time.time >= nextFireTime && !isReloading)
        {
            Shoot();
            nextFireTime = Time.time + fireRate;
        }
    }
    
    private void Shoot()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        
        // Визуализация лазерного луча
        ShowLaserBeam();
        
        if (Physics.Raycast(ray, out hit, weaponRange, enemyLayer))
        {
            // Наносим урон врагу
            Enemy enemy = hit.collider.GetComponent<Enemy>();
            if (enemy != null)
            {
                enemy.TakeDamage(weaponDamage);
                Debug.Log($"Попали во врага! Нанесено урона: {weaponDamage}");
            }
        }
        else
        {
            Debug.Log("Выстрел, но не попали в цель");
        }
    }
    
    private void ShowLaserBeam()
    {
        if (laserBeam == null) return;
        
        StartCoroutine(LaserBeamRoutine());
    }
    
    private IEnumerator LaserBeamRoutine()
    {
        laserBeam.enabled = true;
        
        float elapsedTime = 0f;
        
        while (elapsedTime < laserDuration)
        {
            // Каждый кадр обновляем позиции луча
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            Vector3 startPoint = transform.position; // Текущая позиция MTB
            Vector3 endPoint;
            
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, weaponRange, enemyLayer))
            {
                endPoint = hit.point;
            }
            else
            {
                endPoint = ray.origin + ray.direction * weaponRange;
            }
            
            // Обновляем позиции луча
            laserBeam.SetPosition(0, startPoint);
            laserBeam.SetPosition(1, endPoint);
            
            elapsedTime += Time.deltaTime;
            yield return null; // Ждем следующий кадр
        }
        
        laserBeam.enabled = false;
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