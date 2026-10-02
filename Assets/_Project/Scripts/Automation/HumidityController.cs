using Growveld.Environment;
using Growveld.Farming;
using Growveld.Interaction;
using UnityEngine;

namespace Growveld.Automation
{
    public enum HumidityControlMode { Humidify, Dehumidify }

    public sealed class HumidityController : AutomationEquipment, IInteractable, IContextualInfoProvider, IContextualInteractionPrompt
    {
        [SerializeField] private HumidityControlMode mode;
        [SerializeField, Range(0f, 100f)] private float fallbackTargetHumidity = 60f;
        [SerializeField, Min(0.1f)] private float changePerRealMinute = 8f;
        [SerializeField, Min(0f)] private float deadZone = 1f;

        private GrowRoomEnvironment room;
        private float targetHumidity;

        public HumidityControlMode Mode => mode;
        public float TargetHumidity => targetHumidity;
        public string InteractionPrompt => IsOperational ? $"Turn off {DisplayName}" : $"Turn on {DisplayName}";
        public string ContextualInfo => $"{DisplayName}\nStatus: {(IsOperational ? (IsDrawingPower ? "Adjusting" : "Target Reached") : "Off")}\nRoom: {(room != null ? room.DisplayName : "Not inside a grow room")}\nAutomatic target: {targetHumidity:0}%";
        private string DisplayName => mode == HumidityControlMode.Humidify ? "Humidifier" : "Dehumidifier";

        private void Update()
        {
            // Floor-mounted equipment can sit a few centimetres below the room trigger's
            // lower bound, so resolve the containing room from the device's physical centre.
            room = GrowRoomEnvironment.FindContainingRoom(transform.position + Vector3.up * 0.65f);
            targetHumidity = CalculateRecommendedTarget(room);
            if (!IsOperational || room == null)
            {
                IsDrawingPower = false;
                return;
            }

            float difference = targetHumidity - room.Humidity;
            bool shouldRun = mode == HumidityControlMode.Humidify
                ? difference > deadZone
                : difference < -deadZone;
            IsDrawingPower = shouldRun;
            if (!shouldRun) return;

            float direction = mode == HumidityControlMode.Humidify ? 1f : -1f;
            float maximumDelta = changePerRealMinute * Time.deltaTime / 60f;
            room.AdjustHumidity(direction * Mathf.Min(Mathf.Abs(difference), maximumDelta));
        }

        public bool CanInteract(GameObject interactor) => true;
        public string GetInteractionPrompt(GameObject interactor) => InteractionPrompt;
        public void Interact(GameObject interactor) => ToggleOperational();

        private float CalculateRecommendedTarget(GrowRoomEnvironment targetRoom)
        {
            if (targetRoom == null) return fallbackTargetHumidity;
            float sum = 0f;
            int count = 0;
            foreach (PlantInstance plant in FindObjectsByType<PlantInstance>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (plant?.Definition == null || !targetRoom.Contains(plant.transform.position)) continue;
                sum += (plant.Definition.PreferredHumidityMinimum + plant.Definition.PreferredHumidityMaximum) * 0.5f;
                count++;
            }
            return count > 0 ? sum / count : fallbackTargetHumidity;
        }
    }
}
