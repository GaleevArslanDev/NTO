using UnityEngine;

public class Billboard : MonoBehaviour
{
    private Camera mainCamera;
    
    void Start()
    {
        mainCamera = Camera.main;
        
        if (mainCamera == null)
        {
            mainCamera = FindObjectOfType<Camera>();
        }
    }
    
    void LateUpdate()
    {
        if (mainCamera != null)
        {
            transform.LookAt(transform.position + mainCamera.transform.rotation * Vector3.forward,
                mainCamera.transform.rotation * Vector3.up);
        }
    }
}