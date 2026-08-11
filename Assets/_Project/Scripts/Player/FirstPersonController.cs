using UnityEngine;
using UnityEngine.InputSystem;

namespace Growveld.Player
{
    /// <summary>
    /// Handles first-person walking, sprinting, gravity, and mouse look.
    /// Input comes from the Player action map on the attached PlayerInput component.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(PlayerInput))]
    public sealed class FirstPersonController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform cameraTransform;

        [Header("Movement")]
        [SerializeField, Min(0f)] private float walkSpeed = 4.5f;
        [SerializeField, Min(0f)] private float sprintSpeed = 7f;
        [SerializeField, Min(0f)] private float acceleration = 20f;
        [SerializeField] private float gravity = -20f;
        [SerializeField] private float groundedForce = -2f;

        [Header("Mouse Look")]
        [SerializeField, Min(0.001f)] private float mouseSensitivity = 0.08f;
        [SerializeField, Range(1f, 89f)] private float maximumLookAngle = 85f;

        private CharacterController characterController;
        private PlayerInput playerInput;
        private InputAction moveAction;
        private InputAction lookAction;
        private InputAction sprintAction;
        private Vector3 horizontalVelocity;
        private float verticalVelocity;
        private float cameraPitch;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            playerInput = GetComponent<PlayerInput>();

            if (cameraTransform == null)
            {
                Camera childCamera = GetComponentInChildren<Camera>(true);
                cameraTransform = childCamera != null ? childCamera.transform : null;
            }

            moveAction = playerInput.actions.FindAction("Player/Move", true);
            lookAction = playerInput.actions.FindAction("Player/Look", true);
            sprintAction = playerInput.actions.FindAction("Player/Sprint", true);
        }

        private void OnEnable()
        {
            LockCursor();
        }

        private void OnDisable()
        {
            UnlockCursor();
        }

        private void Update()
        {
            UpdateCursorState();

            if (Cursor.lockState == CursorLockMode.Locked)
            {
                ApplyMouseLook();
            }

            ApplyMovement();
        }

        private void ApplyMovement()
        {
            Vector2 movementInput = moveAction.ReadValue<Vector2>();
            Vector3 movementDirection = transform.right * movementInput.x
                + transform.forward * movementInput.y;

            if (movementDirection.sqrMagnitude > 1f)
            {
                movementDirection.Normalize();
            }

            bool isSprinting = sprintAction.IsPressed() && movementDirection.sqrMagnitude > 0.01f;
            float targetSpeed = isSprinting ? sprintSpeed : walkSpeed;
            Vector3 targetHorizontalVelocity = movementDirection * targetSpeed;

            horizontalVelocity = Vector3.MoveTowards(
                horizontalVelocity,
                targetHorizontalVelocity,
                acceleration * Time.deltaTime);

            if (characterController.isGrounded && verticalVelocity < 0f)
            {
                verticalVelocity = groundedForce;
            }

            verticalVelocity += gravity * Time.deltaTime;
            Vector3 finalVelocity = horizontalVelocity + Vector3.up * verticalVelocity;
            characterController.Move(finalVelocity * Time.deltaTime);
        }

        private void ApplyMouseLook()
        {
            Vector2 lookInput = lookAction.ReadValue<Vector2>();

            transform.Rotate(Vector3.up, lookInput.x * mouseSensitivity, Space.Self);

            cameraPitch -= lookInput.y * mouseSensitivity;
            cameraPitch = Mathf.Clamp(cameraPitch, -maximumLookAngle, maximumLookAngle);

            if (cameraTransform != null)
            {
                cameraTransform.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f);
            }
        }

        private void UpdateCursorState()
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                UnlockCursor();
            }

            if (Cursor.lockState != CursorLockMode.Locked
                && Mouse.current != null
                && Mouse.current.leftButton.wasPressedThisFrame)
            {
                LockCursor();
            }
        }

        private static void LockCursor()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private static void UnlockCursor()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}
