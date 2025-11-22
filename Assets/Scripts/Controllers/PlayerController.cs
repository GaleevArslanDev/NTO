using UnityEngine;

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

    private CharacterController characterController;
    private Vector3 velocity;
    private float xRotation = 0f;
    private bool isGrounded;

    private bool isCrouching = false;
    private float standingHeight;
    private Vector3 standingCameraPosition;
    private float currentSpeed;
    private TechTreeUI techTree;
    private QuestBoardUI questBoard;
    private TownHallUI townHall;
    public static PlayerController Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        characterController = GetComponent<CharacterController>();
        if (playerCamera == null)
            playerCamera = Camera.main?.transform;
    }
    
    public void SetControlEnabled(bool enabled)
    {
        this.enabled = enabled;
        if (characterController != null)
            characterController.enabled = enabled;
    }

    private void Start()
    {
        techTree = FindObjectOfType<TechTreeUI>();
        questBoard = FindObjectOfType<QuestBoardUI>();
        townHall = FindObjectOfType<TownHallUI>();
        Cursor.lockState = CursorLockMode.Locked;
        
        standingHeight = characterController.height;
        if (playerCamera != null)
            standingCameraPosition = playerCamera.localPosition;
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
        return (techTree != null && techTree.isUIOpen) ||
             (questBoard != null && questBoard.isUIOpen) ||
             (townHall != null && townHall.isUiOpen);
    }

    void HandleMovement()
    {
        isGrounded = characterController.isGrounded;
        if (isGrounded && velocity.y < 0) velocity.y = -2f;

        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        if (isCrouching)
            currentSpeed = crouchSpeed;
        else
            currentSpeed = Input.GetKey(KeyCode.LeftShift) ? runSpeed : walkSpeed;

        Vector3 move = transform.right * x + transform.forward * z;
        characterController.Move(move * currentSpeed * Time.deltaTime);

        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            if (isCrouching)
            {
                StopCrouch();
            }
            velocity.y = Mathf.Sqrt(jumpForce * -2f * gravity);
        }

        velocity.y += gravity * Time.deltaTime;
        characterController.Move(velocity * Time.deltaTime);
    }

    void HandleCamera()
    {
        if (playerCamera == null) return;
        
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -maxLookAngle, maxLookAngle);

        playerCamera.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX);
    }

    void HandleCrouch()
    {
        if (Input.GetKeyDown(KeyCode.LeftControl) || Input.GetKeyDown(KeyCode.C))
        {
            if (!isCrouching)
            {
                StartCrouch();
            }
            else
            {
                StopCrouch();
            }
        }

        if (isCrouching)
        {
            characterController.height = Mathf.Lerp(characterController.height, crouchHeight, crouchTransitionSpeed * Time.deltaTime);
            
            if (playerCamera != null)
            {
                Vector3 targetCameraPos = standingCameraPosition;
                targetCameraPos.y = crouchHeight - 0.5f;
                playerCamera.localPosition = Vector3.Lerp(playerCamera.localPosition, targetCameraPos, crouchTransitionSpeed * Time.deltaTime);
            }
        }
        else
        {
            characterController.height = Mathf.Lerp(characterController.height, standingHeight, crouchTransitionSpeed * Time.deltaTime);
            
            if (playerCamera != null)
            {
                playerCamera.localPosition = Vector3.Lerp(playerCamera.localPosition, standingCameraPosition, crouchTransitionSpeed * Time.deltaTime);
            }
        }
    }

    void StartCrouch()
    {
        isCrouching = true;
    }

    void StopCrouch()
    {
        if (!CanStandUp()) return;
        
        isCrouching = false;
    }

    bool CanStandUp()
    {
        float raycastDistance = standingHeight - crouchHeight + 0.1f;
        Vector3 raycastOrigin = transform.position + Vector3.up * crouchHeight;
        
        return !Physics.Raycast(raycastOrigin, Vector3.up, raycastDistance);
    }
}