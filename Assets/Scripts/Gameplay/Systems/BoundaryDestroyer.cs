using UnityEngine;

public class BoundaryDestroyer : MonoBehaviour
{
    [Header("Boundary Settings")]
    [SerializeField] private float boundaryDistance = 100f; // Расстояние от центра, на котором происходит удаление
    [SerializeField] private bool useWorldCenter = true; // Использовать центр мира (0,0,0) или позицию объекта
    [SerializeField] private Vector3 centerPoint = Vector3.zero;

    void Start()
    {
        if (!useWorldCenter)
        {
            centerPoint = transform.position;
        }
    }

    void Update()
    {
        // Проверяем расстояние от центра
        float distance = Vector3.Distance(transform.position, centerPoint);

        // Если объект слишком далеко, уничтожаем его
        if (distance > boundaryDistance)
        {
            Destroy(gameObject);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(centerPoint, boundaryDistance);
    }
}