using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Data.Game;
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
    
        private Dictionary<string, Enemy> _activeEnemies = new Dictionary<string, Enemy>();
        private List<string> _spawnedEnemyIds = new List<string>();

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

        public string GenerateEnemyId(Vector3 position, string enemyType)
        {
            return $"{enemyType}_{position.x:F2}_{position.y:F2}_{position.z:F2}_{System.Guid.NewGuid().ToString("N").Substring(0, 8)}";
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
        
            SpawnEnemyAtPosition(beetleEnemyPrefab, spawnPoint, "BeetleEnemy");
        }

        public Enemy SpawnEnemyAtPosition(GameObject enemyPrefab, Vector3 position, string enemyType)
        {
            var enemyObj = Instantiate(enemyPrefab, position, Quaternion.identity);
            var enemy = enemyObj.GetComponent<Enemy>();
        
            if (enemy != null)
            {
                var enemyId = GenerateEnemyId(position, enemyType);
                _activeEnemies[enemyId] = enemy;
                _spawnedEnemyIds.Add(enemyId);
                
                // Сохраняем ID в GameObject для последующего доступа
                var enemySaveComponent = enemyObj.GetComponent<EnemySaveComponent>();
                if (enemySaveComponent == null)
                {
                    enemySaveComponent = enemyObj.AddComponent<EnemySaveComponent>();
                }
                enemySaveComponent.SetEnemyId(enemyId);
            }
            
            return enemy;
        }
    
        public void RemoveEnemy(Enemy enemy)
        {
            var enemyId = FindEnemyId(enemy);
            if (!string.IsNullOrEmpty(enemyId))
            {
                _activeEnemies.Remove(enemyId);
                _spawnedEnemyIds.Remove(enemyId);
            }
        }

        private string FindEnemyId(Enemy enemy)
        {
            foreach (var kvp in _activeEnemies)
            {
                if (kvp.Value == enemy)
                {
                    return kvp.Key;
                }
            }
            return null;
        }
    
        public int GetActiveEnemyCount()
        {
            return _activeEnemies.Count;
        }

        // Методы для системы сохранения
        public EnemySpawnManagerSaveData GetSaveData()
        {
            return new EnemySpawnManagerSaveData
            {
                spawnedEnemyIds = new List<string>(_spawnedEnemyIds)
            };
        }

        public void ApplySaveData(EnemySpawnManagerSaveData saveData)
        {
            if (saveData == null) return;

            _spawnedEnemyIds = saveData.spawnedEnemyIds ?? new List<string>();
        }

        public List<EnemySaveData> GetAllEnemiesSaveData()
        {
            var enemiesSaveData = new List<EnemySaveData>();
            
            foreach (var kvp in _activeEnemies)
            {
                if (kvp.Value == null) continue;
                
                var enemy = kvp.Value;
                var enemyHealth = enemy.GetComponent<EnemyHealth>();
                
                var saveData = new EnemySaveData
                {
                    enemyId = kvp.Key,
                    enemyType = enemy.GetType().Name,
                    position = enemy.transform.position,
                    rotation = enemy.transform.eulerAngles,
                    health = enemyHealth?.currentHealth ?? enemy.health,
                    stateData = new EnemyStateSaveData
                    {
                        isDead = enemy.IsDead,
                        isEmerging = enemy.IsEmerging,
                        lastAttackTime = enemy.GetLastAttackTime(),
                        targetPosition = enemy.GetTargetPosition()
                    }
                };
                
                enemiesSaveData.Add(saveData);
            }
            
            return enemiesSaveData;
        }

        public void RestoreEnemyFromSave(EnemySaveData saveData)
        {
            if (saveData == null || string.IsNullOrEmpty(saveData.enemyId)) return;

            if (_activeEnemies.ContainsKey(saveData.enemyId))
            {
                Debug.LogWarning($"Enemy with ID {saveData.enemyId} already exists!");
                return;
            }

            GameObject enemyPrefab = null;

            switch (saveData.enemyType)
            {
                case "BeetleEnemy":
                    enemyPrefab = beetleEnemyPrefab;
                    break;
            }

            if (enemyPrefab == null)
            {
                Debug.LogError($"No prefab found for enemy type: {saveData.enemyType}");
                return;
            }

            // Создаем врага
            var enemyObj = Instantiate(enemyPrefab, saveData.position, Quaternion.Euler(saveData.rotation));
            var enemy = enemyObj.GetComponent<Enemy>();
            var enemyHealth = enemyObj.GetComponent<EnemyHealth>();

            if (enemy != null)
            {
                // СНАЧАЛА настраиваем EnemyHealth
                if (enemyHealth != null)
                {
                    enemyHealth.SetInitialHealth(saveData.health, saveData.stateData.isDead);
                }

                // ПОТОМ восстанавливаем состояние Enemy
                enemy.SetInitialState(saveData.health, saveData.stateData);

                // Регистрируем в менеджере
                _activeEnemies[saveData.enemyId] = enemy;

                // Устанавливаем ID
                var enemySaveComponent = enemyObj.GetComponent<EnemySaveComponent>();
                if (enemySaveComponent == null)
                {
                    enemySaveComponent = enemyObj.AddComponent<EnemySaveComponent>();
                }
                enemySaveComponent.SetEnemyId(saveData.enemyId);

                Debug.Log($"Restored enemy: {saveData.enemyType} at {saveData.position}, health: {saveData.health}, isEmerging: {saveData.stateData.isEmerging}");
            }
        }

        public void ClearAllEnemies()
        {
            foreach (var enemy in _activeEnemies.Values)
            {
                if (enemy != null)
                {
                    Destroy(enemy.gameObject);
                }
            }
            _activeEnemies.Clear();
            _spawnedEnemyIds.Clear();
        }
    }
}