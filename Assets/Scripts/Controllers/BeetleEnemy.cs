using System.Collections;
using UnityEngine;

public class BeetleEnemy : Enemy
{
    [Header("Beetle Specific Settings")]
    public float chargeDamageMultiplier = 1.5f;
    public float chargeSpeedMultiplier = 1.5f;
    public float chargeCooldown = 5f;
    public float chargeWindupTime = 0.5f;
    
    private bool isCharging = false;
    private float lastChargeTime;
    private float originalSpeed;
    private float originalDamage;
    
    // Анимационные хэши для специфичных анимаций жука
    private readonly int chargeHash = Animator.StringToHash("Charge");
    private readonly int chargeAttackHash = Animator.StringToHash("ChargeAttack");
    
    protected override void Awake()
    {
        base.Awake();
        
        // Сохраняем оригинальные значения
        originalSpeed = movementSpeed;
        originalDamage = damage;
    }
    
    protected override void UpdateEnemyBehavior()
    {
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        
        // Проверяем возможность заряда
        if (!isCharging && !isEmerging && !isDead && 
            Time.time - lastChargeTime >= chargeCooldown &&
            distanceToPlayer <= detectionRange && distanceToPlayer > attackRange)
        {
            StartCharge();
        }
        
        base.UpdateEnemyBehavior();
    }
    
    protected override void HandleAttackBehavior()
    {
        if (isCharging)
        {
            HandleChargeAttack();
        }
        else
        {
            base.HandleAttackBehavior();
        }
    }
    
    protected override void HandleChaseBehavior()
    {
        if (isCharging)
        {
            // Во время заряда просто продолжаем движение к игроку
            if (agent != null && agent.enabled)
            {
                agent.isStopped = false;
                agent.SetDestination(player.position);
            }
        }
        else
        {
            base.HandleChaseBehavior();
        }
    }
    
    private void StartCharge()
    {
        if (isCharging || isEmerging || isDead) return;
    
        isCharging = true;
        lastChargeTime = Time.time;
    
        // Увеличиваем скорость и урон
        if (agent != null && agent.enabled)
        {
            agent.speed = originalSpeed * chargeSpeedMultiplier;
            agent.acceleration = acceleration * 2f; // Увеличиваем ускорение для заряда
        }
        damage = originalDamage * chargeDamageMultiplier;
    
        // Запускаем анимацию заряда
        if (animator != null)
        {
            animator.SetTrigger(chargeHash);
            animator.SetBool(moveHash, true);
        }
    
        // Завершаем заряд через время или при достижении цели
        StartCoroutine(ChargeRoutine());
    }
    
    private IEnumerator ChargeRoutine()
    {
        float chargeDuration = 3f;
        float elapsedTime = 0f;
    
        while (elapsedTime < chargeDuration && isCharging && !isDead)
        {
            if (player != null && agent != null && agent.enabled)
            {
                agent.SetDestination(player.position);
            }
            elapsedTime += Time.deltaTime;
            yield return null;
        }
    
        EndCharge();
    }
    
    private void EndCharge()
    {
        if (!isCharging) return;
        
        isCharging = false;
        
        // Восстанавливаем оригинальные значения
        if (agent != null)
        {
            agent.speed = originalSpeed;
        }
        damage = originalDamage;
    }
    
    private void HandleChargeAttack()
    {
        // Во время заряда атака происходит при столкновении
        // Базовая логика движения уже обрабатывается в HandleChaseBehavior
        
        if (animator != null)
        {
            animator.SetBool(moveHash, true);
        }
        
        FacePlayer();
    }
    
    // Переопределяем метод TakeDamage для прерывания заряда при получении урона
    public override void TakeDamage(float damageAmount, Vector3 hitPoint = default(Vector3))
    {
        base.TakeDamage(damageAmount, hitPoint);
        
        // Прерываем заряд при получении урона
        if (isCharging)
        {
            EndCharge();
        }
    }
    
    protected override void OnDeath()
    {
        // Останавливаем все инвоки
        CancelInvoke();
        
        // Специфичные эффекты смерти жука
        // Например, разлет хитина или что-то подобное
    }
    
    // Специфичная атака жука при заряде
    public void OnChargeAttackAnimationEvent()
    {
        if (!isCharging) return;
        
        // Дополнительный урон или эффекты при атаке во время заряда
        if (Vector3.Distance(transform.position, player.position) <= attackRange * 1.5f)
        {
            PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damage * 1.2f); // Дополнительный множитель
                
                // Можно добавить отталкивание игрока
                Rigidbody playerRb = player.GetComponent<Rigidbody>();
                if (playerRb != null)
                {
                    Vector3 pushDirection = (player.position - transform.position).normalized;
                    playerRb.AddForce(pushDirection * 5f, ForceMode.Impulse);
                }
            }
        }
    }
}