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

        [SerializeField, Min(0.1f)] private float coverageRadius = 6f;
        [SerializeField, Min(0f)] private float powerConsumptionKilowatts = 1.2f;
        [SerializeField] private Light lightSource;
        [SerializeField, Min(0f)] private float visualIntensity = 300f;
        [SerializeField, Min(0.1f)] private float visualRange = 8f;
        [SerializeField] private Color visualColor = new(0.72f, 0.82f, 1f, 1f);
        [SerializeField] private LightType visualLightType = LightType.Point;
        [SerializeField, Range(1f, 179f)] private float spotAngle = 110f;
        [SerializeField] private bool automaticSchedule = true;
        [SerializeField, Min(1f)] private float fallbackCycleRealSeconds = 1800f;
        [SerializeField, Min(0f)] private float fallbackActiveRealSeconds = 1200f;

        private GrowRoomEnvironment currentRoom;
        private bool externalScheduleEnabled;
        private bool externalScheduleActive;

        public float CoverageRadius => coverageRadius;
        public float PowerConsumptionKilowatts => powerConsumptionKilowatts;
        public float VisualIntensity => visualIntensity;
        public float VisualRange => visualRange;
        public Color VisualColor => visualColor;
        public Light LightSource => lightSource;
        public LightType VisualLightType => visualLightType;
        public GrowRoomEnvironment CurrentRoom => currentRoom;
        public bool IsActive { get; private set; }

        private void Awake()
        {
            ResolveAndConfigureLight();
        }

        private void OnValidate()
        {
            coverageRadius = Mathf.Max(0.1f, coverageRadius);
            visualIntensity = Mathf.Max(0f, visualIntensity);
            visualRange = Mathf.Max(0.1f, visualRange);
            ResolveAndConfigureLight();
        }

        private void OnEnable()
        {
            ResolveAndConfigureLight();
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
            ResolveAndConfigureLight();
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

        private void ResolveAndConfigureLight()
        {
            if (lightSource == null) lightSource = GetComponentInChildren<Light>(true);
            if (lightSource == null) return;

            if (!lightSource.gameObject.activeSelf) lightSource.gameObject.SetActive(true);
            lightSource.color = visualColor;
            lightSource.intensity = visualIntensity;
            lightSource.range = visualRange;
            lightSource.type = visualLightType;
            if (visualLightType == LightType.Spot) lightSource.spotAngle = spotAngle;
            lightSource.useColorTemperature = false;
        }
    }
}
