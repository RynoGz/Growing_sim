using System;
using UnityEngine;

namespace Growveld.Farming
{
    [Serializable]
    public sealed class DryingSlotState
    {
        [SerializeField] private Transform anchor;
        [SerializeField] private HarvestBatch batch;
        [SerializeField, Min(0f)] private float remainingSeconds;

        public Transform Anchor => anchor;
        public HarvestBatch Batch => batch;
        public float RemainingSeconds => remainingSeconds;
        public bool IsEmpty => batch == null;
        public bool IsReady => batch != null && batch.Status == HarvestStatus.Dried;

        public DryingSlotState(Transform anchor)
        {
            this.anchor = anchor;
        }

        public void StartDrying(HarvestBatch harvestBatch, float durationSeconds)
        {
            batch = harvestBatch;
            remainingSeconds = Mathf.Max(0f, durationSeconds);
        }

        public bool Advance(float elapsedSeconds)
        {
            if (batch == null || batch.Status == HarvestStatus.Dried) return false;
            remainingSeconds = Mathf.Max(0f, remainingSeconds - Mathf.Max(0f, elapsedSeconds));
            if (remainingSeconds > 0f) return false;
            batch.SetStatus(HarvestStatus.Dried);
            return true;
        }

        public HarvestBatch RemoveBatch()
        {
            HarvestBatch removed = batch;
            batch = null;
            remainingSeconds = 0f;
            return removed;
        }

        public void Restore(HarvestBatch restoredBatch, float restoredRemainingSeconds)
        {
            batch = restoredBatch;
            remainingSeconds = Mathf.Max(0f, restoredRemainingSeconds);
        }
    }
}
