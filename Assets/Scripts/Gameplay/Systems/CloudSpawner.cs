using UnityEngine;
using System.Collections.Generic;

public class CloudSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private GameObject[] cloudPrefabs;
    [SerializeField] private float spawnRate = 2f;
    [SerializeField] private int maxClouds = 20;

    [Header("Spawn Area")]
    [SerializeField] private Transform spawnZone;
    [SerializeField] private Vector3 spawnAreaSize = new Vector3(100f, 30f, 100f); // Увеличьте размер области

    [Header("Cloud Settings")]
    [SerializeField] private Vector2 speedRange = new Vector2(3f, 8f);
    [SerializeField] private Vector2 scaleRange = new Vector2(0.8f, 1.5f);
    [SerializeField] private Vector2 rotationRange = new Vector2(-15f, 15f);

    [Header("Spacing Settings")]
    [SerializeField] private float minCloudDistance = 10f; // Минимальное расстояние между облаками
    [SerializeField] private int maxSpawnAttempts = 30; // Максимальное количество попыток спавна

    [Header("Movement Direction")]
    [SerializeField] private Vector3 movementDirection = Vector3.forward;

    private List<GameObject> activeClouds = new List<GameObject>();
    private float spawnTimer = 0f;
    private Bounds spawnBounds;

    void Start()
    {
        if (spawnZone == null)
        {
            spawnZone = transform;
        }

        spawnBounds = new Bounds(spawnZone.position, spawnAreaSize);

        // Спавним с проверкой расстояния
        int initialClouds = Mathf.Min(maxClouds / 2, 10);
        for (int i = 0; i < initialClouds; i++)
        {
            TrySpawnCloudWithSpacing(100); // Больше попыток для начального спавна
        }
    }

    void Update()
    {
        spawnTimer += Time.deltaTime;
        if (spawnTimer >= spawnRate && activeClouds.Count < maxClouds)
        {
            if (TrySpawnCloudWithSpacing(maxSpawnAttempts))
            {
                spawnTimer = 0f;
            }
        }

        RemoveDistantClouds();
    }

    bool TrySpawnCloudWithSpacing(int maxAttempts)
    {
        if (cloudPrefabs.Length == 0)
        {
            Debug.LogWarning("No cloud prefabs assigned!");
            return false;
        }

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            // Генерируем случайную позицию
            Vector3 randomPosition = new Vector3(
                Random.Range(-spawnAreaSize.x / 2, spawnAreaSize.x / 2),
                Random.Range(-spawnAreaSize.y / 2, spawnAreaSize.y / 2),
                Random.Range(-spawnAreaSize.z / 2, spawnAreaSize.z / 2)
            );

            Vector3 spawnPosition = spawnZone.position + randomPosition;

            // Проверяем расстояние до других облаков
            bool tooClose = false;
            foreach (GameObject cloud in activeClouds)
            {
                if (cloud == null) continue;

                float distance = Vector3.Distance(spawnPosition, cloud.transform.position);
                if (distance < minCloudDistance)
                {
                    tooClose = true;
                    break;
                }
            }

            // Если позиция подходящая, спавним облако
            if (!tooClose || attempt == maxAttempts - 1)
            {
                SpawnCloudAtPosition(spawnPosition);
                return true;
            }
        }

        return false;
    }

    void SpawnCloudAtPosition(Vector3 position)
    {
        GameObject cloudPrefab = cloudPrefabs[Random.Range(0, cloudPrefabs.Length)];
        GameObject cloud = Instantiate(cloudPrefab, position, Quaternion.identity);

        // Применяем случайный поворот
        Vector3 randomRotation = new Vector3(
            Random.Range(rotationRange.x, rotationRange.y),
            Random.Range(rotationRange.x, rotationRange.y),
            Random.Range(rotationRange.x, rotationRange.y)
        );
        cloud.transform.rotation = Quaternion.Euler(randomRotation);

        // Применяем случайный размер
        float randomScale = Random.Range(scaleRange.x, scaleRange.y);
        cloud.transform.localScale = Vector3.one * randomScale;

        // Учитываем масштаб при проверке расстояния (опционально)
        float effectiveMinDistance = minCloudDistance * randomScale;

        // Добавляем компонент для движения
        CloudMovement movement = cloud.AddComponent<CloudMovement>();
        movement.SetMovementSpeed(Random.Range(speedRange.x, speedRange.y));
        movement.SetMovementDirection(movementDirection);

        activeClouds.Add(cloud);
        cloud.transform.SetParent(transform);
    }

    // Альтернативный метод: спавн в секторах
    void SpawnCloudInSector()
    {
        if (cloudPrefabs.Length == 0 || activeClouds.Count >= maxClouds)
            return;

        // Делим пространство на сектора
        int sectorsX = Mathf.CeilToInt(spawnAreaSize.x / minCloudDistance);
        int sectorsY = Mathf.CeilToInt(spawnAreaSize.y / (minCloudDistance * 0.5f));
        int sectorsZ = Mathf.CeilToInt(spawnAreaSize.z / minCloudDistance);

        // Создаем сетку возможных позиций
        List<Vector3> possiblePositions = new List<Vector3>();

        for (int x = 0; x < sectorsX; x++)
        {
            for (int y = 0; y < sectorsY; y++)
            {
                for (int z = 0; z < sectorsZ; z++)
                {
                    Vector3 sectorCenter = new Vector3(
                        (x + 0.5f) * minCloudDistance - spawnAreaSize.x / 2,
                        (y + 0.5f) * minCloudDistance * 0.5f - spawnAreaSize.y / 2,
                        (z + 0.5f) * minCloudDistance - spawnAreaSize.z / 2
                    );

                    // Добавляем небольшой рандом в пределах сектора
                    Vector3 jitter = new Vector3(
                        Random.Range(-minCloudDistance * 0.3f, minCloudDistance * 0.3f),
                        Random.Range(-minCloudDistance * 0.15f, minCloudDistance * 0.15f),
                        Random.Range(-minCloudDistance * 0.3f, minCloudDistance * 0.3f)
                    );

                    possiblePositions.Add(spawnZone.position + sectorCenter + jitter);
                }
            }
        }

        // Перемешиваем позиции
        for (int i = 0; i < possiblePositions.Count; i++)
        {
            Vector3 temp = possiblePositions[i];
            int randomIndex = Random.Range(i, possiblePositions.Count);
            possiblePositions[i] = possiblePositions[randomIndex];
            possiblePositions[randomIndex] = temp;
        }

        // Пытаемся спавнить в свободных позициях
        foreach (Vector3 pos in possiblePositions)
        {
            if (activeClouds.Count >= maxClouds)
                break;

            bool positionValid = true;
            foreach (GameObject cloud in activeClouds)
            {
                if (cloud == null) continue;

                if (Vector3.Distance(pos, cloud.transform.position) < minCloudDistance)
                {
                    positionValid = false;
                    break;
                }
            }

            if (positionValid)
            {
                SpawnCloudAtPosition(pos);
                break;
            }
        }
    }

    void RemoveDistantClouds()
    {
        List<GameObject> cloudsToRemove = new List<GameObject>();

        foreach (GameObject cloud in activeClouds)
        {
            if (cloud == null)
            {
                cloudsToRemove.Add(cloud);
                continue;
            }

            // Учитываем направление движения при проверке дистанции
            Vector3 checkPoint = spawnZone.position;
            if (movementDirection != Vector3.zero)
            {
                // Проекция позиции облака на плоскость, перпендикулярную направлению
                Vector3 toCloud = cloud.transform.position - spawnZone.position;
                float dot = Vector3.Dot(toCloud.normalized, movementDirection.normalized);

                // Если облако движется в правильном направлении, даем ему больше времени
                if (dot > 0.7f)
                {
                    float distance = Vector3.Distance(cloud.transform.position, spawnZone.position);
                    if (distance > Mathf.Max(spawnAreaSize.x, spawnAreaSize.y, spawnAreaSize.z) * 3f)
                    {
                        cloudsToRemove.Add(cloud);
                    }
                    continue;
                }
            }

            float distanceSimple = Vector3.Distance(cloud.transform.position, spawnZone.position);
            if (distanceSimple > Mathf.Max(spawnAreaSize.x, spawnAreaSize.y, spawnAreaSize.z) * 2f)
            {
                cloudsToRemove.Add(cloud);
            }
        }

        foreach (GameObject cloud in cloudsToRemove)
        {
            if (cloud != null)
            {
                Destroy(cloud);
            }
            activeClouds.Remove(cloud);
        }
    }

    void OnDrawGizmosSelected()
    {
        if (spawnZone == null) return;

        Gizmos.color = new Color(0, 1, 0, 0.2f);
        Gizmos.DrawCube(spawnZone.position, spawnAreaSize);
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(spawnZone.position, spawnAreaSize);

        // Показываем минимальное расстояние
        Gizmos.color = new Color(1, 0, 0, 0.1f);
        foreach (GameObject cloud in activeClouds)
        {
            if (cloud != null)
            {
                Gizmos.DrawSphere(cloud.transform.position, minCloudDistance / 2);
            }
        }
    }
}