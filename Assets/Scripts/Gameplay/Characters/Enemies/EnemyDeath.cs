using UnityEngine;

namespace Gameplay.Characters.Enemies
{
    public class EnemyDeath : MonoBehaviour
    {
        [Header("Death Particles")]
        public GameObject deathParticlePrefab;
        public Vector3 particleOffset = new Vector3(0, 0.5f, 0);
    
        public void InstantDeathWithParticles()
        {
            if (deathParticlePrefab != null)
            {
                var spawnPosition = transform.position + particleOffset;
                var deathParticles = Instantiate(deathParticlePrefab, spawnPosition, Quaternion.identity);
            
                SetupParticles(deathParticles);
            }
        
            Destroy(gameObject);
        }
    
        private void SetupParticles(GameObject particles)
        {
            var system = particles.GetComponent<ParticleSystem>();
            if (system != null)
            {
                Destroy(particles, system.main.duration + system.main.startLifetime.constantMax);
            }
            else
            {
                Destroy(particles, 3f);
            }
        }
    }
}