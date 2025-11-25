using System.Collections;
using Gameplay.Characters.Player;
using Gameplay.Systems;
using UI;
using UnityEngine;
using UnityEngine.AI;

namespace Gameplay.Characters.Enemies
{
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
    
        protected Transform Player;
        protected NavMeshAgent Agent;
        protected Animator Animator;
        protected AudioSource AudioSource;
        protected Collider EnemyCollider;
        protected EnemyHealth EnemyHealth;
    
        protected bool IsEmerging = true;
        protected bool IsDead;
        protected bool CanAttack = true;
    
        protected float LastAttackTime;
        protected Vector3 TargetPosition;
    
        protected readonly int EmergeHash = Animator.StringToHash("Emerge");
        protected readonly int MoveHash = Animator.StringToHash("Move");
        protected readonly int AttackHash = Animator.StringToHash("Attack");
        protected readonly int DieHash = Animator.StringToHash("Die");
    
        protected virtual void Awake()
        {
            Agent = GetComponent<NavMeshAgent>();
            Animator = GetComponent<Animator>();
            AudioSource = GetComponent<AudioSource>();
            EnemyCollider = GetComponent<Collider>();
            EnemyHealth = GetComponent<EnemyHealth>();
            Player = GameObject.FindGameObjectWithTag("Player")?.transform;
        
            SetupNavMeshAgent();
            SetupHealthSystem();
        }
    
        protected virtual void Start()
        {
            if (Player == null)
            {
                Debug.LogError("Player not found! Make sure player has 'Player' tag.");

                Player = GameObject.FindGameObjectWithTag("Player")?.transform;
            }
    
            StartCoroutine(EmergeFromGround());
        }
    
        protected virtual void SetupHealthSystem()
        {
            if (EnemyHealth == null) return;
            EnemyHealth.maxHealth = health;
            EnemyHealth.currentHealth = health;
            
            EnemyHealth.OnEnemyDied += OnHealthDepleted;
            EnemyHealth.OnHealthChanged += OnHealthChanged;
        }
    
        protected virtual void Update()
        {
            if (IsEmerging || IsDead) return;
            if (Player == null) return;
        
            UpdateEnemyBehavior();
        }
    
        protected virtual void SetupNavMeshAgent()
        {
            if (Agent == null) return;
            Agent.speed = movementSpeed;
            Agent.acceleration = acceleration;
            Agent.stoppingDistance = attackRange - 0.5f;
            Agent.enabled = false;
        }
    
        protected virtual void OnEnable()
        {
            IsDead = false;
            IsEmerging = true;
            CanAttack = true;
    
            if (EnemyCollider != null)
                EnemyCollider.enabled = false;
        }
    
        protected virtual void OnDisable()
        {
            StopAllCoroutines();
            CancelInvoke();
        }
    
        protected virtual IEnumerator EmergeFromGround()
        {
            TargetPosition = transform.position;
            var spawnedParticleSystem = Instantiate(emergeEffect, TargetPosition, Quaternion.identity).GetComponent<ParticleSystem>();
            spawnedParticleSystem.Play();
    
            var startPosition = TargetPosition - Vector3.up * emergenceHeight;
            transform.position = startPosition;
    
            if (EnemyCollider != null)
                EnemyCollider.enabled = false;
    
            if (Animator != null)
            {
                Animator.SetTrigger(EmergeHash);
            }
    
            var elapsedTime = 0f;
            while (elapsedTime < emergenceTime)
            {
                var progress = elapsedTime / emergenceTime;
                if (progress >= 0.9)
                {
                    spawnedParticleSystem.Stop();
                }
            
                var smoothProgress = Mathf.SmoothStep(0f, 1f, progress);
                transform.position = Vector3.Lerp(startPosition, TargetPosition, smoothProgress);
                elapsedTime += Time.deltaTime;
                yield return null;
            }
    
            transform.position = TargetPosition;
            IsEmerging = false;
    
            if (EnemyCollider != null)
                EnemyCollider.enabled = true;
    
            if (Agent != null)
            {
                Agent.enabled = true;
                Agent.Warp(TargetPosition);
            }

            Destroy(spawnedParticleSystem.gameObject);
    
            OnEmergenceComplete();
        }
    
