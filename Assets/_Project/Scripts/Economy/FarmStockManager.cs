using System;
using System.Collections.Generic;
using Growveld.Farming;
using UnityEngine;

namespace Growveld.Economy
{
    /// <summary>
    /// Aggregated dried farm stock, kept separately by strain and quality grade.
    /// </summary>
    public sealed class FarmStockManager : MonoBehaviour
    {
        [SerializeField] private List<FarmStockEntry> entries = new();
        [SerializeField] private PlantDefinition defaultStrain;

        public event Action StockChanged;

        public IReadOnlyList<FarmStockEntry> Entries => entries;
        public PlantDefinition DefaultStrain => defaultStrain;
        public float TotalKilograms
        {
            get
            {
                float total = 0f;
                foreach (FarmStockEntry entry in entries) total += entry.WeightKilograms;
                return total;
            }
        }

        public void AddStock(PlantDefinition strain, QualityGrade grade, float kilograms)
        {
            PlantDefinition resolved = strain != null ? strain : defaultStrain;
            if (kilograms <= 0f || resolved == null) return;
            GetEntry(resolved, grade, true).Add(kilograms);
            StockChanged?.Invoke();
        }

        public void AddStock(QualityGrade grade, float kilograms) => AddStock(defaultStrain, grade, kilograms);

        public float GetWeight(PlantDefinition strain, QualityGrade grade)
        {
            FarmStockEntry entry = GetEntry(strain, grade, false);
            return entry != null ? entry.WeightKilograms : 0f;
        }

        public float GetWeight(QualityGrade grade)
        {
            float total = 0f;
            foreach (FarmStockEntry entry in entries)
            {
                if (entry != null && entry.QualityGrade == grade) total += entry.WeightKilograms;
            }
            return total;
        }

        public void ClearAll()
        {
            entries.Clear();
            StockChanged?.Invoke();
        }

        public void RestoreStock(float low, float standard, float premium, float topGrade)
        {
            entries.Clear();
            AddRestoredEntry(defaultStrain, QualityGrade.Low, low);
            AddRestoredEntry(defaultStrain, QualityGrade.Standard, standard);
            AddRestoredEntry(defaultStrain, QualityGrade.Premium, premium);
            AddRestoredEntry(defaultStrain, QualityGrade.TopGrade, topGrade);
            StockChanged?.Invoke();
        }

        public void RestoreEntries(IEnumerable<RestoredFarmStockEntry> restoredEntries)
        {
            entries.Clear();
            if (restoredEntries != null)
            {
                foreach (RestoredFarmStockEntry restored in restoredEntries)
                {
                    AddRestoredEntry(restored.Strain, restored.QualityGrade, restored.WeightKilograms);
                }
            }
            StockChanged?.Invoke();
        }

        private void AddRestoredEntry(PlantDefinition strain, QualityGrade grade, float kilograms)
        {
            if (strain == null || kilograms <= 0f) return;
            FarmStockEntry entry = GetEntry(strain, grade, true);
            entry.Set(kilograms);
        }

        private FarmStockEntry GetEntry(PlantDefinition strain, QualityGrade grade, bool create)
        {
            entries ??= new List<FarmStockEntry>();
            foreach (FarmStockEntry entry in entries)
            {
                if (entry != null && entry.Strain == strain && entry.QualityGrade == grade) return entry;
            }
            if (!create || strain == null) return null;
            FarmStockEntry created = new(strain, grade);
            entries.Add(created);
            return created;
        }
    }

    public readonly struct RestoredFarmStockEntry
    {
        public RestoredFarmStockEntry(PlantDefinition strain, QualityGrade qualityGrade, float weightKilograms)
        {
            Strain = strain;
            QualityGrade = qualityGrade;
            WeightKilograms = weightKilograms;
        }

        public PlantDefinition Strain { get; }
        public QualityGrade QualityGrade { get; }
        public float WeightKilograms { get; }
    }
}
