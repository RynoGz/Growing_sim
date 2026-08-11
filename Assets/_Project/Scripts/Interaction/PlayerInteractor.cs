using Growveld.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Growveld.Interaction
{
    /// <summary>
    /// Finds an interactable object in the centre of the player's view and
    /// invokes it when the Interact input action is pressed.
    /// </summary>
    [RequireComponent(typeof(PlayerInput))]
    public sealed class PlayerInteractor : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Camera viewCamera;
        [SerializeField] private InteractionPromptUI promptUI;

        [Header("Raycast")]
        [SerializeField, Min(0.1f)] private float interactionDistance = 4.5f;
        [SerializeField] private LayerMask interactionLayers = ~0;

        private InputAction interactAction;
        private IInteractable currentInteractable;

        private void Awake()
        {
            if (viewCamera == null)
            {
                viewCamera = GetComponentInChildren<Camera>(true);
            }

            PlayerInput playerInput = GetComponent<PlayerInput>();
            interactAction = playerInput.actions.FindAction("Player/Interact", true);
        }

        private void Update()
        {
            FindInteractionTarget();

            if (currentInteractable != null && interactAction.WasPressedThisFrame())
            {
                currentInteractable.Interact(gameObject);
            }
        }

        private void OnDisable()
        {
            ClearInteractionTarget();
        }

        private void FindInteractionTarget()
        {
            if (viewCamera == null)
            {
                ClearInteractionTarget();
                return;
            }

            Ray interactionRay = new Ray(viewCamera.transform.position, viewCamera.transform.forward);
            bool hitSomething = Physics.Raycast(
                interactionRay,
                out RaycastHit hit,
                interactionDistance,
                interactionLayers,
                QueryTriggerInteraction.Ignore);

            if (!hitSomething)
            {
                ClearInteractionTarget();
                return;
            }

            IInteractable interactable = hit.collider.GetComponentInParent<IInteractable>();
            if (interactable == null || !interactable.CanInteract(gameObject))
            {
                ClearInteractionTarget();
                return;
            }

            currentInteractable = interactable;
            promptUI?.ShowPrompt($"[E] {interactable.InteractionPrompt}");
        }

        private void ClearInteractionTarget()
        {
            currentInteractable = null;
            promptUI?.HidePrompt();
        }

        private void OnDrawGizmosSelected()
        {
            Camera cameraToDraw = viewCamera != null ? viewCamera : GetComponentInChildren<Camera>();
            if (cameraToDraw == null)
            {
                return;
            }

            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(
                cameraToDraw.transform.position,
                cameraToDraw.transform.position + cameraToDraw.transform.forward * interactionDistance);
        }
    }
}
