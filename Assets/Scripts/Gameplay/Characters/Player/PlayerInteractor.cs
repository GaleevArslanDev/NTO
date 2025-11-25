using Gameplay.Characters.NPC;
using UnityEngine;
using static UnityEngine.Screen;

namespace Gameplay.Characters.Player
{
    public class PlayerInteractor : MonoBehaviour
    {
        [Header("Interaction Settings")]
        [SerializeField] private float interactionDistance = 3f;
        [SerializeField] private LayerMask npcLayerMask;
        [SerializeField] private KeyCode interactionKey = KeyCode.E;
    
        [Header("UI Indicators")]
        [SerializeField] private GameObject interactionPrompt;
    
        private Camera _mainCamera;
        private NpcInteraction _currentNpc;

        private void Start()
        {
            _mainCamera = Camera.main;
            if (interactionPrompt != null)
                interactionPrompt.SetActive(false);
        }

        private void Update()
        {
            CheckForNpc();
            HandleInteractionInput();
        }
    
        private void CheckForNpc()
        {
            var ray = _mainCamera.ScreenPointToRay(new Vector3(width / 2.0f, height / 2.0f, 0));

            NpcInteraction newNpc = null;
        
            if (Physics.Raycast(ray, out var hit, interactionDistance, npcLayerMask))
            {
                newNpc = hit.collider.GetComponent<NpcInteraction>();
            }

            if (newNpc == _currentNpc) return;
            if (_currentNpc != null)
            {
                HideInteractionPrompt();
            }
            
            _currentNpc = newNpc;
            
            if (_currentNpc != null)
            {
                ShowInteractionPrompt();
            }
        }
    
        private void HandleInteractionInput()
        {
            if (_currentNpc != null && Input.GetKeyDown(interactionKey))
            {
                _currentNpc.StartDialogueWithPlayer();
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
            if (_mainCamera == null) return;
            Gizmos.color = Color.blue;
            var rayStart = _mainCamera.transform.position;
            var rayDirection = _mainCamera.transform.forward * interactionDistance;
            Gizmos.DrawRay(rayStart, rayDirection);
        }
    }
}