        protected virtual void OnEmergenceComplete()
        {
        
        }
    
        protected virtual void UpdateEnemyBehavior()
        {
            var distanceToPlayer = Vector3.Distance(transform.position, Player.position);
        
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
            if (Agent != null && Agent.enabled)
            {
                Agent.isStopped = true;
            }
        
            FacePlayer();
        
            if (CanAttack && Time.time - LastAttackTime >= attackCooldown)
            {
                Attack();
            }
        
            if (Animator != null)
            {
                Animator.SetBool(MoveHash, false);
            }
        }
    
        protected virtual void HandleChaseBehavior()
        {
            if (Agent != null && Agent.enabled)
            {
                Agent.isStopped = false;
                Agent.SetDestination(Player.position);
            }
        
            if (Animator != null)
            {
                Animator.SetBool(MoveHash, true);
            }
        }
    
        protected virtual void HandleIdleBehavior()
        {
            if (Agent != null && Agent.enabled)
            {
                Agent.isStopped = true;
            }
        
            if (Animator != null)
            {
                Animator.SetBool(MoveHash, false);
            }
        }
    
        protected virtual void FacePlayer()
        {
            var lookDirection = Player.position - transform.position;
            lookDirection.y = 0;
        
            if (lookDirection != Vector3.zero)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, 
                    Quaternion.LookRotation(lookDirection), Time.deltaTime * 5f);
            }
        }
    
        protected virtual void Attack()
        {
            LastAttackTime = Time.time;
    
            if (Animator != null)
            {
                Animator.SetTrigger(AttackHash);
            }
    
            if (attackSound != null && AudioSource != null)
            {
                AudioSource.PlayOneShot(attackSound);
            }
        
            var distanceToPlayer = Vector3.Distance(transform.position, Player.position);
            //Debug.Log($"Distance to player: {distanceToPlayer}, Attack range: {attackRange}");
    
            if (distanceToPlayer <= attackRange)
            {
                var playerHealth = Player.GetComponent<PlayerHealth>();
                if (playerHealth != null)
                {
                    //Debug.Log($"Dealing {damage} damage to player");
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
            if (IsDead || IsEmerging) return;
        
            if (EnemyHealth != null)
            {
                EnemyHealth.TakeDamage(damageAmount, hitPoint);
            }
            else
            {
                health -= damageAmount;
            
                if (health <= 0)
                {
                    Die();
                }
            }
        
            if (damageSound != null && AudioSource != null)
            {
                AudioSource.PlayOneShot(damageSound);
            }
        }
    
        protected virtual void OnHealthChanged(float currentHealth)
        {
            health = currentHealth;
        }
    
        protected virtual void OnHealthDepleted(EnemyHealth healthComponent)
        {
            Die();
        }
    
        protected virtual void Die()
        {
            if (IsDead) return;
            IsDead = true;
        
            if (deathSound != null && AudioSource != null)
            {
                AudioSource.PlayClipAtPoint(deathSound, transform.position);
            }
        
            if (EnemySpawnManager.Instance != null)
            {
                EnemySpawnManager.Instance.RemoveEnemy(this);
            }
        
            if (AIAssistant.Instance != null)
            {
                AIAssistant.Instance.OnEnemyKilled(this.GetType().Name);
            }
        
            var enemyDeath = GetComponent<EnemyDeath>();
            if (enemyDeath != null)
            {
                enemyDeath.InstantDeathWithParticles();
            }
            else
            {
                Destroy(gameObject);
            }
        
            QuestSystem.Instance.ReportEnemyKill(this.GetType().Name);
        
            OnDeath();
        }
    
        protected virtual void OnDeath()
        {
        
        }
    
        public virtual void OnAttackAnimationEvent()
        {
        
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
}