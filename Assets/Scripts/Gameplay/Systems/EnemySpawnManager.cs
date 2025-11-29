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

        [Header("Enemy Prefabs")] [SerializeField]
        private GameObject beetleEnemyPrefab;

        [Header("Spawn Settings")] [SerializeField]
        private float spawnRadius = 3f;

        [SerializeField] private int maxEnemiesPerSpawn = 3;
        [SerializeField] private float spawnHeight = 0.5f;

        private Dictionary<string, Enemy> _activeEnemies = new Dictionary<string, Enemy>();
        private List<string> _spawnedEnemyIds = new List<string>();
        private bool _isRestoringEnemies = false;

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
        
        public void RestoreAllEnemies(List<EnemySaveData> enemiesData)
        {
            if (!gameObject.activeInHierarchy || !enabled)
            {
                Debug.LogWarning("EnemySpawnManager is not active or enabled, attempting to enable...");
                gameObject.SetActive(true);
                enabled = true;
        
                // Даем один кадр на активацию
                StartCoroutine(DelayedRestoration(enemiesData));
                return;
            }

            PerformEnemyRestoration(enemiesData);
        }
        
        private IEnumerator DelayedRestoration(List<EnemySaveData> enemiesData)
        {
            yield return null; // Ждем один кадр
            PerformEnemyRestoration(enemiesData);
        }

        private void PerformEnemyRestoration(List<EnemySaveData> enemiesData)
        {
            if (enemiesData == null || enemiesData.Count == 0)
            {
                Debug.Log("No enemy data to restore");
                return;
            }

            _isRestoringEnemies = true;

            try
            {
                Debug.Log($"Starting enemy restoration for {enemiesData.Count} enemies");

                // Создаем временный словарь для быстрого поиска
                var saveDataDict = enemiesData.ToDictionary(data => data.enemyId, data => data);

                // Списки для управления
                var enemiesToRemove = new List<string>();
                var enemiesToUpdate = new List<string>();
                var enemiesToCreate = new List<EnemySaveData>();

                // Анализируем текущее состояние
                foreach (var existingEnemy in _activeEnemies)
                {
                    if (saveDataDict.ContainsKey(existingEnemy.Key))
                    {
                        enemiesToUpdate.Add(existingEnemy.Key);
                    }
                    else
                    {
                        enemiesToRemove.Add(existingEnemy.Key);
                    }
                }

                // Находим врагов, которых нужно создать
                foreach (var enemyData in enemiesData)
                {
                    if (!_activeEnemies.ContainsKey(enemyData.enemyId) && !enemyData.stateData.isDead)
                    {
                        enemiesToCreate.Add(enemyData);
                    }
                }

                // Удаляем лишних врагов
                foreach (var enemyId in enemiesToRemove)
                {
                    if (_activeEnemies.TryGetValue(enemyId, out var enemy) && enemy != null)
                    {
                        Destroy(enemy.gameObject);
                    }

                    _activeEnemies.Remove(enemyId);
                    _spawnedEnemyIds.Remove(enemyId);
                }

                // Обновляем существующих врагов
                foreach (var enemyId in enemiesToUpdate)
                {
                    if (_activeEnemies.TryGetValue(enemyId, out var enemy) && enemy != null &&
                        saveDataDict.TryGetValue(enemyId, out var enemyData))
                    {
                        var enemyHealth = enemy.GetComponent<EnemyHealth>();
                        if (enemyHealth != null)
                        {
                            enemyHealth.SetInitialHealth(enemyData.health, enemyData.stateData.isDead);
                        }

                        enemy.SetInitialState(enemyData.health, enemyData.stateData, enemyData.position);
                    }
                }

                // Создаем новых врагов
                foreach (var enemyData in enemiesToCreate)
                {
                    StartCoroutine(RestoreEnemyWithDelay(enemyData, Random.Range(0f, 0.3f)));
                }

                Debug.Log(
                    $"Enemy restoration complete: {enemiesToUpdate.Count} updated, {enemiesToCreate.Count} created, {enemiesToRemove.Count} removed");

                Debug.Log($"Enemy restoration complete");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error during enemy restoration: {e.Message}");
            }
            finally
            {
                _isRestoringEnemies = false;
            }
        }

        private IEnumerator RestoreEnemyWithDelay(EnemySaveData enemyData, float delay)
        {
            yield return new WaitForSeconds(delay);
            RestoreEnemyFromSave(enemyData);
        }

        public string GenerateEnemyId(Vector3 position, string enemyType)
        {
            return
                $"{enemyType}_{position.x:F2}_{position.y:F2}_{position.z:F2}_{System.Guid.NewGuid().ToString("N").Substring(0, 8)}";
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

            if (UnityEngine.AI.NavMesh.SamplePosition(spawnPoint, out var hit, spawnRadius,
                    UnityEngine.AI.NavMesh.AllAreas))
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
                if (kvp.Value == null) 
                {
                    Debug.LogWarning($"Found null enemy in active enemies, removing: {kvp.Key}");
                    continue;
                }

                try
                {
                    var enemy = kvp.Value;
                    var enemyHealth = enemy.GetComponent<EnemyHealth>();

                    // Проверяем, что враг валиден для сохранения
                    if (enemyHealth == null)
                    {
                        Debug.LogWarning($"Enemy {kvp.Key} has no EnemyHealth component, skipping");
                        continue;
                    }

                    var saveData = new EnemySaveData
                    {
                        enemyId = kvp.Key,
                        enemyType = enemy.GetType().Name,
                        position = enemy.transform.position,
                        rotation = enemy.transform.eulerAngles,
                        health = enemyHealth.currentHealth,
                        stateData = new EnemyStateSaveData
                        {
                            isDead = enemy.IsDead,
                            isEmerging = enemy.IsEmerging,
                            lastAttackTime = enemy.GetLastAttackTime(),
                            targetPosition = enemy.GetTargetPosition()
                        }
                    };

                    enemiesSaveData.Add(saveData);
            
                    Debug.Log($"Saved enemy: {saveData.enemyId} at {saveData.position}, health: {saveData.health}");
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Error saving enemy {kvp.Key}: {e.Message}");
                }
            }

            Debug.Log($"Total enemies saved: {enemiesSaveData.Count}");
            return enemiesSaveData;
        }

        public void RestoreEnemyFromSave(EnemySaveData saveData)
        {
            if (saveData == null || string.IsNullOrEmpty(saveData.enemyId))
            {
                Debug.LogError("Invalid enemy save data");
                return;
            }

            // Проверяем, не восстанавливаем ли мы уже этого врага
            if (_activeEnemies.ContainsKey(saveData.enemyId))
            {
                Debug.LogWarning($"Enemy with ID {saveData.enemyId} already exists, skipping creation");
                return;
            }

            GameObject enemyPrefab = null;

            // Определяем префаб по типу врага
            switch (saveData.enemyType)
            {
                case "BeetleEnemy":
                    enemyPrefab = beetleEnemyPrefab;
                    break;
                // Добавьте другие типы врагов по необходимости
                default:
                    Debug.LogError($"Unknown enemy type: {saveData.enemyType}");
                    return;
            }

            if (enemyPrefab == null)
            {
                Debug.LogError($"No prefab found for enemy type: {saveData.enemyType}");
                return;
            }

            try
            {
                // Создаем врага с правильной позицией и поворотом
                var enemyObj = Instantiate(enemyPrefab, saveData.position, Quaternion.Euler(saveData.rotation));
                var enemy = enemyObj.GetComponent<Enemy>();
                var enemyHealth = enemyObj.GetComponent<EnemyHealth>();

                if (enemy != null && enemyHealth != null)
                {
                    // СНАЧАЛА настраиваем EnemyHealth
                    enemyHealth.SetInitialHealth(saveData.health, saveData.stateData.isDead);

                    // ПОТОМ восстанавливаем состояние Enemy
                    enemy.SetInitialState(saveData.health, saveData.stateData, saveData.position);

                    // Регистрируем в менеджере
                    _activeEnemies[saveData.enemyId] = enemy;
                    _spawnedEnemyIds.Add(saveData.enemyId);

                    // Устанавливаем ID
                    var enemySaveComponent = enemyObj.GetComponent<EnemySaveComponent>();
                    if (enemySaveComponent == null)
                    {
                        enemySaveComponent = enemyObj.AddComponent<EnemySaveComponent>();
                    }

                    enemySaveComponent.SetEnemyId(saveData.enemyId);

                    Debug.Log($"Successfully restored enemy: {saveData.enemyType} at {saveData.position}, " +
                              $"health: {saveData.health}, isEmerging: {saveData.stateData.isEmerging}, isDead: {saveData.stateData.isDead}");
                }
                else
                {
                    Debug.LogError($"Failed to get required components from {saveData.enemyType}");
                    Destroy(enemyObj);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error creating enemy from save data: {e.Message}");
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

        public bool IsRestoringEnemies => _isRestoringEnemies;
    }
}