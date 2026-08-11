using System.Text;
using Growveld.Economy;
using Growveld.Farming;
using Growveld.Inventory;
using UnityEngine;
using UnityEngine.UI;

namespace Growveld.UI
{
    /// <summary>
    /// Shows the player the balanced prototype costs, crop values, and utility rates.
    /// </summary>
    public sealed class BalanceOverviewUI : MonoBehaviour
    {
        [SerializeField] private EconomyManager economy;
        [SerializeField] private PlantDefinition plant;
        [SerializeField] private Growveld.Farming.QualitySettings quality;
        [SerializeField] private SellingSettings selling;
        [SerializeField] private UtilitySettings utilities;
        [SerializeField] private ItemDefinition[] items;
        [SerializeField] private Text summaryText;
        [SerializeField, Min(0f)] private float growLightKilowatts = 1.2f;
        [SerializeField, Range(0f, 24f)] private float scheduledLightHours = 18f;

        private void OnEnable()
        {
            if (economy != null) economy.BalanceChanged += HandleBalanceChanged;
            Refresh();
        }

        private void OnDisable()
        {
            if (economy != null) economy.BalanceChanged -= HandleBalanceChanged;
        }

        private void HandleBalanceChanged(float _) => Refresh();

        private void Refresh()
        {
            if (summaryText == null || plant == null || quality == null || selling == null || utilities == null) return;

            float outdoorKit = Price("seed") + Price("nutrients") + Price("watering_can")
                + Price("grow_pot") + Price("drying_rack") + Price("storage_bin");
            float indoorKit = outdoorKit + Price("grow_light") + Price("grow_room");
            float idealYield = plant.BaseYieldKilograms * 1.12f;
            float lowSale = idealYield * selling.BasePricePerKilogram * quality.GetPriceMultiplier(QualityGrade.Low);
            float standardSale = idealYield * selling.BasePricePerKilogram * quality.GetPriceMultiplier(QualityGrade.Standard);
            float premiumSale = idealYield * selling.BasePricePerKilogram * quality.GetPriceMultiplier(QualityGrade.Premium);
            float topSale = idealYield * selling.BasePricePerKilogram * quality.GetPriceMultiplier(QualityGrade.TopGrade);
            float dailyLightKwh = growLightKilowatts * scheduledLightHours;
            float dailyLightCost = dailyLightKwh * utilities.ElectricityRandPerKilowattHour;
            float wateringCost = utilities.WaterLitresPerWatering * utilities.WaterRandPerLitre;

            StringBuilder builder = new();
            builder.AppendLine("FINANCIAL PLAN");
            builder.AppendLine($"Current balance: R{(economy != null ? economy.Balance : 0f):N0}");
            builder.AppendLine($"Outdoor starter kit: R{outdoorKit:N0}  |  Indoor starter kit: R{indoorKit:N0}");
            builder.AppendLine();
            builder.AppendLine($"CROP: {plant.TotalGrowthSeconds / 60f:0} min grow + 10 min dry  |  ideal yield about {idealYield:0.00} kg");
            builder.AppendLine($"Projected sale: Low R{lowSale:N0}  |  Standard R{standardSale:N0}  |  Premium R{premiumSale:N0}  |  Top R{topSale:N0}");
            builder.AppendLine();
            builder.AppendLine($"ONE GROW LIGHT / DAY: {dailyLightKwh:0.0} kWh = R{dailyLightCost:0.00}");
            builder.AppendLine($"ONE WATERING: {utilities.WaterLitresPerWatering:0} L = R{wateringCost:0.00}");
            builder.Append("Equipment is reusable. Seeds and nutrient doses are recurring costs.");
            summaryText.text = builder.ToString();
        }

        private float Price(string itemId)
        {
            if (items == null) return 0f;
            foreach (ItemDefinition item in items) if (item != null && item.ItemId == itemId) return item.PurchasePrice;
            return 0f;
        }
    }
}
