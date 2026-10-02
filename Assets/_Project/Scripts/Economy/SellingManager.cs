using System;
using System.Text;
using Growveld.Farming;
using Growveld.Progression;
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
        [SerializeField] private BusinessProgression progression;

        public event Action<string, float> SaleCompleted;

        public string LastSaleSummary { get; private set; } = "No sales yet.";
        public int LastExperienceAwarded { get; private set; }

        public float CalculateTotalSaleValue()
        {
            if (farmStock == null || qualitySettings == null || sellingSettings == null) return 0f;
            float total = 0f;
            foreach (FarmStockEntry entry in farmStock.Entries)
            {
                if (entry == null || entry.WeightKilograms <= 0f) continue;
                total += entry.WeightKilograms * GetBasePrice(entry.Strain) * qualitySettings.GetPriceMultiplier(entry.QualityGrade);
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
            int experience = 0;
            foreach (FarmStockEntry entry in farmStock.Entries)
            {
                if (entry == null || entry.WeightKilograms <= 0f) continue;
                float basePrice = GetBasePrice(entry.Strain);
                float multiplier = qualitySettings.GetPriceMultiplier(entry.QualityGrade);
                float value = entry.WeightKilograms * basePrice * multiplier;
                experience += progression != null ? progression.CalculateSaleExperience(entry.WeightKilograms, entry.QualityGrade) : 0;
                string strainName = entry.Strain != null ? entry.Strain.DisplayName : "Northern Lights";
                builder.AppendLine($"{strainName} - {qualitySettings.GetDisplayName(entry.QualityGrade)}");
                builder.AppendLine($"{entry.WeightKilograms:0.00} kg x R{basePrice:N0} x {multiplier:0.00} = R{value:N0}\n");
            }
            builder.AppendLine($"Total: R{CalculateTotalSaleValue():N0}");
            builder.AppendLine($"XP Earned: {experience:N0} XP");
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
            int experience = CalculateTotalExperience();
            farmStock.ClearAll();
            foreach (StorageContainer storage in FindObjectsByType<StorageContainer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                storage?.RestoreStoredKilograms(0f);
            }
            economy.Credit(total, "Sold all farm stock");
            progression?.AwardSale(total, experience);
            LastExperienceAwarded = experience;
            LastSaleSummary = $"{breakdown}\n\nSALE COMPLETE: R{total:N0}\nXP EARNED: {experience:N0}";
            SaleCompleted?.Invoke(LastSaleSummary, total);
            return true;
        }

        private int CalculateTotalExperience()
        {
            if (farmStock == null || progression == null) return 0;
            int total = 0;
            foreach (FarmStockEntry entry in farmStock.Entries)
            {
                if (entry != null) total += progression.CalculateSaleExperience(entry.WeightKilograms, entry.QualityGrade);
            }
            return total;
        }

        private float GetBasePrice(PlantDefinition strain)
        {
            if (strain != null) return strain.BaseSellingPricePerKilogram;
            return sellingSettings != null ? sellingSettings.BasePricePerKilogram : 0f;
        }
    }
}
