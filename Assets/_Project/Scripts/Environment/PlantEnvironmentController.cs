using Growveld.Farming;
using UnityEngine;

namespace Growveld.Environment
{
    /// <summary>
    /// Resolves the environment around one plant and supplies its environmental growth multiplier.
    /// </summary>
    [RequireComponent(typeof(PlantInstance))]
    public sealed class PlantEnvironmentController : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float refreshInterval = 0.5f;

        private PlantInstance plant;
        private GrowRoomEnvironment currentRoom;
        private GrowLight coveringLight;
        private float refreshTimer;

        public GrowRoomEnvironment CurrentRoom => currentRoom;
        public bool IsIndoor => currentRoom != null;
        public GrowLight CoveringLight => coveringLight;
        public string ContextSummary => currentRoom == null
            ? "Environment: Uncontrolled"
            : $"Environment: Indoor\nHumidity: {currentRoom.HumidityStatus}\nGrow light: {(coveringLight != null ? "Active" : "No coverage")}";

        private void Awake()
        {
            plant = GetComponent<PlantInstance>();
        }

        private void Update()
        {
            refreshTimer -= Time.deltaTime;
            if (refreshTimer > 0f)
            {
                return;
            }

            refreshTimer = refreshInterval;
            currentRoom = GrowRoomEnvironment.FindContainingRoom(transform.position);
            coveringLight = currentRoom != null
                ? GrowLight.FindCoveringLight(transform.position, currentRoom)
                : null;
            float multiplier = currentRoom != null
                ? currentRoom.GetHumidityGrowthMultiplier() * (coveringLight != null ? 1.15f : 0f)
                : 1f;
            plant.SetExternalGrowthMultiplier(multiplier);
        }
    }
}
