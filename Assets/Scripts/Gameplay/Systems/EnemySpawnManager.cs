using System.Collections;
using System.Collections.Generic;
using Data.Inventory;
using Gameplay.Characters.Enemies;
using UnityEngine;

namespace Gameplay.Systems
{
    public class EnemySpawnManager : MonoBehaviour
    {
        public static EnemySpawnManager Instance;
    
        [Header("Enemy Prefabs")]
        [SerializeField] private GameObject beetleEnemyPrefab;
    
        [Header("Spawn Settings")]
        [SerializeField] private float spawnRadius = 3f;
        [SerializeField] private int maxEnemiesPerSpawn = 3;
        [SerializeField] private float spawnHeight = 0.5f;
    
        private List<Enemy> _activeEnemies = new();

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
        }
    
        public void TrySpawnEnemy(ItemData itemData, Vector3 spawnPosition)
        {
            if (itemData == null) return;
        
            if (Random.value <= itemData.enemySpawnChance)
            {
                SpawnBeetleEnemies(itemData, spawnPosition);
            }
        }
    
        private void SpawnBeetleEnemies(ItemData itemData, Vector3 spawnPosition)
        {
            var enemyCount = Random.Range(1, Mathf.Min(itemData.maxSpawnCount, maxEnemiesPerSpawn) + 1);
        
            for (var i = 0; i < enemyCount; i++)
            {
                StartCoroutine(SpawnBeetleWithDelay(spawnPosition, i * 0.3f));
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
        
            var randomCircle = Random.insideUnitCircle * spawnRadius;
            var spawnPoint = spawnPosition + new Vector3(randomCircle.x, spawnHeight, randomCircle.y);
        
            if (UnityEngine.AI.NavMesh.SamplePosition(spawnPoint, out var hit, spawnRadius, UnityEngine.AI.NavMesh.AllAreas))
            {
                spawnPoint = hit.position;
            }
        
            var enemyObj = Instantiate(beetleEnemyPrefab, spawnPoint, Quaternion.identity);
            var enemy = enemyObj.GetComponent<Enemy>();
        
            if (enemy != null)
            {
                _activeEnemies.Add(enemy);
            }
        }
    
        public void RemoveEnemy(Enemy enemy)
        {
            if (_activeEnemies.Contains(enemy))
            {
                _activeEnemies.Remove(enemy);
            }
        }
    
        public int GetActiveEnemyCount()
        {
            return _activeEnemies.Count;
        }
    }
}