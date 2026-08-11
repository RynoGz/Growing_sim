using System;
using Growveld.Farming;
using UnityEngine;

namespace Growveld.Economy
{
    [Serializable]
    public sealed class FarmStockEntry
    {
        [SerializeField] private QualityGrade qualityGrade;
        [SerializeField, Min(0f)] private float weightKilograms;

        public QualityGrade QualityGrade => qualityGrade;
        public float WeightKilograms => weightKilograms;

        public FarmStockEntry(QualityGrade grade)
        {
            qualityGrade = grade;
        }

        public void Add(float kilograms)
        {
            weightKilograms = Mathf.Max(0f, weightKilograms + kilograms);
        }

        public void Set(float kilograms)
        {
            weightKilograms = Mathf.Max(0f, kilograms);
        }
    }
}
