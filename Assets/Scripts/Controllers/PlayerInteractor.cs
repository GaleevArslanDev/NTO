using UnityEngine;

public class PlayerInteractor : MonoBehaviour
{
    [Header("Interaction Settings")]
    [SerializeField] private float interactionDistance = 3f;
    [SerializeField] private LayerMask npcLayerMask;
    [SerializeField] private KeyCode interactionKey = KeyCode.E;
    
    [Header("UI Indicators")]
    [SerializeField] private GameObject interactionPrompt;
    
    private Camera mainCamera;
    private NPCInteraction currentNPC;
    
    void Start()
    {
        mainCamera = Camera.main;
        if (interactionPrompt != null)
            interactionPrompt.SetActive(false);
    }
    
    void Update()
    {
        CheckForNPC();
        HandleInteractionInput();
    }
    
    private void CheckForNPC()
    {
        Ray ray = mainCamera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
        RaycastHit hit;
        
        NPCInteraction newNPC = null;
        
        if (Physics.Raycast(ray, out hit, interactionDistance, npcLayerMask))
        {
            newNPC = hit.collider.GetComponent<NPCInteraction>();
        }
        
        if (newNPC != currentNPC)
        {
            if (currentNPC != null)
            {
                HideInteractionPrompt();
            }
            
            currentNPC = newNPC;
            
            if (currentNPC != null)
            {
                ShowInteractionPrompt();
            }
        }
    }
    
    private void HandleInteractionInput()
    {
        if (currentNPC != null && Input.GetKeyDown(interactionKey))
        {
            currentNPC.StartDialogueWithPlayer();
        }
    }
    
    private void ShowInteractionPrompt()
    {
        if (interactionPrompt != null)
        {
            interactionPrompt.SetActive(true);
        }
    }
    
    private void HideInteractionPrompt()
    {
        if (interactionPrompt != null)
        {
            interactionPrompt.SetActive(false);
        }
    }
    
    private void OnDrawGizmos()
    {
        if (mainCamera != null)
        {
            Gizmos.color = Color.blue;
            Vector3 rayStart = mainCamera.transform.position;
            Vector3 rayDirection = mainCamera.transform.forward * interactionDistance;
            Gizmos.DrawRay(rayStart, rayDirection);
        }
    }
}