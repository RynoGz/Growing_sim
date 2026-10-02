using System.Collections.Generic;
using Growveld.Core;
using UnityEngine;

namespace Growveld.Environment
{
    /// <summary>
    /// An automatically scheduled indoor grow light with circular horizontal coverage.
    /// </summary>
    public sealed class GrowLight : MonoBehaviour
    {
        public const float SharedCoverageRadius = 6f;
        public const float SharedVisualIntensity = 55f;
        public const float SharedVisualRange = 4.5f;
        public static readonly Color SharedVisualColor = new(0.8f, 0.84f, 0.96f, 1f);

        private static readonly List<GrowLight> ActiveLights = new();

        [SerializeField, Min(0.1f)] private float coverageRadius = SharedCoverageRadius;
        [SerializeField, Min(0f)] private float powerConsumptionKilowatts = 1.2f;
        [SerializeField] private Light lightSource;
        [SerializeField, Min(0f)] private float visualIntensity = SharedVisualIntensity;
        [SerializeField, Min(0.1f)] private float visualRange = SharedVisualRange;
        [SerializeField] private Color visualColor = new(0.8f, 0.84f, 0.96f, 1f);
        [SerializeField] private LightType visualLightType = LightType.Point;
        [SerializeField, Range(1f, 179f)] private float spotAngle = 110f;
        [SerializeField] private bool automaticSchedule = true;
        [SerializeField, Min(1f)] private float fallbackCycleRealSeconds = 1800f;
        [SerializeField, Min(0f)] private float fallbackActiveRealSeconds = 1200f;

        private GrowRoomEnvironment currentRoom;
        private GameTimeManager gameTime;
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
            BindToGameClock();
            RefreshState();
        }

        private void Start()
        {
            // All scene Awake calls have completed by Start, so this also covers unusual
            // script ordering where the clock was not yet discoverable during OnEnable.
            BindToGameClock();
        }

        private void OnDisable()
        {
            UnbindGameClock();
            ActiveLights.Remove(this);
            IsActive = false;
            if (lightSource != null) lightSource.enabled = false;
        }

        private void Update()
        {
            if (gameTime == null) BindToGameClock();
            Vector3 roomSamplePosition = lightSource != null
                ? lightSource.transform.position
                : transform.position + Vector3.up;
            currentRoom = GrowRoomEnvironment.FindContainingRoom(roomSamplePosition);
            if (gameTime == null) RefreshState();
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

        /// <summary>Immediately evaluates this instance against the authoritative game clock.</summary>
        public void EvaluateSchedule(GameTimeManager clock)
        {
            if (clock != null && gameTime != clock) BindToGameClock(clock);
            if (automaticSchedule && clock != null)
            {
                SetExternalSchedule(clock.AreGrowLightsScheduledOn);
            }
            else
            {
                ClearExternalSchedule();
            }
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

        private void BindToGameClock()
        {
            GameTimeManager clock = GameTimeManager.Current;
            if (clock == null) clock = FindFirstObjectByType<GameTimeManager>();
            BindToGameClock(clock);
        }

        private void BindToGameClock(GameTimeManager clock)
        {
            if (gameTime == clock)
            {
                if (gameTime != null) HandleGameTimeChanged();
                return;
            }

            UnbindGameClock();
            gameTime = clock;
            if (gameTime != null)
            {
                gameTime.TimeChanged += HandleGameTimeChanged;
                HandleGameTimeChanged();
            }
        }

        private void UnbindGameClock()
        {
            if (gameTime != null) gameTime.TimeChanged -= HandleGameTimeChanged;
            gameTime = null;
        }

        private void HandleGameTimeChanged()
        {
            if (automaticSchedule && gameTime != null)
            {
                SetExternalSchedule(gameTime.AreGrowLightsScheduledOn);
            }
            else
            {
                ClearExternalSchedule();
            }
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
            lightSource.shadows = LightShadows.Soft;
            lightSource.shadowStrength = 0.72f;
            lightSource.shadowBias = 0.08f;
            lightSource.shadowNormalBias = 0.35f;
        }
    }
}
