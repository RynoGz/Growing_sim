using System;
using Growveld.Interaction;
using Growveld.Inventory;
using Growveld.Environment;
using Growveld.Economy;
using Growveld.Carrying;
using Growveld.UI;
using UnityEngine;

namespace Growveld.Farming
{
    /// <summary>
    /// Runtime state for one plant. Care and environment modifiers plug into its growth multiplier.
    /// </summary>
    public sealed class PlantInstance : MonoBehaviour, IInteractable, IContextualInfoProvider, IInteractionWhileCarrying
    {
        [SerializeField] private PlantDefinition definition;
        [SerializeField] private GameObject[] stageVisuals;
        [SerializeField] private Transform visualRoot;
        [SerializeField, Min(0f)] private float elapsedGrowthSeconds;
        [SerializeField, Min(0f)] private float externalGrowthMultiplier = 1f;
        [SerializeField] private bool simulationEnabled = true;
        [SerializeField, Range(0f, 100f)] private float waterLevel = 100f;
        [SerializeField, Range(0f, 100f)] private float nutrientLevel = 100f;
        [SerializeField, Range(0f, 100f)] private float health = 100f;
        [SerializeField, Range(0f, 100f)] private float qualityScore = 100f;
        [SerializeField, Range(0f, 2f)] private float yieldPotential = 1f;
        [SerializeField, Min(0f)] private float accumulatedCareScore;
        [SerializeField, Min(0f)] private float careSampleSeconds;
        [SerializeField] private GameObject harvestBatchPrefab;

        private PlantGrowthStage currentStage;
        private PlantEnvironmentController environmentController;
        private UtilityManager utilityManager;

        public event Action<PlantGrowthStage> StageChanged;

        public PlantDefinition Definition => definition;
        public PlantGrowthStage CurrentStage => currentStage;
        public float ElapsedGrowthSeconds => elapsedGrowthSeconds;
        public float GrowthProgress01 => definition == null || definition.TotalGrowthSeconds <= 0f
            ? 0f
            : Mathf.Clamp01(elapsedGrowthSeconds / definition.TotalGrowthSeconds);
        public float GrowthPercent => GrowthProgress01 * 100f;
        public bool IsHarvestReady => currentStage == PlantGrowthStage.HarvestReady;
        public float WaterLevel => waterLevel;
        public float NutrientLevel => nutrientLevel;
        public float Health => health;
        public float QualityScore => qualityScore;
        public float YieldPotential => yieldPotential;
        public float AccumulatedCareScore => accumulatedCareScore;
        public float CareSampleSeconds => careSampleSeconds;
        public QualityGrade CurrentQualityGrade => definition != null && definition.QualitySettings != null
            ? definition.QualitySettings.GetGrade(qualityScore)
            : QualityGrade.Standard;
        public float QualityPriceMultiplier => definition != null && definition.QualitySettings != null
            ? definition.QualitySettings.GetPriceMultiplier(CurrentQualityGrade)
            : 1f;
        public float EstimatedYieldKilograms => definition != null
            ? definition.BaseYieldKilograms * yieldPotential
            : 0f;
        public string WaterStatus => FormatResourceStatus(waterLevel, definition != null ? definition.MaximumWater : 100f);
        public string NutrientStatus => FormatResourceStatus(nutrientLevel, definition != null ? definition.MaximumNutrients : 100f);
        public string InteractionPrompt => IsHarvestReady ? "Harvest plant" : "Care for plant (select watering can or nutrients)";
        public string ContextualInfo
        {
            get
            {
                if (definition == null) return "Plant data missing";
                string environmentInfo = environmentController != null
                    ? $"\n{environmentController.ContextSummary}"
                    : string.Empty;
                string gradeName = definition.QualitySettings != null
                    ? definition.QualitySettings.GetDisplayName(CurrentQualityGrade)
                    : CurrentQualityGrade.ToString();
                return $"{definition.DisplayName}\nStage: {FormatStage(currentStage)}\nGrowth: {GrowthPercent:0}%\nWater: {WaterStatus}\nNutrients: {NutrientStatus}\nHealth: {health:0}%\nQuality: {gradeName}\nYield potential: {yieldPotential * 100f:0}%{environmentInfo}";
            }
        }

        private void Awake()
        {
            environmentController = GetComponent<PlantEnvironmentController>();
            utilityManager = FindFirstObjectByType<UtilityManager>();
            RefreshStage(true);
        }

        private void Update()
        {
            if (!simulationEnabled || definition == null || IsHarvestReady)
            {
                return;
            }

            SimulateCare(Time.deltaTime);
            RecordCareSample(Time.deltaTime);
            float careMultiplier = CalculateCareGrowthMultiplier();
            if (health > 0f && externalGrowthMultiplier > 0f && careMultiplier > 0f)
            {
                AdvanceGrowth(Time.deltaTime * externalGrowthMultiplier * careMultiplier);
            }
        }

