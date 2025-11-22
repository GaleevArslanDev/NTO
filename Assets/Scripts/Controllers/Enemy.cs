using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public abstract class Enemy : MonoBehaviour
{
    [Header("Enemy Base Settings")]
    public float health = 30f;
    public float damage = 10f;
    public float attackRange = 2f;
    public float attackCooldown = 1.5f;
    public float detectionRange = 15f;
    public float movementSpeed = 3f;
    public float acceleration = 8f;
    
    [Header("Emergence Settings")]
    public float emergenceTime = 1.5f;
    public float emergenceHeight = 2f;
    public GameObject emergeEffect;
    
    [Header("Audio")]
    public AudioClip attackSound;
    public AudioClip damageSound;
    public AudioClip deathSound;
    
    // Компоненты
    protected Transform player;
    protected NavMeshAgent agent;
    protected Animator animator;
    protected AudioSource audioSource;
    protected Collider enemyCollider;
    protected EnemyHealth enemyHealth;
    
    // Состояния
    protected bool isEmerging = true;
    protected bool isDead = false;
    protected bool canAttack = true;
    
    // Таймеры
    protected float lastAttackTime;
    protected Vector3 targetPosition;
    
    // Анимационные хэши
    protected readonly int emergeHash = Animator.StringToHash("Emerge");
    protected readonly int moveHash = Animator.StringToHash("Move");
    protected readonly int attackHash = Animator.StringToHash("Attack");
    protected readonly int dieHash = Animator.StringToHash("Die");
    
    // Свойства
    public bool IsDead => isDead;
    public bool IsEmerging => isEmerging;
    
    protected virtual void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();
        enemyCollider = GetComponent<Collider>();
        enemyHealth = GetComponent<EnemyHealth>();
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        
        SetupNavMeshAgent();
        SetupHealthSystem();
    }
    
    protected virtual void Start()
    {
        // Начинаем появление из-под земли
        if (player == null)
        {
            Debug.LogError("Player not found! Make sure player has 'Player' tag.");
            // Попробуем найти игрока снова
            player = GameObject.FindGameObjectWithTag("Player")?.transform;
        }
    
        StartCoroutine(EmergeFromGround());
    }
    
    protected virtual void SetupHealthSystem()
    {
        if (enemyHealth != null)
        {
            enemyHealth.maxHealth = health;
            enemyHealth.currentHealth = health;
            
            // Подписываемся на события здоровья
            enemyHealth.OnEnemyDied += OnHealthDepleted;
            enemyHealth.OnHealthChanged += OnHealthChanged;
        }
    }
    
    protected virtual void Update()
    {
        if (isEmerging || isDead) return;
        if (player == null) return;
        
        UpdateEnemyBehavior();
    }
    
    protected virtual void SetupNavMeshAgent()
    {
        if (agent != null)
        {
            agent.speed = movementSpeed;
            agent.acceleration = acceleration;
            agent.stoppingDistance = attackRange - 0.5f;
            agent.enabled = false; // Включится после появления
        }
    }
    
    protected virtual void OnEnable()
    {
        // Сброс состояний при повторном использовании объекта
        isDead = false;
        isEmerging = true;
        canAttack = true;
    
        if (enemyCollider != null)
            enemyCollider.enabled = false;
    }
    
    protected virtual void OnDisable()
    {
        // Останавливаем все корутины при отключении
        StopAllCoroutines();
        CancelInvoke();
    }
    
    protected virtual IEnumerator EmergeFromGround()
    {
        // Сохраняем целевую позицию
        targetPosition = transform.position;
        var spawnedParticleSystem = Instantiate(emergeEffect, targetPosition, Quaternion.identity).GetComponent<ParticleSystem>();
        spawnedParticleSystem.Play();
    
        // Начинаем под землей
        Vector3 startPosition = targetPosition - Vector3.up * emergenceHeight;
        transform.position = startPosition;
    
        // Отключаем коллайдер на время появления
        if (enemyCollider != null)
            enemyCollider.enabled = false;
    
        // Запускаем анимацию появления
        if (animator != null)
        {
            animator.SetTrigger(emergeHash);
        }
    
        // Плавно поднимаемся
        float elapsedTime = 0f;
        while (elapsedTime < emergenceTime)
        {
            float progress = elapsedTime / emergenceTime;
            if (progress >= 0.9)
            {
                spawnedParticleSystem.Stop();
            }
            // Используем более плавную интерполяцию
            float smoothProgress = Mathf.SmoothStep(0f, 1f, progress);
            transform.position = Vector3.Lerp(startPosition, targetPosition, smoothProgress);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
    
        transform.position = targetPosition;
        isEmerging = false;
    
        // Включаем коллайдер и NavMeshAgent после появления
        if (enemyCollider != null)
            enemyCollider.enabled = true;
    
        if (agent != null)
        {
            agent.enabled = true;
            // Важно: установить позицию для NavMeshAgent
            agent.Warp(targetPosition);
        }

        Destroy(spawnedParticleSystem.gameObject);
    
        OnEmergenceComplete();
    }
    
    protected virtual void OnEmergenceComplete()
    {
        // Можно переопределить в наследниках
    }
    
    protected virtual void UpdateEnemyBehavior()
    {
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        
        if (distanceToPlayer <= attackRange)
        {
            HandleAttackBehavior();
        }
        else if (distanceToPlayer <= detectionRange)
        {
            HandleChaseBehavior();
        }
        else
        {
            HandleIdleBehavior();
        }
    }
    
    protected virtual void HandleAttackBehavior()
    {
        // Останавливаем движение
        if (agent != null && agent.enabled)
        {
            agent.isStopped = true;
        }
        
        // Смотрим на игрока
        FacePlayer();
        
        // Атакуем, если можно
        if (canAttack && Time.time - lastAttackTime >= attackCooldown)
        {
            Attack();
        }
        
        // Обновляем анимацию
        if (animator != null)
        {
            animator.SetBool(moveHash, false);
        }
    }
    
    protected virtual void HandleChaseBehavior()
    {
        // Возобновляем движение
        if (agent != null && agent.enabled)
        {
            agent.isStopped = false;
            agent.SetDestination(player.position);
        }
        
        // Обновляем анимацию
        if (animator != null)
        {
            animator.SetBool(moveHash, true);
        }
    }
    
    protected virtual void HandleIdleBehavior()
    {
        // Останавливаемся
        if (agent != null && agent.enabled)
        {
            agent.isStopped = true;
        }
        
        // Обновляем анимацию
        if (animator != null)
        {
            animator.SetBool(moveHash, false);
        }
    }
    
    protected virtual void FacePlayer()
    {
        Vector3 lookDirection = player.position - transform.position;
        lookDirection.y = 0;
        
        if (lookDirection != Vector3.zero)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, 
                Quaternion.LookRotation(lookDirection), Time.deltaTime * 5f);
        }
    }
    
    protected virtual void Attack()
    {
        lastAttackTime = Time.time;
    
        // Запускаем анимацию атаки
        if (animator != null)
        {
            animator.SetTrigger(attackHash);
        }
    
        // Воспроизводим звук атаки
        if (attackSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(attackSound);
        }
    
        // Наносим урон игроку
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        Debug.Log($"Distance to player: {distanceToPlayer}, Attack range: {attackRange}");
    
        if (distanceToPlayer <= attackRange)
        {
            PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                Debug.Log($"Dealing {damage} damage to player");
                playerHealth.TakeDamage(damage);
            }
            else
            {
                Debug.LogError("PlayerHealth component not found on player!");
            }
        }
        else
        {
            Debug.LogWarning("Player is out of attack range");
        }
    }
    
    public virtual void TakeDamage(float damageAmount, Vector3 hitPoint = default(Vector3))
    {
        if (isDead || isEmerging) return;
        
        // Используем систему здоровья для обработки урона
        if (enemyHealth != null)
        {
            enemyHealth.TakeDamage(damageAmount, hitPoint);
        }
        else
        {
            // Резервный вариант, если системы здоровья нет
            health -= damageAmount;
            
            if (health <= 0)
            {
                Die();
            }
        }
        
        // Воспроизводим звук получения урона
        if (damageSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(damageSound);
        }
    }
    
    protected virtual void OnHealthChanged(float currentHealth)
    {
        // Обновляем локальную переменную здоровья
        health = currentHealth;
    }
    
    protected virtual void OnHealthDepleted(EnemyHealth healthComponent)
    {
        Die();
    }
    
    protected virtual void Die()
    {
        if (isDead) return;
        isDead = true;
        
        // Воспроизводим звук смерти
        if (deathSound != null && audioSource != null)
        {
            AudioSource.PlayClipAtPoint(deathSound, transform.position);
        }
        
        // Уведомляем менеджер спавна
        if (EnemySpawnManager.Instance != null)
        {
            EnemySpawnManager.Instance.RemoveEnemy(this);
        }
        
        // Запускаем мгновенную смерть с партиклами
        EnemyDeath enemyDeath = GetComponent<EnemyDeath>();
        if (enemyDeath != null)
        {
            enemyDeath.InstantDeathWithParticles();
        }
        else
        {
            // Если компонента смерти нет, просто уничтожаем
            Destroy(gameObject);
        }
        
        QuestSystem.Instance.ReportEnemyKill(this.GetType().Name);
        
        OnDeath();
    }
    
    protected virtual void OnDeath()
    {
        // Можно переопределить в наследниках
    }
    
    // Метод для анимации атаки (вызывается из Animation Event)
    public virtual void OnAttackAnimationEvent()
    {
        // Дополнительная логика атаки
    }
    
    protected virtual void OnDrawGizmosSelected()
    {
        // Диапазон обнаружения
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        
        // Диапазон атаки
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}