using UnityEngine;

namespace Growveld.Environment
{
    /// <summary>
    /// Prototype-wide outdoor daylight and humidity source.
    /// </summary>
    public sealed class OutdoorEnvironment : MonoBehaviour
    {
        public static OutdoorEnvironment Current { get; private set; }

        [SerializeField, Range(0f, 100f)] private float globalHumidity = 44f;
        [SerializeField, Range(0f, 100f)] private float idealMinimumHumidity = 35f;
        [SerializeField, Range(0f, 100f)] private float idealMaximumHumidity = 60f;
        [SerializeField, Range(0.1f, 1f)] private float outdoorGrowthRate = 0.78f;
        [SerializeField, Min(1f)] private float fallbackDayNightCycleRealSeconds = 1800f;
        [SerializeField, Min(0f)] private float fallbackDaylightRealSeconds = 1200f;

        private bool externalDaylightEnabled;
        private bool externalDaylight;

        public float GlobalHumidity => globalHumidity;
        public bool IsDaylight { get; private set; }
        public string HumidityStatus
        {
            get
            {
                if (globalHumidity < idealMinimumHumidity) return "Low";
                if (globalHumidity > idealMaximumHumidity) return "High";
                return "Good";
            }
        }

        private void OnEnable()
        {
            Current = this;
            RefreshDaylight();
        }

        private void OnDisable()
        {
            if (Current == this) Current = null;
        }

        private void Update()
        {
            RefreshDaylight();
        }

        public float GetGrowthMultiplier()
        {
            if (!IsDaylight) return 0f;
            float distanceFromIdeal = globalHumidity < idealMinimumHumidity
                ? idealMinimumHumidity - globalHumidity
                : globalHumidity > idealMaximumHumidity
                    ? globalHumidity - idealMaximumHumidity
                    : 0f;
            float humidityMultiplier = Mathf.Lerp(1f, 0.62f, Mathf.Clamp01(distanceFromIdeal / 35f));
            return outdoorGrowthRate * humidityMultiplier;
        }

        public void SetGlobalHumidity(float humidity)
        {
            globalHumidity = Mathf.Clamp(humidity, 0f, 100f);
        }

        public void SetExternalDaylight(bool daylight)
        {
            externalDaylightEnabled = true;
            externalDaylight = daylight;
            RefreshDaylight();
        }

        public void ClearExternalDaylight()
        {
            externalDaylightEnabled = false;
            RefreshDaylight();
        }

        private void RefreshDaylight()
        {
            if (externalDaylightEnabled)
            {
                IsDaylight = externalDaylight;
                return;
            }

            float cyclePosition = Mathf.Repeat(Time.time, Mathf.Max(1f, fallbackDayNightCycleRealSeconds));
            IsDaylight = cyclePosition < Mathf.Clamp(
                fallbackDaylightRealSeconds,
                0f,
                fallbackDayNightCycleRealSeconds);
        }
    }
}
