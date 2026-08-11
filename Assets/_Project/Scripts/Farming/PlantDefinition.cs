using UnityEngine;

namespace Growveld.Farming
{
    /// <summary>
    /// Configurable timings and baseline yield for the prototype's single crop.
    /// </summary>
    [CreateAssetMenu(menuName = "Growveld/Farming/Plant Definition", fileName = "Plant_")]
    public sealed class PlantDefinition : ScriptableObject
    {
        [SerializeField] private string plantId = "generic_crop";
        [SerializeField] private string displayName = "Generic Cannabis Plant";
        [SerializeField, Min(1f)] private float germinationSeconds = 120f;
        [SerializeField, Min(1f)] private float seedlingSeconds = 240f;
        [SerializeField, Min(1f)] private float vegetativeSeconds = 540f;
        [SerializeField, Min(1f)] private float floweringSeconds = 900f;
        [SerializeField, Min(0.01f)] private float baseYieldKilograms = 0.45f;

        public string PlantId => plantId;
        public string DisplayName => displayName;
        public float GerminationSeconds => germinationSeconds;
        public float SeedlingSeconds => seedlingSeconds;
        public float VegetativeSeconds => vegetativeSeconds;
        public float FloweringSeconds => floweringSeconds;
        public float BaseYieldKilograms => baseYieldKilograms;
        public float TotalGrowthSeconds => germinationSeconds + seedlingSeconds + vegetativeSeconds + floweringSeconds;

        public float GetStageStartTime(PlantGrowthStage stage)
        {
            return stage switch
            {
                PlantGrowthStage.Germination => 0f,
                PlantGrowthStage.Seedling => germinationSeconds,
                PlantGrowthStage.Vegetative => germinationSeconds + seedlingSeconds,
                PlantGrowthStage.Flowering => germinationSeconds + seedlingSeconds + vegetativeSeconds,
                _ => TotalGrowthSeconds
            };
        }

        public float GetStageDuration(PlantGrowthStage stage)
        {
            return stage switch
            {
                PlantGrowthStage.Germination => germinationSeconds,
                PlantGrowthStage.Seedling => seedlingSeconds,
                PlantGrowthStage.Vegetative => vegetativeSeconds,
                PlantGrowthStage.Flowering => floweringSeconds,
                _ => 0f
            };
        }

        public PlantGrowthStage GetStage(float elapsedGrowthSeconds)
        {
            if (elapsedGrowthSeconds < germinationSeconds) return PlantGrowthStage.Germination;
            if (elapsedGrowthSeconds < germinationSeconds + seedlingSeconds) return PlantGrowthStage.Seedling;
            if (elapsedGrowthSeconds < germinationSeconds + seedlingSeconds + vegetativeSeconds) return PlantGrowthStage.Vegetative;
            if (elapsedGrowthSeconds < TotalGrowthSeconds) return PlantGrowthStage.Flowering;
            return PlantGrowthStage.HarvestReady;
        }
    }
}
