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
        private float refreshTimer;

        public GrowRoomEnvironment CurrentRoom => currentRoom;
        public bool IsIndoor => currentRoom != null;
        public string ContextSummary => currentRoom == null
            ? "Environment: Uncontrolled"
            : $"Environment: Indoor\nHumidity: {currentRoom.HumidityStatus}";

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
            float multiplier = currentRoom != null ? currentRoom.GetHumidityGrowthMultiplier() : 1f;
            plant.SetExternalGrowthMultiplier(multiplier);
        }
    }
}
