using System;
using Growveld.Core;
using Growveld.Environment;
using UnityEngine;

namespace Growveld.Economy
{
    /// <summary>
    /// Accumulates actual light runtime and manual water usage, then deducts one daily bill.
    /// </summary>
    public sealed class UtilityManager : MonoBehaviour
    {
        [SerializeField] private EconomyManager economy;
        [SerializeField] private UtilitySettings settings;
        [SerializeField] private GameTimeManager gameTime;
        [SerializeField, Min(0f)] private float currentElectricityKilowattHours;
        [SerializeField, Min(0f)] private float currentWaterLitres;
        [SerializeField, Min(0f)] private float fallbackDayElapsedSeconds;
        [SerializeField, Min(1)] private int currentDay = 1;
        [SerializeField] private bool externalDayClockEnabled;

        public event Action UsageChanged;
        public event Action<DailyUtilityBill> BillProcessed;

        public float CurrentElectricityKilowattHours => currentElectricityKilowattHours;
        public float CurrentWaterLitres => currentWaterLitres;
        public int CurrentDay => currentDay;
        public DailyUtilityBill LastBill { get; private set; }
        public float CurrentElectricityCost => settings != null
            ? currentElectricityKilowattHours * settings.ElectricityRandPerKilowattHour
            : 0f;
        public float CurrentWaterCost => settings != null
            ? currentWaterLitres * settings.WaterRandPerLitre
            : 0f;

        private float notificationTimer;

        private void Update()
        {
            float activeKilowatts = 0f;
            foreach (GrowLight growLight in GrowLight.GetActiveLights())
            {
                if (growLight != null && growLight.IsActive) activeKilowatts += growLight.PowerConsumptionKilowatts;
            }
            // Utilities follow the accelerated simulation clock. At the prototype's
            // 30-minute day, an 18-hour light schedule must still consume 18 game-hours.
            float elapsedHours = gameTime != null
                ? gameTime.GameMinutesAdvancedThisFrame / 60f
                : Time.deltaTime / 3600f;
            currentElectricityKilowattHours += activeKilowatts * Mathf.Max(0f, elapsedHours);

            if (!externalDayClockEnabled && settings != null)
            {
                fallbackDayElapsedSeconds += Time.deltaTime;
                if (fallbackDayElapsedSeconds >= settings.FallbackDayRealSeconds)
                {
                    fallbackDayElapsedSeconds -= settings.FallbackDayRealSeconds;
                    ProcessDailyBill(currentDay);
                    currentDay++;
                }
            }

            notificationTimer -= Time.deltaTime;
            if (notificationTimer <= 0f)
            {
                notificationTimer = 1f;
                UsageChanged?.Invoke();
            }
        }

        public void RecordWatering()
        {
            if (settings == null) return;
            currentWaterLitres += settings.WaterLitresPerWatering;
            UsageChanged?.Invoke();
        }

        public DailyUtilityBill ProcessDailyBill(int completedDay)
        {
            float electricityCost = CurrentElectricityCost;
            float waterCost = CurrentWaterCost;
            DailyUtilityBill bill = new(
                completedDay,
                electricityCost,
                waterCost,
                currentElectricityKilowattHours,
                currentWaterLitres);
            LastBill = bill;
            if (bill.TotalCost > 0f)
            {
                economy?.DeductBill(bill.TotalCost, $"Day {completedDay} utilities");
            }

            currentElectricityKilowattHours = 0f;
            currentWaterLitres = 0f;
            BillProcessed?.Invoke(bill);
            UsageChanged?.Invoke();
            return bill;
        }

        public void EnableExternalDayClock(int day)
        {
            externalDayClockEnabled = true;
            currentDay = Mathf.Max(1, day);
        }

        public void RestoreUsage(float electricityKwh, float waterLitres, int day, float fallbackElapsed)
        {
            currentElectricityKilowattHours = Mathf.Max(0f, electricityKwh);
            currentWaterLitres = Mathf.Max(0f, waterLitres);
            currentDay = Mathf.Max(1, day);
            fallbackDayElapsedSeconds = Mathf.Max(0f, fallbackElapsed);
            UsageChanged?.Invoke();
        }
    }
}
