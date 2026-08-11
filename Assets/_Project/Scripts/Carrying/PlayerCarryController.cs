using System;
using UnityEngine;

namespace Growveld.Carrying
{
    /// <summary>
    /// Owns the player's single large-object carry slot and handles dropping.
    /// </summary>
    public sealed class PlayerCarryController : MonoBehaviour
    {
        [SerializeField] private Transform carryAnchor;
        [SerializeField, Range(0f, 1f)] private float inheritedDropVelocity = 0.35f;

        private CharacterController characterController;
        private CarryableObject heldObject;

        public event Action<CarryableObject> HeldObjectChanged;

        public CarryableObject HeldObject => heldObject;

        public bool IsCarrying => heldObject != null;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
        }

        private void OnDisable()
        {
            if (Application.isPlaying && IsCarrying)
            {
                DropHeldObject();
            }
        }

        public bool CanCarry(CarryableObject candidate)
        {
            return candidate != null && !IsCarrying && !candidate.IsCarried && carryAnchor != null;
        }

        public bool TryPickUp(CarryableObject candidate)
        {
            if (!CanCarry(candidate))
            {
                return false;
            }

            Collider[] playerColliders = GetComponentsInChildren<Collider>(true);
            if (!candidate.BeginCarry(this, carryAnchor, playerColliders))
            {
                return false;
            }

            heldObject = candidate;
            HeldObjectChanged?.Invoke(heldObject);
            return true;
        }

        public void DropHeldObject()
        {
            if (heldObject == null)
            {
                return;
            }

            CarryableObject objectToDrop = heldObject;
            heldObject = null;

            Vector3 dropVelocity = characterController != null
                ? characterController.velocity * inheritedDropVelocity
                : Vector3.zero;

            objectToDrop.EndCarry(dropVelocity);
            HeldObjectChanged?.Invoke(null);
        }

        public CarryableObject ReleaseHeldObjectForTransfer()
        {
            if (heldObject == null)
            {
                return null;
            }

            CarryableObject transferredObject = heldObject;
            heldObject = null;
            transferredObject.EndCarry(Vector3.zero);
            HeldObjectChanged?.Invoke(null);
            return transferredObject;
        }

        internal void NotifyCarryableUnavailable(CarryableObject carryable)
        {
            if (heldObject != carryable)
            {
                return;
            }

            heldObject = null;
            HeldObjectChanged?.Invoke(null);
        }
    }
}
