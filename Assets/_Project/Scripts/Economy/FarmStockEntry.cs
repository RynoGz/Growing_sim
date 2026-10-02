using System;
using Growveld.Farming;
using UnityEngine;

namespace Growveld.Economy
{
    [Serializable]
    public sealed class FarmStockEntry
    {
        [SerializeField] private PlantDefinition strain;
        [SerializeField] private QualityGrade qualityGrade;
        [SerializeField, Min(0f)] private float weightKilograms;

        public PlantDefinition Strain => strain;
        public string StrainId => strain != null ? strain.PlantId : string.Empty;
        public QualityGrade QualityGrade => qualityGrade;
        public float WeightKilograms => weightKilograms;

        public FarmStockEntry(PlantDefinition strainDefinition, QualityGrade grade)
        {
            strain = strainDefinition;
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