        public bool CanInteract(GameObject interactor)
        {
            return true;
        }

        public void Interact(GameObject interactor)
        {
            if (IsHarvestReady)
            {
                Harvest(interactor);
                return;
            }

            if (!interactor.TryGetComponent(out PlayerInventory inventory))
            {
                return;
            }

            InventorySlot selectedSlot = inventory.SelectedSlot;
            if (selectedSlot == null || selectedSlot.IsEmpty)
            {
                return;
            }

            if (selectedSlot.Item.ItemId == "watering_can")
            {
                float waterBefore = waterLevel;
                AddWater(definition.WaterPerUse);
                if (waterLevel > waterBefore) utilityManager?.RecordWatering();
            }
            else if (selectedSlot.Item.ItemId == "nutrients"
                && inventory.Remove(selectedSlot.Item, 1))
            {
                AddNutrients(definition.NutrientsPerDose);
            }
        }

        public HarvestBatch Harvest()
        {
            return Harvest(null);
        }

        public HarvestBatch Harvest(GameObject harvester)
        {
            if (!IsHarvestReady || harvestBatchPrefab == null)
            {
                return null;
            }

            PlayerCarryController carryController = null;
            if (harvester != null)
            {
                if (!harvester.TryGetComponent(out carryController) || carryController.IsCarrying)
                {
                    GameplayMessageUI.Show("Your hands are full.");
                    return null;
                }
            }

            Vector3 spawnPosition = harvester != null
                ? transform.position
                : transform.position + transform.forward * 0.8f + Vector3.up * 0.45f;
            GameObject batchObject = Instantiate(harvestBatchPrefab, spawnPosition, Quaternion.identity);
            HarvestBatch batch = batchObject.GetComponent<HarvestBatch>();
            if (batch != null)
            {
                batch.Initialise(CalculateHarvestYieldKilograms(), CurrentQualityGrade, HarvestStatus.Fresh);
            }

            if (carryController != null)
            {
                CarryableObject carryable = batchObject.GetComponent<CarryableObject>();
                if (carryable == null || !carryController.TryPickUp(carryable))
                {
                    Destroy(batchObject);
                    GameplayMessageUI.Show("Your hands are full.");
                    return null;
                }
            }

            PlantingContainer container = GetComponentInParent<PlantingContainer>();
            container?.ClearPlant(this);
            Destroy(gameObject);
            return batch;
        }

        public void AdvanceGrowth(float growthSeconds)
        {
            if (definition == null || growthSeconds <= 0f)
            {
                return;
            }

            elapsedGrowthSeconds = Mathf.Clamp(
                elapsedGrowthSeconds + growthSeconds,
                0f,
                definition.TotalGrowthSeconds);
            RefreshStage(false);
        }

        public void SetExternalGrowthMultiplier(float multiplier)
        {
            externalGrowthMultiplier = Mathf.Max(0f, multiplier);
        }

        public void AddWater(float amount)
        {
            if (definition == null) return;
            waterLevel = Mathf.Clamp(waterLevel + Mathf.Max(0f, amount), 0f, definition.MaximumWater);
        }

        public void AddNutrients(float amount)
        {
            if (definition == null) return;
            nutrientLevel = Mathf.Clamp(nutrientLevel + Mathf.Max(0f, amount), 0f, definition.MaximumNutrients);
        }

        public void RestoreCare(float restoredWater, float restoredNutrients, float restoredHealth)
        {
            float maximumWater = definition != null ? definition.MaximumWater : 100f;
            float maximumNutrients = definition != null ? definition.MaximumNutrients : 100f;
            waterLevel = Mathf.Clamp(restoredWater, 0f, maximumWater);
            nutrientLevel = Mathf.Clamp(restoredNutrients, 0f, maximumNutrients);
            health = Mathf.Clamp(restoredHealth, 0f, 100f);
        }

        public void RestoreQuality(float restoredQualityScore, float restoredYieldPotential, float restoredAccumulatedScore, float restoredSampleSeconds)
        {
            qualityScore = Mathf.Clamp(restoredQualityScore, 0f, 100f);
            yieldPotential = Mathf.Clamp(restoredYieldPotential, 0f, 2f);
            accumulatedCareScore = Mathf.Max(0f, restoredAccumulatedScore);
            careSampleSeconds = Mathf.Max(0f, restoredSampleSeconds);
        }

        public float CalculateHarvestYieldKilograms()
        {
            if (definition == null) return 0f;
            float randomVariation = UnityEngine.Random.Range(0.95f, 1.05f);
            return Mathf.Max(0.01f, definition.BaseYieldKilograms * yieldPotential * randomVariation);
        }

