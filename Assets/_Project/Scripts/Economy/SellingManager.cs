using System;
using System.Text;
using Growveld.Farming;
using UnityEngine;

namespace Growveld.Economy
{
    /// <summary>
    /// Immediately sells all stored stock at base price x weight x quality multiplier.
    /// </summary>
    public sealed class SellingManager : MonoBehaviour
    {
        [SerializeField] private EconomyManager economy;
        [SerializeField] private FarmStockManager farmStock;
        [SerializeField] private Growveld.Farming.QualitySettings qualitySettings;
        [SerializeField] private SellingSettings sellingSettings;

        public event Action<string, float> SaleCompleted;

        public string LastSaleSummary { get; private set; } = "No sales yet.";

        public float CalculateTotalSaleValue()
        {
            if (farmStock == null || qualitySettings == null || sellingSettings == null) return 0f;
            float total = 0f;
            foreach (QualityGrade grade in Enum.GetValues(typeof(QualityGrade)))
            {
                total += farmStock.GetWeight(grade)
                    * sellingSettings.BasePricePerKilogram
                    * qualitySettings.GetPriceMultiplier(grade);
            }
            return total;
        }

        public string BuildProjectedSummary()
        {
            if (farmStock == null || qualitySettings == null || sellingSettings == null)
            {
                return "Selling system is not configured.";
            }

            StringBuilder builder = new("SELL ALL STOCK\n\n");
            foreach (QualityGrade grade in Enum.GetValues(typeof(QualityGrade)))
            {
                float weight = farmStock.GetWeight(grade);
                float multiplier = qualitySettings.GetPriceMultiplier(grade);
                float value = weight * sellingSettings.BasePricePerKilogram * multiplier;
                builder.AppendLine($"{qualitySettings.GetDisplayName(grade)}: {weight:0.00} kg x R{sellingSettings.BasePricePerKilogram:N0} x {multiplier:0.00} = R{value:N0}");
            }
            builder.AppendLine($"\nProjected total: R{CalculateTotalSaleValue():N0}");
            return builder.ToString();
        }

        public bool SellAllStock()
        {
            float total = CalculateTotalSaleValue();
            if (total <= 0f || farmStock == null || economy == null)
            {
                LastSaleSummary = "No farm stock is available to sell.";
                SaleCompleted?.Invoke(LastSaleSummary, 0f);
                return false;
            }

            string breakdown = BuildProjectedSummary();
            farmStock.ClearAll();
            economy.Credit(total, "Sold all farm stock");
            LastSaleSummary = $"{breakdown}\n\nSALE COMPLETE: R{total:N0}";
            SaleCompleted?.Invoke(LastSaleSummary, total);
            return true;
        }
    }
}
