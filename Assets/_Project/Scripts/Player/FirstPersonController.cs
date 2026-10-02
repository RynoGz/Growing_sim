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
        private PlayerInputStateController inputState;
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
            inputState = GetComponent<PlayerInputStateController>();
            if (inputState == null) inputState = gameObject.AddComponent<PlayerInputStateController>();

            if (cameraTransform == null)
            {
                Camera childCamera = GetComponentInChildren<Camera>(true);
                cameraTransform = childCamera != null ? childCamera.transform : null;
            }

            moveAction = playerInput.actions.FindAction("Player/Move", true);
            lookAction = playerInput.actions.FindAction("Player/Look", true);
            sprintAction = playerInput.actions.FindAction("Player/Sprint", true);
        }

        private void Update()
        {
            if (inputState != null && !inputState.AllowsMovementAndLook) return;

            // Read both actions every frame and apply them independently. Neither path is
            // conditional on the other action, sprint state, or incidental cursor changes.
            Vector2 lookInput = lookAction.ReadValue<Vector2>();
            Vector2 movementInput = moveAction.ReadValue<Vector2>();
            ApplyMouseLook(lookInput);
            ApplyMovement(movementInput, sprintAction.IsPressed(), Time.deltaTime);
        }

        private void ApplyMovement(Vector2 movementInput, bool sprintPressed, float deltaTime)
        {
            Vector3 movementDirection = transform.right * movementInput.x
                + transform.forward * movementInput.y;

            if (movementDirection.sqrMagnitude > 1f)
            {
                movementDirection.Normalize();
            }

            bool isSprinting = sprintPressed && movementDirection.sqrMagnitude > 0.01f;
            float targetSpeed = isSprinting ? sprintSpeed : walkSpeed;
            Vector3 targetHorizontalVelocity = movementDirection * targetSpeed;

            horizontalVelocity = Vector3.MoveTowards(
                horizontalVelocity,
                targetHorizontalVelocity,
                acceleration * Mathf.Max(0f, deltaTime));

            if (characterController.isGrounded && verticalVelocity < 0f)
            {
                verticalVelocity = groundedForce;
            }

            verticalVelocity += gravity * Mathf.Max(0f, deltaTime);
            Vector3 finalVelocity = horizontalVelocity + Vector3.up * verticalVelocity;
            characterController.Move(finalVelocity * Mathf.Max(0f, deltaTime));
        }

        private void ApplyMouseLook(Vector2 lookInput)
        {
            transform.Rotate(Vector3.up, lookInput.x * mouseSensitivity, Space.Self);

            cameraPitch -= lookInput.y * mouseSensitivity;
            cameraPitch = Mathf.Clamp(cameraPitch, -maximumLookAngle, maximumLookAngle);

            if (cameraTransform != null)
            {
                cameraTransform.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f);
            }
        }

    }
}
