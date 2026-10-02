using Growveld.Farming;
using Growveld.Interaction;
using Growveld.Inventory;
using Growveld.UI;
using UnityEngine;

namespace Growveld.Automation
{
    public sealed class NutrientDoser : AutomationEquipment, IInteractable, IContextualInfoProvider, IContextualInteractionPrompt
    {
        [SerializeField, Min(1f)] private float tankCapacity = 100f;
        [SerializeField, Min(0f)] private float currentNutrients;
        [SerializeField, Min(1f)] private float unitsPerInventoryItem = 25f;
        [SerializeField, Min(0.25f)] private float cycleIntervalSeconds = 10f;
        [SerializeField, Range(0f, 1f)] private float targetNutrientFraction = 0.75f;
        [SerializeField, Min(0f)] private float nutrientUnitsPerPlantCycle = 8f;
        [SerializeField] private ItemDefinition nutrientItem;

        private float timer;
        private bool emptyNotificationShown;

        public override float StoredResource => currentNutrients;
        public float TankCapacity => tankCapacity;
        public float CurrentNutrients => currentNutrients;
        public string InteractionPrompt => currentNutrients < tankCapacity - 0.01f ? "Refill nutrient doser" : IsOperational ? "Turn off nutrient doser" : "Turn on nutrient doser";
        public string ContextualInfo => $"Nutrient Doser\nStatus: {(IsOperational ? (currentNutrients > 0f ? "Active" : "Supply Empty") : "Off")}\nCurrent Nutrients: {currentNutrients:0} / {tankCapacity:0}\nCoverage: {CoverageRadius:0.0} m";

        private void Update()
        {
            timer -= Time.deltaTime;
            if (!IsOperational || currentNutrients <= 0f)
            {
                IsDrawingPower = false;
                if (IsOperational && currentNutrients <= 0f && !emptyNotificationShown)
                {
                    GameplayMessageUI.Show("Nutrient Doser supply is empty.");
                    emptyNotificationShown = true;
                }
                return;
            }
            if (timer > 0f) return;

            timer = cycleIntervalSeconds;
            bool serviced = false;
            foreach (PlantInstance plant in FindObjectsByType<PlantInstance>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (plant == null || plant.Definition == null || !Covers(plant.transform.position) || currentNutrients <= 0f) continue;
                float target = plant.Definition.MaximumNutrients * targetNutrientFraction;
                if (plant.NutrientLevel >= target) continue;
                float amount = Mathf.Min(nutrientUnitsPerPlantCycle, target - plant.NutrientLevel, currentNutrients);
                plant.AddNutrients(amount);
                currentNutrients -= amount;
                serviced = amount > 0f;
            }
            IsDrawingPower = serviced;
        }

        public bool CanInteract(GameObject interactor) => true;
        public string GetInteractionPrompt(GameObject interactor)
        {
            if (interactor != null && interactor.TryGetComponent(out PlayerInventory inventory)
                && nutrientItem != null && inventory.Count(nutrientItem) > 0 && currentNutrients < tankCapacity - 0.01f)
            {
                return "Refill nutrient doser";
            }
            return IsOperational ? "Turn off nutrient doser" : "Turn on nutrient doser";
        }

        public void Interact(GameObject interactor)
        {
            if (interactor != null && interactor.TryGetComponent(out PlayerInventory inventory)
                && nutrientItem != null && inventory.Count(nutrientItem) > 0 && currentNutrients < tankCapacity - 0.01f
                && inventory.Remove(nutrientItem, 1))
            {
                currentNutrients = Mathf.Min(tankCapacity, currentNutrients + unitsPerInventoryItem);
                emptyNotificationShown = false;
                GameplayMessageUI.Show($"Nutrient Doser refilled: {currentNutrients:0} / {tankCapacity:0}");
                return;
            }
            ToggleOperational();
        }

        public override void RestoreAutomation(bool restoredOperational, float storedResource)
        {
            base.RestoreAutomation(restoredOperational, storedResource);
            currentNutrients = Mathf.Clamp(storedResource, 0f, tankCapacity);
            emptyNotificationShown = currentNutrients <= 0f;
        }

        public static NutrientDoser FindCovering(Vector3 position)
        {
            foreach (AutomationEquipment equipment in GetActiveEquipment())
            {
                if (equipment is NutrientDoser doser && doser.IsOperational && doser.currentNutrients > 0f && doser.Covers(position)) return doser;
            }
            return null;
        }
    }
}
