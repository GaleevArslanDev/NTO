using UnityEngine;

public class CloudMovement : MonoBehaviour
{
    private float speed;
    private Vector3 direction;

    public void SetMovementSpeed(float newSpeed)
    {
        speed = newSpeed;
    }

    public void SetMovementDirection(Vector3 newDirection)
    {
        direction = newDirection.normalized;
    }

    void Update()
    {
        // Движение облака
        transform.Translate(direction * speed * Time.deltaTime, Space.World);
    }
}
