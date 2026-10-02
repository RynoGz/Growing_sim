using UnityEngine;

namespace Growveld.Farming
{
    /// <summary>
    /// Data-driven strain definition shared by seeds, plants, harvests, stock and sales.
    /// </summary>
    [CreateAssetMenu(menuName = "Growveld/Farming/Plant Definition", fileName = "Plant_")]
    public sealed class PlantDefinition : ScriptableObject
    {
        [SerializeField] private string plantId = "northern_lights";
        [SerializeField] private string displayName = "Northern Lights";
        [SerializeField, TextArea] private string shortDescription;
        [SerializeField, TextArea] private string guidanceText;
        [SerializeField] private string difficulty = "Easy";
        [SerializeField] private string growthLabel = "Fast";
        [SerializeField] private string yieldLabel = "High";
        [SerializeField] private string indoorSuitability = "Excellent";
        [SerializeField] private string outdoorSuitability = "Good";
        [SerializeField, Min(1)] private int unlockLevel = 1;
        [SerializeField, Min(0f)] private float seedPurchasePrice = 180f;
        [SerializeField, Min(0f)] private float baseSellingPricePerKilogram = 1200f;
        [SerializeField, Range(1f, 100f)] private float maximumQualityPotential = 100f;
        [SerializeField, Min(0.1f)] private float indoorGrowthModifier = 1f;
        [SerializeField, Min(0.1f)] private float outdoorGrowthModifier = 1f;
        [SerializeField, Min(0.1f)] private float waterConsumptionModifier = 1f;
        [SerializeField, Min(0.1f)] private float nutrientConsumptionModifier = 1f;
        [SerializeField, Range(0f, 100f)] private float preferredHumidityMinimum = 52f;
        [SerializeField, Range(0f, 100f)] private float preferredHumidityMaximum = 66f;
        [SerializeField, Min(0.1f)] private float qualitySensitivity = 1f;
        [SerializeField] private Sprite shopIcon;
        [SerializeField] private GameObject plantPrefab;
        [SerializeField, Min(1f)] private float germinationSeconds = 120f;
        [SerializeField, Min(1f)] private float seedlingSeconds = 240f;
        [SerializeField, Min(1f)] private float vegetativeSeconds = 540f;
        [SerializeField, Min(1f)] private float floweringSeconds = 900f;
        [SerializeField, Min(0.01f)] private float baseYieldKilograms = 0.45f;
        [Header("Care")]
        [SerializeField, Min(1f)] private float maximumWater = 100f;
        [SerializeField, Min(1f)] private float maximumNutrients = 100f;
        [SerializeField, Min(0f)] private float waterConsumptionPerRealMinute = 4f;
        [SerializeField, Min(0f)] private float nutrientConsumptionPerRealMinute = 2f;
        [SerializeField, Min(0f)] private float waterPerUse = 45f;
        [SerializeField, Min(0f)] private float nutrientsPerDose = 35f;
        [SerializeField, Min(0f)] private float healthLossPerCriticalMinute = 8f;
        [SerializeField, Min(0f)] private float healthRecoveryPerGoodMinute = 2f;
        [SerializeField] private QualitySettings qualitySettings;

        public string PlantId => plantId;
        public string DisplayName => displayName;
        public string ShortDescription => shortDescription;
        public string GuidanceText => guidanceText;
        public string Difficulty => difficulty;
        public string GrowthLabel => growthLabel;
        public string YieldLabel => yieldLabel;
        public string IndoorSuitability => indoorSuitability;
        public string OutdoorSuitability => outdoorSuitability;
        public int UnlockLevel => Mathf.Max(1, unlockLevel);
        public float SeedPurchasePrice => seedPurchasePrice;
        public float BaseSellingPricePerKilogram => baseSellingPricePerKilogram;
        public float MaximumQualityPotential => maximumQualityPotential;
        public float IndoorGrowthModifier => indoorGrowthModifier;
        public float OutdoorGrowthModifier => outdoorGrowthModifier;
        public float WaterConsumptionModifier => waterConsumptionModifier;
        public float NutrientConsumptionModifier => nutrientConsumptionModifier;
        public float PreferredHumidityMinimum => Mathf.Min(preferredHumidityMinimum, preferredHumidityMaximum);
        public float PreferredHumidityMaximum => Mathf.Max(preferredHumidityMinimum, preferredHumidityMaximum);
        public float QualitySensitivity => qualitySensitivity;
        public Sprite ShopIcon => shopIcon;
        public GameObject PlantPrefab => plantPrefab;
        public float GerminationSeconds => germinationSeconds;
        public float SeedlingSeconds => seedlingSeconds;
        public float VegetativeSeconds => vegetativeSeconds;
        public float FloweringSeconds => floweringSeconds;
        public float BaseYieldKilograms => baseYieldKilograms;
        public float MaximumWater => maximumWater;
        public float MaximumNutrients => maximumNutrients;
        public float WaterConsumptionPerRealMinute => waterConsumptionPerRealMinute * waterConsumptionModifier;
        public float NutrientConsumptionPerRealMinute => nutrientConsumptionPerRealMinute * nutrientConsumptionModifier;
        public float WaterPerUse => waterPerUse;
        public float NutrientsPerDose => nutrientsPerDose;
        public float HealthLossPerCriticalMinute => healthLossPerCriticalMinute;
        public float HealthRecoveryPerGoodMinute => healthRecoveryPerGoodMinute;
        public QualitySettings QualitySettings => qualitySettings;
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
