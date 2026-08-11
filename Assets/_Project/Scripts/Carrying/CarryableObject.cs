using Growveld.Interaction;
using UnityEngine;

namespace Growveld.Carrying
{
    /// <summary>
    /// Makes a Rigidbody object pick-up-able through the shared interaction system.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public sealed class CarryableObject : MonoBehaviour, IInteractable
    {
        [SerializeField] private string displayName = "large object";
        [SerializeField] private Rigidbody rigidbodyComponent;

        private Collider[] objectColliders;
        private Collider[] ignoredPlayerColliders;
        private Transform originalParent;
        private Transform activeCarryAnchor;
        private PlayerCarryController currentCarrier;
        private bool originalUseGravity;
        private bool originalIsKinematic;
        private RigidbodyInterpolation originalInterpolation;

        public string InteractionPrompt => $"Pick up {displayName}";
        public string DisplayName => displayName;

        public bool IsCarried => currentCarrier != null;

        private void Awake()
        {
            if (rigidbodyComponent == null)
            {
                rigidbodyComponent = GetComponent<Rigidbody>();
            }

            objectColliders = GetComponentsInChildren<Collider>(true);
        }

        private void OnDisable()
        {
            if (Application.isPlaying && currentCarrier != null)
            {
                currentCarrier.NotifyCarryableUnavailable(this);
                currentCarrier = null;
            }
        }

        private void LateUpdate()
        {
            if (currentCarrier == null || activeCarryAnchor == null)
            {
                return;
            }

            transform.SetPositionAndRotation(activeCarryAnchor.position, activeCarryAnchor.rotation);
        }

        public bool CanInteract(GameObject interactor)
        {
            return !IsCarried
                && interactor.TryGetComponent(out PlayerCarryController carryController)
                && carryController.CanCarry(this);
        }

        public void Interact(GameObject interactor)
        {
            if (interactor.TryGetComponent(out PlayerCarryController carryController))
            {
                carryController.TryPickUp(this);
            }
        }

        internal bool BeginCarry(
            PlayerCarryController carrier,
            Transform carryAnchor,
            Collider[] playerColliders)
        {
            if (carrier == null || carryAnchor == null || IsCarried)
            {
                return false;
            }

            if (rigidbodyComponent == null)
            {
                rigidbodyComponent = GetComponent<Rigidbody>();
            }

            if (objectColliders == null || objectColliders.Length == 0)
            {
                objectColliders = GetComponentsInChildren<Collider>(true);
            }

            currentCarrier = carrier;
            activeCarryAnchor = carryAnchor;
            originalParent = transform.parent;
            originalUseGravity = rigidbodyComponent.useGravity;
            originalIsKinematic = rigidbodyComponent.isKinematic;
            originalInterpolation = rigidbodyComponent.interpolation;

            rigidbodyComponent.linearVelocity = Vector3.zero;
            rigidbodyComponent.angularVelocity = Vector3.zero;
            rigidbodyComponent.interpolation = RigidbodyInterpolation.None;
            rigidbodyComponent.useGravity = false;
            rigidbodyComponent.isKinematic = true;

            ignoredPlayerColliders = playerColliders;
            SetPlayerCollisionIgnored(true);

            transform.SetParent(carryAnchor, false);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            Physics.SyncTransforms();
            return true;
        }

        internal void EndCarry(Vector3 inheritedVelocity)
        {
            if (!IsCarried)
            {
                return;
            }

            transform.SetParent(originalParent, true);
            SetPlayerCollisionIgnored(false);

            rigidbodyComponent.isKinematic = originalIsKinematic;
            rigidbodyComponent.useGravity = originalUseGravity;
            rigidbodyComponent.interpolation = originalInterpolation;

            if (!rigidbodyComponent.isKinematic)
            {
                rigidbodyComponent.linearVelocity = inheritedVelocity;
                rigidbodyComponent.angularVelocity = Vector3.zero;
            }

            currentCarrier = null;
            activeCarryAnchor = null;
            ignoredPlayerColliders = null;
        }

        private void SetPlayerCollisionIgnored(bool shouldIgnore)
        {
            if (objectColliders == null || ignoredPlayerColliders == null)
            {
                return;
            }

            foreach (Collider objectCollider in objectColliders)
            {
                if (objectCollider == null)
                {
                    continue;
                }

                foreach (Collider playerCollider in ignoredPlayerColliders)
                {
                    if (playerCollider != null)
                    {
                        Physics.IgnoreCollision(objectCollider, playerCollider, shouldIgnore);
                    }
                }
            }
        }
    }
}
