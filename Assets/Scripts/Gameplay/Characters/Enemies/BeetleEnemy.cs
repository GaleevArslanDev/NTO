using System.Collections;
using Gameplay.Characters.Player;
using UnityEngine;

namespace Gameplay.Characters.Enemies
{
    public class BeetleEnemy : Enemy
    {
        // Beetle Specific Settings
        [Header("Beetle Specific Settings")]
        public float chargeDamageMultiplier = 1.5f;
        public float chargeSpeedMultiplier = 1.5f;
        public float chargeCooldown = 5f;
        public float chargeWindupTime = 0.5f;
    
        // State Variables
        private bool _isCharging;
        private float _lastChargeTime;
        private float _originalSpeed;
        private float _originalDamage;
    
        // Animator Hashes
        private readonly int _chargeHash = Animator.StringToHash("Charge");
    
        protected override void Awake()
        {
            base.Awake();
        
            _originalSpeed = movementSpeed;
            _originalDamage = damage;
        }
    
    
        protected override void UpdateEnemyBehavior()
        {
            var distanceToPlayer = Vector3.Distance(transform.position, Player.position);
        
            if (!_isCharging && !IsEmerging && !IsDead && 
                Time.time - _lastChargeTime >= chargeCooldown &&
                distanceToPlayer <= detectionRange && distanceToPlayer > attackRange)
            {
                StartCharge();
            }
        
            base.UpdateEnemyBehavior();
        }
    
        protected override void HandleAttackBehavior()
        {
            if (_isCharging)
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
            if (_isCharging)
            {
                if (Agent == null || !Agent.enabled) return;
                Agent.isStopped = false;
                Agent.SetDestination(Player.position);
            }
            else
            {
                base.HandleChaseBehavior();
            }
        }
    
        private void StartCharge()
        {
            if (_isCharging || IsEmerging || IsDead) return;
    
            _isCharging = true;
            _lastChargeTime = Time.time;
    
            if (Agent != null && Agent.enabled)
            {
                Agent.speed = _originalSpeed * chargeSpeedMultiplier;
                Agent.acceleration = acceleration * 2f;
            }
            damage = _originalDamage * chargeDamageMultiplier;
    
            if (Animator != null)
            {
                Animator.SetTrigger(_chargeHash);
                Animator.SetBool(MoveHash, true);
            }
    
            StartCoroutine(ChargeRoutine());
        }
    
        private IEnumerator ChargeRoutine()
        {
            const float chargeDuration = 3f;
            var elapsedTime = 0f;
    
            while (elapsedTime < chargeDuration && _isCharging && !IsDead)
            {
                if (Player != null && Agent != null && Agent.enabled)
                {
                    Agent.SetDestination(Player.position);
                }
                elapsedTime += Time.deltaTime;
                yield return null;
            }
    
            EndCharge();
        }
    
        private void EndCharge()
        {
            if (!_isCharging) return;
        
            _isCharging = false;
        
            if (Agent != null)
            {
                Agent.speed = _originalSpeed;
            }
            damage = _originalDamage;
        }
    
        private void HandleChargeAttack()
        {
            if (Animator != null)
            {
                Animator.SetBool(MoveHash, true);
            }
        
            FacePlayer();
        }
    
        public override void TakeDamage(float damageAmount, Vector3 hitPoint = default(Vector3))
        {
            base.TakeDamage(damageAmount, hitPoint);
        
            if (_isCharging)
            {
                EndCharge();
            }
        }
    
        protected override void OnDeath()
        {
            CancelInvoke();
        }
    
        public void OnChargeAttackAnimationEvent()
        {
            if (!_isCharging) return;

            if (!(Vector3.Distance(transform.position, Player.position) <= attackRange * 1.5f)) return;
            var playerHealth = Player.GetComponent<PlayerHealth>();
            if (playerHealth == null) return;
            playerHealth.TakeDamage(damage * 1.2f);
                
            var playerRb = Player.GetComponent<Rigidbody>();
            if (playerRb == null) return;
            var pushDirection = (Player.position - transform.position).normalized;
            playerRb.AddForce(pushDirection * 5f, ForceMode.Impulse);
        }
    }
}