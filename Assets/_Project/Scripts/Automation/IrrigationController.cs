using Growveld.Economy;
using Growveld.Farming;
using Growveld.Interaction;
using UnityEngine;

namespace Growveld.Automation
{
    public sealed class IrrigationController : AutomationEquipment, IInteractable, IContextualInfoProvider, IContextualInteractionPrompt
    {
        [SerializeField, Min(0.25f)] private float cycleIntervalSeconds = 8f;
        [SerializeField, Range(0f, 1f)] private float targetWaterFraction = 0.78f;
        [SerializeField, Min(0f)] private float waterPerPlantCycle = 16f;
        [SerializeField, Min(0f)] private float litresPerPlantCycle = 4f;

        private UtilityManager utilities;
        private float timer;

        public string InteractionPrompt => IsOperational ? "Turn off irrigation" : "Turn on irrigation";
        public string ContextualInfo => $"Irrigation Controller\nStatus: {(IsOperational ? "Active" : "Off")}\nCoverage: {CoverageRadius:0.0} m\nMaintains water near {targetWaterFraction * 100f:0}%";

        private void Awake()
        {
            utilities = FindFirstObjectByType<UtilityManager>();
        }

        private void Update()
        {
            timer -= Time.deltaTime;
            if (!IsOperational)
            {
                IsDrawingPower = false;
                return;
            }
            if (timer > 0f) return;

            timer = cycleIntervalSeconds;
            bool serviced = false;
            foreach (PlantInstance plant in FindObjectsByType<PlantInstance>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (plant == null || plant.Definition == null || !Covers(plant.transform.position)) continue;
                float target = plant.Definition.MaximumWater * targetWaterFraction;
                if (plant.WaterLevel >= target) continue;
                float before = plant.WaterLevel;
                plant.AddWater(Mathf.Min(waterPerPlantCycle, target - before));
                if (plant.WaterLevel > before)
                {
                    serviced = true;
                    utilities?.RecordWaterLitres(litresPerPlantCycle);
                }
            }
            IsDrawingPower = serviced;
        }

        public bool CanInteract(GameObject interactor) => true;
        public string GetInteractionPrompt(GameObject interactor) => InteractionPrompt;
        public void Interact(GameObject interactor) => ToggleOperational();

        public static IrrigationController FindCovering(Vector3 position)
        {
            foreach (AutomationEquipment equipment in GetActiveEquipment())
            {
                if (equipment is IrrigationController irrigation && irrigation.IsOperational && irrigation.Covers(position)) return irrigation;
            }
            return null;
        }
    }
}
