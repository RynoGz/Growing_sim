using System.Collections.Generic;
using UnityEngine;

namespace Growveld.Environment
{
    /// <summary>
    /// An automatically scheduled indoor grow light with circular horizontal coverage.
    /// </summary>
    public sealed class GrowLight : MonoBehaviour
    {
        private static readonly List<GrowLight> ActiveLights = new();

        [SerializeField, Min(0.1f)] private float coverageRadius = 3.5f;
        [SerializeField, Min(0f)] private float powerConsumptionKilowatts = 1.2f;
        [SerializeField] private Light lightSource;
        [SerializeField] private bool automaticSchedule = true;
        [SerializeField, Min(1f)] private float fallbackCycleRealSeconds = 1800f;
        [SerializeField, Min(0f)] private float fallbackActiveRealSeconds = 1200f;

        private GrowRoomEnvironment currentRoom;
        private bool externalScheduleEnabled;
        private bool externalScheduleActive;

        public float CoverageRadius => coverageRadius;
        public float PowerConsumptionKilowatts => powerConsumptionKilowatts;
        public GrowRoomEnvironment CurrentRoom => currentRoom;
        public bool IsActive { get; private set; }

        private void OnEnable()
        {
            if (!ActiveLights.Contains(this)) ActiveLights.Add(this);
            RefreshState();
        }

        private void OnDisable()
        {
            ActiveLights.Remove(this);
            IsActive = false;
            if (lightSource != null) lightSource.enabled = false;
        }

        private void Update()
        {
            Vector3 roomSamplePosition = lightSource != null
                ? lightSource.transform.position
                : transform.position + Vector3.up;
            currentRoom = GrowRoomEnvironment.FindContainingRoom(roomSamplePosition);
            RefreshState();
        }

        public bool Covers(Vector3 worldPosition, GrowRoomEnvironment requiredRoom)
        {
            if (!IsActive || requiredRoom == null || currentRoom != requiredRoom)
            {
                return false;
            }

            Vector3 lightPosition = lightSource != null ? lightSource.transform.position : transform.position;
            Vector2 lightHorizontal = new(lightPosition.x, lightPosition.z);
            Vector2 plantHorizontal = new(worldPosition.x, worldPosition.z);
            return Vector2.Distance(lightHorizontal, plantHorizontal) <= coverageRadius;
        }

        public void SetExternalSchedule(bool scheduledActive)
        {
            externalScheduleEnabled = true;
            externalScheduleActive = scheduledActive;
            RefreshState();
        }

        public void ClearExternalSchedule()
        {
            externalScheduleEnabled = false;
            RefreshState();
        }

        public static GrowLight FindCoveringLight(Vector3 worldPosition, GrowRoomEnvironment room)
        {
            foreach (GrowLight growLight in ActiveLights)
            {
                if (growLight != null && growLight.Covers(worldPosition, room)) return growLight;
            }
            return null;
        }

        public static IReadOnlyList<GrowLight> GetActiveLights()
        {
            return ActiveLights;
        }

        private void RefreshState()
        {
            bool scheduledActive;
            if (externalScheduleEnabled)
            {
                scheduledActive = externalScheduleActive;
            }
            else if (automaticSchedule)
            {
                float cyclePosition = Mathf.Repeat(Time.time, Mathf.Max(1f, fallbackCycleRealSeconds));
                scheduledActive = cyclePosition < Mathf.Clamp(fallbackActiveRealSeconds, 0f, fallbackCycleRealSeconds);
            }
            else
            {
                scheduledActive = true;
            }

            IsActive = isActiveAndEnabled && scheduledActive;
            if (lightSource != null) lightSource.enabled = IsActive;
        }
    }
}
