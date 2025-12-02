using Data.Game;
using Gameplay.Systems;
using UI;
using UnityEngine;

namespace Gameplay.Characters.Player
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement Settings")]
        public float walkSpeed = 10f;
        public float runSpeed = 20f;
        public float jumpForce = 3f;
        public float gravity = -3.72f;

        [Header("Camera Settings")]
        public float mouseSensitivity = 300f;
        public Transform playerCamera;
        public float maxLookAngle = 90f;

        [Header("Crouch Settings")]
        public float crouchHeight = 1f;
        public float crouchSpeed = 3f;
        public float crouchTransitionSpeed = 5f;

        private CharacterController _characterController;
        private Vector3 _velocity;
        private float _xRotation;
        private bool _isGrounded;

        private bool _isCrouching;
        private float _standingHeight;
        private Vector3 _standingCameraPosition;
        private float _currentSpeed; 
        private float _currentMouseSensitivity;
        public static PlayerController Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            _characterController = GetComponent<CharacterController>();
            if (playerCamera == null)
                playerCamera = Camera.main?.transform;
        }
    
        public void SetControlEnabled(bool value)
        {
            enabled = value;
            if (_characterController != null)
                _characterController.enabled = value;
        }

        private void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;
            
            if (GameSettings.Instance != null)
            {
                _currentMouseSensitivity = GameSettings.Instance.MouseSensitivity;
                GameSettings.Instance.OnSettingsChanged += OnSettingsChanged;
            }
        
            _standingHeight = _characterController.height;
            if (playerCamera != null)
                _standingCameraPosition = playerCamera.localPosition;
        }
        
        private void OnSettingsChanged(SettingsSaveData settings)
        {
            _currentMouseSensitivity = settings.mouseSensitivity;
        }

        private void Update()
        {
            if (IsAnyUIOpen()) return;
        
            HandleMovement();
            HandleCamera();
            HandleCrouch();
        }

        private bool IsAnyUIOpen()
        {
            return UIManager.Instance != null && UIManager.Instance.IsAnyUIOpen();
        }

        private void HandleMovement()
        {
            _isGrounded = _characterController.isGrounded;
            if (_isGrounded && _velocity.y < 0) _velocity.y = -2f;

            var x = Input.GetAxis("Horizontal");
            var z = Input.GetAxis("Vertical");

            if (_isCrouching)
                _currentSpeed = crouchSpeed;
            else
                _currentSpeed = Input.GetKey(KeyCode.LeftShift) ? runSpeed : walkSpeed;

            var move = transform.right * x + transform.forward * z;
            _characterController.Move(move * (_currentSpeed * Time.deltaTime));

            if (Input.GetButtonDown("Jump") && _isGrounded)
            {
                if (_isCrouching)
                {
                    StopCrouch();
                }
                _velocity.y = Mathf.Sqrt(jumpForce * -2f * gravity);
            }

            _velocity.y += gravity * Time.deltaTime;
            _characterController.Move(_velocity * Time.deltaTime);
        }

        private void HandleCamera()
        {
            if (playerCamera == null) return;
        
            var mouseX = Input.GetAxis("Mouse X") * _currentMouseSensitivity * Time.deltaTime;
            var mouseY = Input.GetAxis("Mouse Y") * _currentMouseSensitivity * Time.deltaTime;
            
            if (GameSettings.Instance != null && GameSettings.Instance.InvertMouseY)
            {
                mouseY = -mouseY;
            }

            _xRotation -= mouseY;
            _xRotation = Mathf.Clamp(_xRotation, -maxLookAngle, maxLookAngle);

            playerCamera.localRotation = Quaternion.Euler(_xRotation, 0f, 0f);
            transform.Rotate(Vector3.up * mouseX);
        }

        private void HandleCrouch()
        {
            if (Input.GetKeyDown(KeyCode.LeftControl) || Input.GetKeyDown(KeyCode.C))
            {
                if (!_isCrouching)
                {
                    StartCrouch();
                }
                else
                {
                    StopCrouch();
                }
            }

            if (_isCrouching)
            {
                _characterController.height = Mathf.Lerp(_characterController.height, crouchHeight, crouchTransitionSpeed * Time.deltaTime);

                if (playerCamera == null) return;
                var targetCameraPos = _standingCameraPosition;
                targetCameraPos.y = crouchHeight - 0.5f;
                playerCamera.localPosition = Vector3.Lerp(playerCamera.localPosition, targetCameraPos, crouchTransitionSpeed * Time.deltaTime);
            }
            else
            {
                _characterController.height = Mathf.Lerp(_characterController.height, _standingHeight, crouchTransitionSpeed * Time.deltaTime);
            
                if (playerCamera != null)
                {
                    playerCamera.localPosition = Vector3.Lerp(playerCamera.localPosition, _standingCameraPosition, crouchTransitionSpeed * Time.deltaTime);
                }
            }
        }

        private void StartCrouch()
        {
            _isCrouching = true;
        }

        private void StopCrouch()
        {
            if (!CanStandUp()) return;
        
            _isCrouching = false;
        }

        private bool CanStandUp()
        {
            var raycastDistance = _standingHeight - crouchHeight + 0.1f;
            var raycastOrigin = transform.position + Vector3.up * crouchHeight;
        
            return !Physics.Raycast(raycastOrigin, Vector3.up, raycastDistance);
        }
        
        private void OnDestroy()
        {
            if (GameSettings.Instance != null)
            {
                GameSettings.Instance.OnSettingsChanged -= OnSettingsChanged;
            }
        }
    }
}