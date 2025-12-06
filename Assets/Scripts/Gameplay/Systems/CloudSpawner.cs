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
    [SerializeField] private Vector3 spawnAreaSize = new Vector3(100f, 30f, 100f);

    [Header("Cloud Settings")]
    [SerializeField] private Vector2 speedRange = new Vector2(3f, 8f);
    [SerializeField] private Vector2 scaleRange = new Vector2(0.8f, 1.5f);
    [SerializeField] private Vector2 rotationRange = new Vector2(-15f, 15f);

    [Header("Spacing Settings")]
    [SerializeField] private float minCloudDistance = 10f;
    [SerializeField] private int maxSpawnAttempts = 30;

    [Header("Hot Spawn Settings")]
    [SerializeField] private bool useHotSpawn = true; // Включить горячий спавн
    [SerializeField] private int initialCloudCount = 15; // Количество облаков при старте
    [SerializeField] private float hotSpawnMinDistance = 8f; // Минимальное расстояние при горячем спавне
    [SerializeField] private bool fillSpawnArea = true; // Равномерно заполнить всю зону

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

        // Горячий спавн при старте
        if (useHotSpawn)
        {
            PerformHotSpawn();
        }
        else
        {
            // Старый метод (постепенный спавн)
            int initialClouds = Mathf.Min(maxClouds / 2, 10);
            for (int i = 0; i < initialClouds; i++)
            {
                TrySpawnCloudWithSpacing(100);
            }
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

    // Горячий спавн - мгновенное создание облаков по всей зоне
    void PerformHotSpawn()
    {
        if (!fillSpawnArea)
        {
            // Простой случайный спавн с расстоянием
            for (int i = 0; i < initialCloudCount && activeClouds.Count < maxClouds; i++)
            {
                TrySpawnCloudWithSpacing(50, hotSpawnMinDistance);
            }
        }
        else
        {
            // Равномерное заполнение всей зоны спавна
            FillSpawnAreaUniformly();
        }
    }

    // Метод равномерного заполнения зоны спавна
    void FillSpawnAreaUniformly()
    {
        if (cloudPrefabs.Length == 0) return;

        // Рассчитываем оптимальное количество облаков для равномерного распределения
        float cellSize = Mathf.Max(minCloudDistance, hotSpawnMinDistance);
        int cellsX = Mathf.FloorToInt(spawnAreaSize.x / cellSize);
        int cellsY = Mathf.FloorToInt(spawnAreaSize.y / (cellSize * 0.7f)); // Меньше по вертикали
        int cellsZ = Mathf.FloorToInt(spawnAreaSize.z / cellSize);

        int totalCells = cellsX * cellsY * cellsZ;
        int cloudsToSpawn = Mathf.Min(initialCloudCount, totalCells, maxClouds);

        Debug.Log($"Hot spawn: {cloudsToSpawn} clouds in {cellsX}x{cellsY}x{cellsZ} grid");

        // Создаем список всех возможных позиций в сетке
        List<Vector3> gridPositions = new List<Vector3>();

        for (int x = 0; x < cellsX; x++)
        {
            for (int y = 0; y < cellsY; y++)
            {
                for (int z = 0; z < cellsZ; z++)
                {
                    // Центр ячейки
                    Vector3 cellCenter = new Vector3(
                        (x + 0.5f) * cellSize - spawnAreaSize.x / 2,
                        (y + 0.5f) * cellSize * 0.7f - spawnAreaSize.y / 2,
                        (z + 0.5f) * cellSize - spawnAreaSize.z / 2
                    );

                    gridPositions.Add(spawnZone.position + cellCenter);
                }
            }
        }

        // Перемешиваем позиции для случайного распределения
        ShuffleList(gridPositions);

        // Спавним облака в перемешанных позициях
        int spawnedCount = 0;
        foreach (Vector3 gridPos in gridPositions)
        {
            if (spawnedCount >= cloudsToSpawn) break;

            // Добавляем небольшой случайный сдвиг
            Vector3 jitter = new Vector3(
                Random.Range(-cellSize * 0.3f, cellSize * 0.3f),
                Random.Range(-cellSize * 0.15f, cellSize * 0.15f),
                Random.Range(-cellSize * 0.3f, cellSize * 0.3f)
            );

            Vector3 spawnPos = gridPos + jitter;

            // Проверяем, не выходит ли за границы
            if (IsInsideSpawnArea(spawnPos))
            {
                SpawnCloudAtPosition(spawnPos, true);
                spawnedCount++;
            }
        }

        // Если не удалось равномерно разместить, добавляем случайные
        while (spawnedCount < cloudsToSpawn && spawnedCount < maxClouds)
        {
            if (TrySpawnCloudWithSpacing(20, hotSpawnMinDistance * 0.8f))
            {
                spawnedCount++;
            }
            else
            {
                break;
            }
        }

        Debug.Log($"Hot spawn completed: {spawnedCount} clouds spawned");
    }

    // Метод для перемешивания списка
    void ShuffleList<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            T temp = list[i];
            int randomIndex = Random.Range(i, list.Count);
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }

    // Проверка, находится ли позиция внутри зоны спавна
    bool IsInsideSpawnArea(Vector3 position)
    {
        Vector3 localPos = position - spawnZone.position;

        return Mathf.Abs(localPos.x) <= spawnAreaSize.x / 2 &&
               Mathf.Abs(localPos.y) <= spawnAreaSize.y / 2 &&
               Mathf.Abs(localPos.z) <= spawnAreaSize.z / 2;
    }

    // Обновленный метод спавна с поддержкой горячего спавна
    bool TrySpawnCloudWithSpacing(int maxAttempts, float customMinDistance = -1)
    {
        if (cloudPrefabs.Length == 0)
        {
            Debug.LogWarning("No cloud prefabs assigned!");
            return false;
        }

        float distanceToCheck = customMinDistance > 0 ? customMinDistance : minCloudDistance;

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
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
                if (distance < distanceToCheck)
                {
                    tooClose = true;
                    break;
                }
            }

            // Если позиция подходящая, спавним облако
            if (!tooClose || attempt == maxAttempts - 1)
            {
                SpawnCloudAtPosition(spawnPosition, customMinDistance > 0);
                return true;
            }
        }

        return false;
    }

    void SpawnCloudAtPosition(Vector3 position, bool isHotSpawn = false)
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

        // Добавляем компонент для движения
        CloudMovement movement = cloud.AddComponent<CloudMovement>();

        // Для горячего спавна можно сделать случайное смещение по времени
        float speed = Random.Range(speedRange.x, speedRange.y);
        if (isHotSpawn)
        {
            // Слегка варьируем скорость для более натурального вида
            speed *= Random.Range(0.8f, 1.2f);
        }

        movement.SetMovementSpeed(speed);
        movement.SetMovementDirection(movementDirection);

        activeClouds.Add(cloud);
        cloud.transform.SetParent(transform);
    }

    // Улучшенный метод удаления облаков
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

            // Вектор от центра зоны к облаку
            Vector3 toCloud = cloud.transform.position - spawnZone.position;

            // Проекция на направление движения
            if (movementDirection != Vector3.zero)
            {
                float dot = Vector3.Dot(toCloud.normalized, movementDirection.normalized);
                float distance = toCloud.magnitude;

                // Разные пороги удаления в зависимости от направления
                float removeDistance = dot > 0.5f ?
                    Mathf.Max(spawnAreaSize.x, spawnAreaSize.z) * 2.5f :
                    Mathf.Max(spawnAreaSize.x, spawnAreaSize.z) * 1.8f;

                if (distance > removeDistance)
                {
                    cloudsToRemove.Add(cloud);
                }
            }
            else
            {
                // Если нет направления движения, используем сферическую проверку
                if (toCloud.magnitude > Mathf.Max(spawnAreaSize.x, spawnAreaSize.y, spawnAreaSize.z) * 2f)
                {
                    cloudsToRemove.Add(cloud);
                }
            }
        }

        // Удаляем помеченные облака
        foreach (GameObject cloud in cloudsToRemove)
        {
            if (cloud != null)
            {
                Destroy(cloud);
            }
            activeClouds.Remove(cloud);
        }
    }

    // Публичный метод для принудительного перезаполнения зоны
    public void RefillSpawnArea()
    {
        // Удаляем старые облака
        foreach (GameObject cloud in activeClouds)
        {
            if (cloud != null)
            {
                Destroy(cloud);
            }
        }
        activeClouds.Clear();

        // Заполняем заново
        PerformHotSpawn();
    }

    // Визуализация в редакторе
    void OnDrawGizmosSelected()
    {
        if (spawnZone == null) return;

        // Зона спавна
        Gizmos.color = new Color(0, 1, 0, 0.2f);
        Gizmos.DrawCube(spawnZone.position, spawnAreaSize);
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(spawnZone.position, spawnAreaSize);

        // Минимальное расстояние
        Gizmos.color = new Color(1, 0, 0, 0.1f);
        foreach (GameObject cloud in activeClouds)
        {
            if (cloud != null)
            {
                Gizmos.DrawSphere(cloud.transform.position, minCloudDistance / 2);
            }
        }

        // Направление движения
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(spawnZone.position, movementDirection.normalized * 20f);
        Gizmos.DrawSphere(spawnZone.position + movementDirection.normalized * 20f, 1f);
    }
}