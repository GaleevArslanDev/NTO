using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EnemySpawnManager : MonoBehaviour
{
    public static EnemySpawnManager Instance;
    
    [Header("Enemy Prefabs")]
    [SerializeField] private GameObject beetleEnemyPrefab;
    
    [Header("Spawn Settings")]
    [SerializeField] private float spawnRadius = 3f;
    [SerializeField] private int maxEnemiesPerSpawn = 3;
    [SerializeField] private float spawnHeight = 0.5f;
    
    private List<Enemy> activeEnemies = new List<Enemy>();
    
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
    
    public void TrySpawnEnemy(ItemData itemData, Vector3 spawnPosition)
    {
        if (itemData == null) return;
        
        // Проверяем шанс спавна врага для этого типа предмета
        if (Random.value <= itemData.enemySpawnChance)
        {
            SpawnBeetleEnemies(itemData, spawnPosition);
        }
    }
    
    private void SpawnBeetleEnemies(ItemData itemData, Vector3 spawnPosition)
    {
        // Определяем количество врагов для спавна (не больше максимального)
        int enemyCount = Random.Range(1, Mathf.Min(itemData.maxSpawnCount, maxEnemiesPerSpawn) + 1);
        
        for (int i = 0; i < enemyCount; i++)
        {
            StartCoroutine(SpawnBeetleWithDelay(spawnPosition, i * 0.3f)); // Задержка между спавнами
        }
    }
    
    private IEnumerator SpawnBeetleWithDelay(Vector3 spawnPosition, float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if (beetleEnemyPrefab == null)
        {
            Debug.LogWarning("Beetle enemy prefab is not assigned in EnemySpawnManager!");
            yield break;
        }
        
        // Вычисляем позицию спавна в радиусе от позиции предмета
        Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;
        Vector3 spawnPoint = spawnPosition + new Vector3(randomCircle.x, spawnHeight, randomCircle.y);
        
        // Проверяем, чтобы точка была на навмеше
        if (UnityEngine.AI.NavMesh.SamplePosition(spawnPoint, out UnityEngine.AI.NavMeshHit hit, spawnRadius, UnityEngine.AI.NavMesh.AllAreas))
        {
            spawnPoint = hit.position;
        }
        
        GameObject enemyObj = Instantiate(beetleEnemyPrefab, spawnPoint, Quaternion.identity);
        Enemy enemy = enemyObj.GetComponent<Enemy>();
        
        if (enemy != null)
        {
            activeEnemies.Add(enemy);
        }
        
        // Эффект появления (можно добавить частицы)
        Debug.Log($"Beetle spawned at {spawnPoint}");
    }
    
    public void RemoveEnemy(Enemy enemy)
    {
        if (activeEnemies.Contains(enemy))
        {
            activeEnemies.Remove(enemy);
        }
    }
    
    public int GetActiveEnemyCount()
    {
        return activeEnemies.Count;
    }
}