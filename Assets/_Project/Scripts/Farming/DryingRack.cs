using System.Collections.Generic;
using System.Text;
using Growveld.Carrying;
using Growveld.Interaction;
using UnityEngine;

namespace Growveld.Farming
{
    /// <summary>
    /// Accepts physical Fresh batches and advances each occupied drying slot independently.
    /// </summary>
    public sealed class DryingRack : MonoBehaviour, IHeldObjectReceiver, IContextualInfoProvider
    {
        [SerializeField, Min(1f)] private float dryingDurationSeconds = 600f;
        [SerializeField] private Transform[] slotAnchors;
        [SerializeField] private Transform outputPoint;
        [SerializeField] private List<DryingSlotState> slots = new();

        public IReadOnlyList<DryingSlotState> Slots => slots;
        public float DryingDurationSeconds => dryingDurationSeconds;
        public int Capacity => slots.Count;
        public int OccupiedCount
        {
            get
            {
                int count = 0;
                foreach (DryingSlotState slot in slots) if (!slot.IsEmpty) count++;
                return count;
            }
        }
        public string InteractionPrompt => HasFreshCarriedBatch(out _)
            ? "Place Fresh batch on drying rack"
            : HasReadyBatch()
                ? "Remove Dried batch"
                : "Drying rack";
        public string ContextualInfo
        {
            get
            {
                StringBuilder builder = new($"Drying Rack: {OccupiedCount}/{Capacity}\n");
                for (int index = 0; index < slots.Count; index++)
                {
                    DryingSlotState slot = slots[index];
                    string state = slot.IsEmpty
                        ? "Empty"
                        : slot.IsReady
                            ? "Dried - Ready"
                            : $"Fresh - {slot.RemainingSeconds / 60f:0.0} min";
                    builder.AppendLine($"Slot {index + 1}: {state}");
                }
                return builder.ToString();
            }
        }

        private void Awake()
        {
            EnsureSlots();
        }

        private void Update()
        {
            foreach (DryingSlotState slot in slots)
            {
                slot.Advance(Time.deltaTime);
            }
        }

        public bool CanInteract(GameObject interactor)
        {
            return (HasFreshCarriedBatch(out _) && FindEmptySlot() != null)
                || (!IsPlayerCarrying(interactor) && HasReadyBatch());
        }

        public void Interact(GameObject interactor)
        {
            if (HasFreshCarriedBatch(out HarvestBatch carriedBatch))
            {
                TryAcceptBatch(interactor, carriedBatch);
            }
            else if (!IsPlayerCarrying(interactor))
            {
                ReleaseFirstReadyBatch();
            }
        }

        public bool TryAcceptBatch(GameObject interactor, HarvestBatch batch)
        {
            DryingSlotState emptySlot = FindEmptySlot();
            if (emptySlot == null || batch == null || batch.Status != HarvestStatus.Fresh
                || !interactor.TryGetComponent(out PlayerCarryController carryController))
            {
                return false;
            }

            CarryableObject released = carryController.ReleaseHeldObjectForTransfer();
            if (released == null || released.gameObject != batch.gameObject)
            {
                return false;
            }

            DockBatch(batch, emptySlot.Anchor);
            emptySlot.StartDrying(batch, dryingDurationSeconds);
            return true;
        }

        public HarvestBatch ReleaseFirstReadyBatch()
        {
            foreach (DryingSlotState slot in slots)
            {
                if (!slot.IsReady) continue;
                HarvestBatch batch = slot.RemoveBatch();
                UndockBatch(batch);
                return batch;
            }
            return null;
        }

        public bool RestoreBatchAtSlot(int slotIndex, HarvestBatch batch, float remainingSeconds)
        {
            EnsureSlots();
            if (batch == null || slotIndex < 0 || slotIndex >= slots.Count || !slots[slotIndex].IsEmpty)
            {
                return false;
            }

            DockBatch(batch, slots[slotIndex].Anchor);
            slots[slotIndex].Restore(batch, remainingSeconds);
            return true;
        }

        private void DockBatch(HarvestBatch batch, Transform anchor)
        {
            Rigidbody body = batch.GetComponent<Rigidbody>();
            if (body != null)
            {
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
                body.useGravity = false;
                body.isKinematic = true;
                body.interpolation = RigidbodyInterpolation.None;
            }
            batch.transform.SetParent(anchor, false);
            batch.transform.localPosition = Vector3.zero;
            batch.transform.localRotation = Quaternion.identity;
        }

        private void UndockBatch(HarvestBatch batch)
        {
            if (batch == null) return;
            batch.transform.SetParent(null, true);
            batch.transform.position = outputPoint != null
                ? outputPoint.position
                : transform.position + transform.forward * -1.5f + Vector3.up * 0.5f;
            Rigidbody body = batch.GetComponent<Rigidbody>();
            if (body != null)
            {
                body.isKinematic = false;
                body.useGravity = true;
                body.interpolation = RigidbodyInterpolation.Interpolate;
                body.linearVelocity = Vector3.zero;
            }
        }

        private bool HasFreshCarriedBatch(out HarvestBatch batch)
        {
            PlayerCarryController carryController = FindFirstObjectByType<PlayerCarryController>();
            batch = carryController != null && carryController.HeldObject != null
                ? carryController.HeldObject.GetComponent<HarvestBatch>()
                : null;
            return batch != null && batch.Status == HarvestStatus.Fresh;
        }

        private static bool IsPlayerCarrying(GameObject interactor)
        {
            return interactor.TryGetComponent(out PlayerCarryController controller) && controller.IsCarrying;
        }

        private DryingSlotState FindEmptySlot()
        {
            foreach (DryingSlotState slot in slots) if (slot.IsEmpty) return slot;
            return null;
        }

        private bool HasReadyBatch()
        {
            foreach (DryingSlotState slot in slots) if (slot.IsReady) return true;
            return false;
        }

        private void EnsureSlots()
        {
            if (slots.Count == (slotAnchors?.Length ?? 0)) return;
            slots.Clear();
            if (slotAnchors == null) return;
            foreach (Transform anchor in slotAnchors) slots.Add(new DryingSlotState(anchor));
        }
    }
}
