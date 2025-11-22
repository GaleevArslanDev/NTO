using UnityEngine;

public class EnemyDeath : MonoBehaviour
{
    [Header("Death Particles")]
    public GameObject deathParticlePrefab;
    public Vector3 particleOffset = new Vector3(0, 0.5f, 0);
    
    public void InstantDeathWithParticles()
    {
        // Спавним партиклы смерти ДО уничтожения врага
        if (deathParticlePrefab != null)
        {
            Vector3 spawnPosition = transform.position + particleOffset;
            GameObject deathParticles = Instantiate(deathParticlePrefab, spawnPosition, Quaternion.identity);
            
            // Настраиваем партиклы чтобы они жили самостоятельно
            SetupParticles(deathParticles);
        }
        
        // Немедленно уничтожаем врага
        Destroy(gameObject);
    }
    
    private void SetupParticles(GameObject particles)
    {
        // Убедимся что партиклы не уничтожатся сразу
        ParticleSystem particleSystem = particles.GetComponent<ParticleSystem>();
        if (particleSystem != null)
        {
            // Автоматически уничтожаем партиклы после их завершения
            Destroy(particles, particleSystem.main.duration + particleSystem.main.startLifetime.constantMax);
        }
        else
        {
            // Если нет ParticleSystem, уничтожаем через разумное время
            Destroy(particles, 3f);
        }
    }
}