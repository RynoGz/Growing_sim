using System;
using Growveld.Interaction;
using Growveld.Inventory;
using UnityEngine;

namespace Growveld.Farming
{
    /// <summary>
    /// Runtime state for one plant. Care and environment modifiers plug into its growth multiplier.
    /// </summary>
    public sealed class PlantInstance : MonoBehaviour, IInteractable, IContextualInfoProvider
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

        private PlantGrowthStage currentStage;

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
        public string WaterStatus => FormatResourceStatus(waterLevel, definition != null ? definition.MaximumWater : 100f);
        public string NutrientStatus => FormatResourceStatus(nutrientLevel, definition != null ? definition.MaximumNutrients : 100f);
        public string InteractionPrompt => IsHarvestReady ? "Inspect harvest-ready plant" : "Care for plant (select watering can or nutrients)";
        public string ContextualInfo => definition == null
            ? "Plant data missing"
            : $"{definition.DisplayName}\nStage: {FormatStage(currentStage)}\nGrowth: {GrowthPercent:0}%\nWater: {WaterStatus}\nNutrients: {NutrientStatus}\nHealth: {health:0}%";

        private void Awake()
        {
            RefreshStage(true);
        }

        private void Update()
        {
            if (!simulationEnabled || definition == null || IsHarvestReady)
            {
                return;
            }

            SimulateCare(Time.deltaTime);
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
                AddWater(definition.WaterPerUse);
            }
            else if (selectedSlot.Item.ItemId == "nutrients"
                && inventory.Remove(selectedSlot.Item, 1))
            {
                AddNutrients(definition.NutrientsPerDose);
            }
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
