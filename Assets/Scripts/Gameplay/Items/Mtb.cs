using System.Collections;
using Gameplay.Characters.Enemies;
using UnityEngine;
using UnityEngine.UI;

namespace Gameplay.Items
{
    public class Mtb : MonoBehaviour
    {
        public static Mtb Instance;
    
        [Header("Vacuum Settings")]
        [SerializeField] private float vacuumRadius = 5f;
        [SerializeField] private LayerMask itemLayer = 1;
        [SerializeField] private ParticleSystem vacuumParticles;
        [SerializeField] private float vacuumSpeedMultiplier = 1f;
        [SerializeField] private float vacuumSpeed = 1f;
    
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
    
        private bool _isVacuuming;
        private CollectableItem _currentTargetItem;
        private float _nextFireTime;
        private bool _isReloading;
        private float _baseWeaponDamage;
        private float _baseVacuumRadius;
        private float _baseWeaponFireRate;
        private float _baseVacuumSpeedMultiplier;
        private float _baseVacuumSpeed;

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
            _baseWeaponDamage = weaponDamage;
            _baseWeaponFireRate = fireRate;
            _baseVacuumRadius = vacuumRadius;
            _baseVacuumSpeedMultiplier = vacuumSpeedMultiplier;
        
            _baseVacuumSpeed = vacuumSpeed;
        }

        private void Start()
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

            if (reloadSlider == null) return;
            reloadSlider.gameObject.SetActive(true);
            reloadSlider.value = 1f;
            UpdateReloadUI(1f);
        }

        private void Update()
        {
            UpdateReloadIndicator();
            if (Cursor.lockState == CursorLockMode.None) return;
            HandleItemTargeting();
            HandleWeapon();
        }
    
        private void UpdateReloadIndicator()
        {
            if (reloadSlider == null) return;
        
            float reloadProgress;
        
            if (Time.time < _nextFireTime)
            {
                var timeSinceLastShot = Time.time - (_nextFireTime - fireRate);
                reloadProgress = timeSinceLastShot / fireRate;
                _isReloading = true;
            }
            else
            {
                reloadProgress = 1f;
                _isReloading = false;
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
            if (Camera.main != null)
            {
                var ray = Camera.main.ScreenPointToRay(Input.mousePosition);

                CollectableItem newTarget = null;
        
                if (Physics.Raycast(ray, out var hit, vacuumRadius, itemLayer))
                {
                    newTarget = hit.collider.gameObject.GetComponent<CollectableItem>();
                }
        
                if (newTarget != _currentTargetItem)
                {
                    if (_currentTargetItem != null)
                    {
                        _currentTargetItem.StopBreaking(_currentTargetItem.data.vacuumTime * (1f / vacuumSpeedMultiplier));
                    }
            
                    _currentTargetItem = newTarget;
                }
            }

            if (_currentTargetItem == null || _currentTargetItem.CanBeCollected) return;
            if (Input.GetMouseButton(1))
            {
                if (!_currentTargetItem.IsBeingBroken)
                {
                    _currentTargetItem.StartBreaking(vacuumSpeedMultiplier);
                }
            }
            else if (Input.GetMouseButtonUp(1))
            {
                _currentTargetItem.StopBreaking(_currentTargetItem.data.vacuumTime * (1f / vacuumSpeedMultiplier));
            }
        }
    
        private void HandleWeapon()
        {
            if (!Input.GetMouseButtonDown(0) || !(Time.time >= _nextFireTime) || _isReloading) return;
            Shoot();
            _nextFireTime = Time.time + fireRate;
        }
    
        private void Shoot()
        {
            if (Camera.main == null) return;
            var ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            ShowLaserBeam();
    
            if (Physics.Raycast(ray, out var hit, weaponRange, enemyLayer))
            {
                var enemy = hit.collider.GetComponent<Enemy>();
                if (enemy != null)
                {
                    enemy.TakeDamage(weaponDamage, hit.point);
                }
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
        
            var elapsedTime = 0f;
        
            while (elapsedTime < laserDuration)
            {
                if (Camera.main != null)
                {
                    var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                    var startPoint = transform.position;
                    Vector3 endPoint;

                    if (Physics.Raycast(ray, out var hit, weaponRange, enemyLayer))
                    {
                        endPoint = hit.point;
                    }
                    else
                    {
                        endPoint = ray.origin + ray.direction * weaponRange;
                    }
            
                    laserBeam.SetPosition(0, startPoint);
                    laserBeam.SetPosition(1, endPoint);
                }

                elapsedTime += Time.deltaTime;
                yield return null;
            }
        
            laserBeam.enabled = false;
        }
    
        public void UpdateStats(float damageMult, float miningSpeedMult, float collectionRangeMult, float weaponFireRateMult)
        {
            weaponDamage = _baseWeaponDamage * damageMult;
            fireRate = _baseWeaponFireRate * weaponFireRateMult;
            vacuumRadius = _baseVacuumRadius * collectionRangeMult;
            vacuumSpeedMultiplier = _baseVacuumSpeedMultiplier * miningSpeedMult;
        }
    
        private IEnumerator VacuumRoutine(CollectableItem item)
        {
            _isVacuuming = true;
    
            SetParticleColor(item.data.particleColor);
            vacuumParticles.Play();
            
            item.StartCollection(transform, vacuumSpeedMultiplier);
            
            yield return new WaitForSeconds(item.data.vacuumTime * (1f / vacuumSpeedMultiplier));
    
            _isVacuuming = false;
        }
    
        public void StartVacuuming(CollectableItem item)
        {
            if (!_isVacuuming)
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
}