using System.Collections.Generic;
using UnityEngine;

namespace Growveld.Automation
{
    /// <summary>Shared operational, coverage and electricity state for placeable automation.</summary>
    public abstract class AutomationEquipment : MonoBehaviour
    {
        private static readonly List<AutomationEquipment> ActiveEquipment = new();

        [SerializeField] private bool operational = true;
        [SerializeField, Min(0f)] private float coverageRadius = 6f;
        [SerializeField, Min(0f)] private float powerConsumptionKilowatts = 0.15f;

        public bool IsOperational => operational;
        public float CoverageRadius => coverageRadius;
        public float PowerConsumptionKilowatts => powerConsumptionKilowatts;
        public bool IsDrawingPower { get; protected set; }
        public virtual float StoredResource => 0f;

        protected virtual void OnEnable()
        {
            if (!ActiveEquipment.Contains(this)) ActiveEquipment.Add(this);
        }

        protected virtual void OnDisable()
        {
            ActiveEquipment.Remove(this);
            IsDrawingPower = false;
        }

        protected void ToggleOperational()
        {
            operational = !operational;
            if (!operational) IsDrawingPower = false;
        }

        public bool Covers(Vector3 worldPosition)
        {
            Vector2 delta = new(worldPosition.x - transform.position.x, worldPosition.z - transform.position.z);
            return delta.sqrMagnitude <= coverageRadius * coverageRadius;
        }

        public virtual void RestoreAutomation(bool restoredOperational, float storedResource)
        {
            operational = restoredOperational;
            IsDrawingPower = false;
        }

        public static IReadOnlyList<AutomationEquipment> GetActiveEquipment() => ActiveEquipment;
    }
}
