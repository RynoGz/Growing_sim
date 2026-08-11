using System;
using Growveld.Economy;
using Growveld.Environment;
using UnityEngine;

namespace Growveld.Core
{
    /// <summary>
    /// Authoritative in-session clock. One full game day lasts thirty real minutes by default.
    /// </summary>
    public sealed class GameTimeManager : MonoBehaviour
    {
        [SerializeField] private TimeSettings settings;
        [SerializeField] private Light sun;
        [SerializeField] private UtilityManager utilities;
        [SerializeField, Min(1)] private int day = 1;
        [SerializeField, Range(0f, 24f)] private float timeOfDayHours = 7f;

        public event Action<int> DayStarted;
        public event Action TimeChanged;

        public int Day => day;
        public float TimeOfDayHours => timeOfDayHours;
        public float GameMinutesAdvancedThisFrame { get; private set; }
        public bool IsDaylight => settings != null
            && IsHourWithinSchedule(timeOfDayHours, settings.SunriseHour, settings.SunsetHour);
        public bool AreGrowLightsScheduledOn => settings != null
            && IsHourWithinSchedule(timeOfDayHours, settings.GrowLightOnHour, settings.GrowLightOffHour);
        public string FormattedTime
        {
            get
            {
                int hour = Mathf.FloorToInt(timeOfDayHours) % 24;
                int minute = Mathf.FloorToInt((timeOfDayHours - hour) * 60f) % 60;
                return $"{hour:00}:{minute:00}";
            }
        }

        private void Start()
        {
            utilities?.EnableExternalDayClock(day);
            ApplyEnvironmentState();
        }

        private void Update()
        {
            if (settings == null || settings.RealSecondsPerFullDay <= 0f)
            {
                GameMinutesAdvancedThisFrame = 0f;
                return;
            }

            float gameHoursDelta = Time.deltaTime * (24f / settings.RealSecondsPerFullDay);
            GameMinutesAdvancedThisFrame = gameHoursDelta * 60f;
            timeOfDayHours += gameHoursDelta;

            while (timeOfDayHours >= 24f)
            {
                timeOfDayHours -= 24f;
                int completedDay = day;
                day++;
                utilities?.ProcessDailyBill(completedDay);
                utilities?.EnableExternalDayClock(day);
                DayStarted?.Invoke(day);
            }

            ApplyEnvironmentState();
            TimeChanged?.Invoke();
        }

        public void RestoreTime(int restoredDay, float restoredHour)
        {
            day = Mathf.Max(1, restoredDay);
            timeOfDayHours = Mathf.Repeat(restoredHour, 24f);
            utilities?.EnableExternalDayClock(day);
            ApplyEnvironmentState();
            TimeChanged?.Invoke();
        }

        private void ApplyEnvironmentState()
        {
            OutdoorEnvironment.Current?.SetExternalDaylight(IsDaylight);
            foreach (GrowLight growLight in GrowLight.GetActiveLights())
            {
                growLight?.SetExternalSchedule(AreGrowLightsScheduledOn);
            }

            if (sun == null || settings == null) return;
            float normalizedDay = timeOfDayHours / 24f;
            sun.transform.rotation = Quaternion.Euler(normalizedDay * 360f - 90f, 145f, 0f);
            float daylightBlend = IsDaylight ? 1f : 0f;
            sun.intensity = Mathf.Lerp(settings.NightSunIntensity, settings.DaytimeSunIntensity, daylightBlend);
            sun.color = IsDaylight
                ? new Color(1f, 0.91f, 0.75f)
                : new Color(0.36f, 0.44f, 0.62f);
            RenderSettings.ambientIntensity = IsDaylight ? 1f : 0.22f;
        }

        private static bool IsHourWithinSchedule(float hour, float startHour, float endHour)
        {
            if (Mathf.Approximately(startHour, endHour)) return true;
            return startHour < endHour
                ? hour >= startHour && hour < endHour
                : hour >= startHour || hour < endHour;
        }
    }
}
