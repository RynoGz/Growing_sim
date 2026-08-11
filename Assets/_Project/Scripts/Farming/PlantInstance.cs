using System;
using Growveld.Interaction;
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
        public string InteractionPrompt => IsHarvestReady ? "Inspect harvest-ready plant" : "Inspect plant";
        public string ContextualInfo => definition == null
            ? "Plant data missing"
            : $"{definition.DisplayName}\nStage: {FormatStage(currentStage)}\nGrowth: {GrowthPercent:0}%";

        private void Awake()
        {
            RefreshStage(true);
        }

        private void Update()
        {
            if (!simulationEnabled || definition == null || IsHarvestReady || externalGrowthMultiplier <= 0f)
            {
                return;
            }

            AdvanceGrowth(Time.deltaTime * externalGrowthMultiplier);
        }

        public bool CanInteract(GameObject interactor)
        {
            return true;
        }

        public void Interact(GameObject interactor)
        {
            // The contextual HUD already displays this plant. Care and harvest actions are added later.
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
    }
}
