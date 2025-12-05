using UnityEngine;
using System.Collections.Generic;

public class CloudSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private GameObject[] cloudPrefabs; // Префабы облаков
    [SerializeField] private float spawnRate = 2f; // Интервал спавна в секундах
    [SerializeField] private int maxClouds = 20; // Максимальное количество облаков одновременно

    [Header("Spawn Area")]
    [SerializeField] private Transform spawnZone; // Зона спавна (пустой GameObject)
    [SerializeField] private Vector3 spawnAreaSize = new Vector3(50f, 20f, 50f); // Размер зоны спавна

    [Header("Cloud Settings")]
    [SerializeField] private Vector2 speedRange = new Vector2(3f, 8f); // Минимальная и максимальная скорость
    [SerializeField] private Vector2 scaleRange = new Vector2(0.8f, 1.5f); // Минимальный и максимальный размер
    [SerializeField] private Vector2 rotationRange = new Vector2(-15f, 15f); // Минимальный и максимальный поворот

    [Header("Movement Direction")]
    [SerializeField] private Vector3 movementDirection = Vector3.forward; // Направление движения

    private List<GameObject> activeClouds = new List<GameObject>();
    private float spawnTimer = 0f;
    private Bounds spawnBounds;

    void Start()
    {
        if (spawnZone == null)
        {
            spawnZone = transform;
        }

        // Создаем Bounds для зоны спавна
        spawnBounds = new Bounds(spawnZone.position, spawnAreaSize);

        // Предварительно спавним несколько облаков
        for (int i = 0; i < maxClouds / 2; i++)
        {
            SpawnCloud();
        }
    }

    void Update()
    {
        // Таймер для спавна
        spawnTimer += Time.deltaTime;
        if (spawnTimer >= spawnRate && activeClouds.Count < maxClouds)
        {
            SpawnCloud();
            spawnTimer = 0f;
        }

        // Удаляем облака, которые вышли за пределы
        RemoveDistantClouds();
    }

    void SpawnCloud()
    {
        if (cloudPrefabs.Length == 0)
        {
            Debug.LogWarning("No cloud prefabs assigned!");
            return;
        }

        // Выбираем случайный префаб облака
        GameObject cloudPrefab = cloudPrefabs[Random.Range(0, cloudPrefabs.Length)];

        // Случайная позиция в зоне спавна
        Vector3 randomPosition = new Vector3(
            Random.Range(-spawnAreaSize.x / 2, spawnAreaSize.x / 2),
            Random.Range(-spawnAreaSize.y / 2, spawnAreaSize.y / 2),
            Random.Range(-spawnAreaSize.z / 2, spawnAreaSize.z / 2)
        );

        Vector3 spawnPosition = spawnZone.position + randomPosition;

        // Создаем облако
        GameObject cloud = Instantiate(cloudPrefab, spawnPosition, Quaternion.identity);

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
        movement.SetMovementSpeed(Random.Range(speedRange.x, speedRange.y));
        movement.SetMovementDirection(movementDirection);

        // Добавляем в список активных облаков
        activeClouds.Add(cloud);

        // Устанавливаем родителя для порядка в иерархии
        cloud.transform.SetParent(transform);
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

            // Рассчитываем расстояние от центра спавна
            float distance = Vector3.Distance(cloud.transform.position, spawnZone.position);

            // Если облако слишком далеко, помечаем на удаление
            if (distance > Mathf.Max(spawnAreaSize.x, spawnAreaSize.y, spawnAreaSize.z) * 2f)
            {
                cloudsToRemove.Add(cloud);
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

    // Визуализация зоны спавна в редакторе
    void OnDrawGizmosSelected()
    {
        if (spawnZone == null) return;

        Gizmos.color = new Color(0, 1, 0, 0.3f);
        Gizmos.DrawCube(spawnZone.position, spawnAreaSize);
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(spawnZone.position, spawnAreaSize);
    }
}