        private void SimulateCare(float realSeconds)
        {
            float realMinutes = Mathf.Max(0f, realSeconds) / 60f;
            waterLevel = Mathf.Max(0f, waterLevel - definition.WaterConsumptionPerRealMinute * realMinutes);
            nutrientLevel = Mathf.Max(0f, nutrientLevel - definition.NutrientConsumptionPerRealMinute * realMinutes);

            bool critical = waterLevel <= definition.MaximumWater * 0.05f
                || nutrientLevel <= definition.MaximumNutrients * 0.05f;
            bool good = waterLevel >= definition.MaximumWater * 0.4f
                && nutrientLevel >= definition.MaximumNutrients * 0.4f;

            if (critical)
            {
                health = Mathf.Max(0f, health - definition.HealthLossPerCriticalMinute * realMinutes);
            }
            else if (good)
            {
                health = Mathf.Min(100f, health + definition.HealthRecoveryPerGoodMinute * realMinutes);
            }
        }

        private float CalculateCareGrowthMultiplier()
        {
            float waterRatio = definition.MaximumWater <= 0f ? 1f : waterLevel / definition.MaximumWater;
            float nutrientRatio = definition.MaximumNutrients <= 0f ? 1f : nutrientLevel / definition.MaximumNutrients;
            float waterMultiplier = waterRatio >= 0.35f ? 1f : Mathf.Lerp(0.25f, 0.75f, waterRatio / 0.35f);
            float nutrientMultiplier = nutrientRatio >= 0.35f ? 1f : Mathf.Lerp(0.35f, 0.8f, nutrientRatio / 0.35f);
            return Mathf.Min(waterMultiplier, nutrientMultiplier);
        }

        private void RecordCareSample(float realSeconds)
        {
            if (realSeconds <= 0f || definition == null)
            {
                return;
            }

            float waterRatio = definition.MaximumWater <= 0f ? 1f : waterLevel / definition.MaximumWater;
            float nutrientRatio = definition.MaximumNutrients <= 0f ? 1f : nutrientLevel / definition.MaximumNutrients;
            float healthRatio = health / 100f;
            float environmentFactor = environmentController != null ? environmentController.QualityFactor : 0.8f;
            float instantCareScore = Mathf.Clamp01(
                waterRatio * 0.28f
                + nutrientRatio * 0.28f
                + healthRatio * 0.24f
                + environmentFactor * 0.2f) * 100f;

            accumulatedCareScore += instantCareScore * realSeconds;
            careSampleSeconds += realSeconds;
            qualityScore = careSampleSeconds <= 0f ? 100f : accumulatedCareScore / careSampleSeconds;

            float careRatio = qualityScore / 100f;
            yieldPotential = Mathf.Lerp(0.42f, 1.12f, careRatio);
        }

        public void RestoreGrowth(float restoredElapsedSeconds)
        {
            elapsedGrowthSeconds = definition == null
                ? Mathf.Max(0f, restoredElapsedSeconds)
                : Mathf.Clamp(restoredElapsedSeconds, 0f, definition.TotalGrowthSeconds);
            RefreshStage(true);
        }

        private void RefreshStage(bool force)
        {
            if (definition == null)
            {
                return;
            }

            PlantGrowthStage nextStage = definition.GetStage(elapsedGrowthSeconds);
            bool stageChanged = nextStage != currentStage;
            currentStage = nextStage;

            if (stageVisuals != null)
            {
                for (int index = 0; index < stageVisuals.Length; index++)
                {
                    if (stageVisuals[index] != null)
                    {
                        stageVisuals[index].SetActive(index == (int)currentStage);
                    }
                }
            }

            UpdateGradualScale();
            if (stageChanged || force)
            {
                StageChanged?.Invoke(currentStage);
            }
        }

        private void UpdateGradualScale()
        {
            if (visualRoot == null || definition == null || IsHarvestReady)
            {
                return;
            }

            float stageStart = definition.GetStageStartTime(currentStage);
            float stageDuration = definition.GetStageDuration(currentStage);
            float stageProgress = stageDuration <= 0f
                ? 1f
                : Mathf.Clamp01((elapsedGrowthSeconds - stageStart) / stageDuration);
            float scale = Mathf.Lerp(0.82f, 1f, stageProgress);
            visualRoot.localScale = Vector3.one * scale;
        }

        private static string FormatStage(PlantGrowthStage stage)
        {
            return stage == PlantGrowthStage.HarvestReady ? "Harvest Ready" : stage.ToString();
        }

        private static string FormatResourceStatus(float current, float maximum)
        {
            float ratio = maximum <= 0f ? 1f : current / maximum;
            if (ratio >= 0.65f) return "Good";
            if (ratio >= 0.3f) return "Low";
            return "Too Low";
        }
    }
}
