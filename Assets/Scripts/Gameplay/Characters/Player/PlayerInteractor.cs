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
        private ReactiveDialogueTrigger _currentReactiveNpc;

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
            var ray = _mainCamera.ScreenPointToRay(new Vector3(Screen.width / 2.0f, Screen.height / 2.0f, 0));

            NpcInteraction newNpc = null;
            ReactiveDialogueTrigger reactiveNpc = null;
    
            if (Physics.Raycast(ray, out var hit, interactionDistance, npcLayerMask))
            {
                newNpc = hit.collider.GetComponent<NpcInteraction>();
                reactiveNpc = hit.collider.GetComponent<ReactiveDialogueTrigger>();
            }

            if (newNpc == _currentNpc && reactiveNpc == _currentReactiveNpc) return;
            if (_currentNpc != null)
            {
                HideInteractionPrompt();
            }
        
            _currentNpc = newNpc;
            _currentReactiveNpc = reactiveNpc;
        
            if (_currentNpc != null)
            {
                ShowInteractionPrompt();
                UpdateInteractionPrompt();
            }
        }
        
        private void UpdateInteractionPrompt()
        {
            if (_currentNpc == null || interactionPrompt == null) return;
    
            var textMesh = interactionPrompt.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            if (textMesh == null) return;
    
            var npcName = _currentNpc.GetNpcName();
    
            if (_currentReactiveNpc != null && _currentReactiveNpc.IsCalling)
            {
                textMesh.text = $"{npcName} (зовет)\nE - Ответить";
            }
            else
            {
                // Диалог недоступен, показываем только услуги
                textMesh.text = $"{npcName}\nF - Услуги";
            }
        }
    
        private void HandleInteractionInput()
        {
            if (_currentNpc == null || !Input.GetKeyDown(interactionKey)) return;
            
            // Только реактивный диалог
            if (_currentReactiveNpc != null && _currentReactiveNpc.IsCalling)
            {
                _currentReactiveNpc.TriggerDialogue();
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