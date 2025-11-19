using UnityEngine;

public class EnemyDeath : MonoBehaviour
{
    [Header("Death Particles")]
    public GameObject deathParticlePrefab;
    public Vector3 particleOffset = new Vector3(0, 0.5f, 0);
    
    public void InstantDeathWithParticles()
    {
        if (deathParticlePrefab != null)
        {
            Vector3 spawnPosition = transform.position + particleOffset;
            GameObject deathParticles = Instantiate(deathParticlePrefab, spawnPosition, Quaternion.identity);
            
            SetupParticles(deathParticles);
        }
        
        Destroy(gameObject);
    }
    
    private void SetupParticles(GameObject particles)
    {
        ParticleSystem particleSystem = particles.GetComponent<ParticleSystem>();
        if (particleSystem != null)
        {
            Destroy(particles, particleSystem.main.duration + particleSystem.main.startLifetime.constantMax);
        }
        else
        {
            Destroy(particles, 3f);
        }
    }
}