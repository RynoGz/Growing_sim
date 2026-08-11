using System;
using System.Collections.Generic;
using Growveld.Farming;
using UnityEngine;

namespace Growveld.Economy
{
    /// <summary>
    /// Aggregated dried farm stock, kept separately by quality grade.
    /// </summary>
    public sealed class FarmStockManager : MonoBehaviour
    {
        [SerializeField] private List<FarmStockEntry> entries = new();

        public event Action StockChanged;

        public IReadOnlyList<FarmStockEntry> Entries => entries;
        public float TotalKilograms
        {
            get
            {
                float total = 0f;
                foreach (FarmStockEntry entry in entries) total += entry.WeightKilograms;
                return total;
            }
        }

        private void Awake()
        {
            EnsureEntries();
        }

        public void AddStock(QualityGrade grade, float kilograms)
        {
            if (kilograms <= 0f) return;
            EnsureEntries();
            GetEntry(grade).Add(kilograms);
            StockChanged?.Invoke();
        }

        public float GetWeight(QualityGrade grade)
        {
            EnsureEntries();
            return GetEntry(grade).WeightKilograms;
        }

        public void ClearAll()
        {
            EnsureEntries();
            foreach (FarmStockEntry entry in entries) entry.Set(0f);
            StockChanged?.Invoke();
        }

        public void RestoreStock(float low, float standard, float premium, float topGrade)
        {
            EnsureEntries();
            GetEntry(QualityGrade.Low).Set(low);
            GetEntry(QualityGrade.Standard).Set(standard);
            GetEntry(QualityGrade.Premium).Set(premium);
            GetEntry(QualityGrade.TopGrade).Set(topGrade);
            StockChanged?.Invoke();
        }

        private FarmStockEntry GetEntry(QualityGrade grade)
        {
            foreach (FarmStockEntry entry in entries)
            {
                if (entry.QualityGrade == grade) return entry;
            }

            FarmStockEntry created = new(grade);
            entries.Add(created);
            return created;
        }

        private void EnsureEntries()
        {
            entries ??= new List<FarmStockEntry>();
            GetEntry(QualityGrade.Low);
            GetEntry(QualityGrade.Standard);
            GetEntry(QualityGrade.Premium);
            GetEntry(QualityGrade.TopGrade);
        }
    }
}
