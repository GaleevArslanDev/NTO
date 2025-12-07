using UnityEngine;

public class SimpleCameraRotator : MonoBehaviour
{
    [Header("Настройки вращения")]
    [SerializeField] private float rotationSpeed = 15f;
    [SerializeField] private Vector3 rotationPoint = Vector3.zero;
    [SerializeField] private float distanceFromCenter = 5f;
    [SerializeField] private float cameraHeight = 2f;

    private float angle = 0f;

    void Start()
    {
        // Начальная позиция камеры
        UpdateCameraPosition();
    }

    void Update()
    {
        // Увеличиваем угол
        angle += rotationSpeed * Time.deltaTime;

        // Обновляем позицию камеры
        UpdateCameraPosition();

        // Смотрим в центр
        transform.LookAt(rotationPoint);
    }

    void UpdateCameraPosition()
    {
        // Преобразуем угол в радианы
        float rad = angle * Mathf.Deg2Rad;

        // Вычисляем позицию по кругу
        float x = Mathf.Cos(rad) * distanceFromCenter;
        float z = Mathf.Sin(rad) * distanceFromCenter;

        // Устанавливаем позицию камеры
        transform.position = new Vector3(
            rotationPoint.x + x,
            rotationPoint.y + cameraHeight,
            rotationPoint.z + z
        );
    }

    // Для отладки в редакторе
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(rotationPoint, distanceFromCenter);
        Gizmos.DrawSphere(rotationPoint, 0.2f);
    }
}