using Growveld.Carrying;
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
        private IContextualInfoProvider currentContextProvider;
        private PlayerCarryController carryController;

        public IInteractable CurrentInteractable => currentInteractable;
        public IContextualInfoProvider CurrentContextProvider => currentContextProvider;

        private void Awake()
        {
            if (viewCamera == null)
            {
                viewCamera = GetComponentInChildren<Camera>(true);
            }

            PlayerInput playerInput = GetComponent<PlayerInput>();
            interactAction = playerInput.actions.FindAction("Player/Interact", true);
            carryController = GetComponent<PlayerCarryController>();
        }

        private void Update()
        {
            FindInteractionTarget();

            if (!interactAction.WasPressedThisFrame())
            {
                return;
            }

            if (carryController != null && carryController.IsCarrying)
            {
                if (currentInteractable is IHeldObjectReceiver)
                {
                    currentInteractable.Interact(gameObject);
                }
                else
                {
                    carryController.DropHeldObject();
                }
            }
            else if (currentInteractable != null)
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
            RaycastHit[] hits = Physics.RaycastAll(
                interactionRay,
                interactionDistance,
                interactionLayers,
                QueryTriggerInteraction.Ignore);
            System.Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));

            RaycastHit? selectedHit = null;
            foreach (RaycastHit candidateHit in hits)
            {
                if (carryController != null
                    && carryController.HeldObject != null
                    && candidateHit.collider.transform.IsChildOf(carryController.HeldObject.transform))
                {
                    continue;
                }

                selectedHit = candidateHit;
                break;
            }

            if (!selectedHit.HasValue)
            {
                ClearInteractionTarget();
                return;
            }

            RaycastHit hit = selectedHit.Value;

            currentContextProvider = FindContextProvider(hit.collider);

            IInteractable interactable = FindFirstAvailableInteractable(hit.collider);
            if (interactable == null)
            {
                ClearInteractionTarget();
                return;
            }

            currentInteractable = interactable;
            promptUI?.ShowPrompt($"[E] {interactable.InteractionPrompt}");
        }

        private IInteractable FindFirstAvailableInteractable(Collider hitCollider)
        {
            MonoBehaviour[] behaviours = hitCollider.GetComponentsInParent<MonoBehaviour>(true);
            foreach (MonoBehaviour behaviour in behaviours)
            {
                if (behaviour is IInteractable interactable && interactable.CanInteract(gameObject))
                {
                    return interactable;
                }
            }

            return null;
        }

        private static IContextualInfoProvider FindContextProvider(Collider hitCollider)
        {
            MonoBehaviour[] behaviours = hitCollider.GetComponentsInParent<MonoBehaviour>(true);
            foreach (MonoBehaviour behaviour in behaviours)
            {
                if (behaviour is IContextualInfoProvider provider) return provider;
            }

            return null;
        }

        private void ClearInteractionTarget()
        {
            currentInteractable = null;
            currentContextProvider = null;
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
