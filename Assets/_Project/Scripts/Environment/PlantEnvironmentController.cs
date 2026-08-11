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
        private OutdoorEnvironment outdoorEnvironment;
        private float refreshTimer;

        public GrowRoomEnvironment CurrentRoom => currentRoom;
        public bool IsIndoor => currentRoom != null;
        public GrowLight CoveringLight => coveringLight;
        public float QualityFactor
        {
            get
            {
                if (currentRoom != null)
                {
                    float humidityFactor = currentRoom.GetHumidityGrowthMultiplier() / 1.12f;
                    return Mathf.Clamp01(humidityFactor) * (coveringLight != null ? 1f : 0.5f);
                }

                if (outdoorEnvironment != null)
                {
                    float growthFactor = outdoorEnvironment.IsDaylight
                        ? outdoorEnvironment.GetGrowthMultiplier() / 0.78f
                        : 1f;
                    return 0.86f * Mathf.Clamp01(growthFactor);
                }

                return 0.8f;
            }
        }
        public string ContextSummary
        {
            get
            {
                if (currentRoom != null)
                {
                    return $"Environment: Indoor\nHumidity: {currentRoom.HumidityStatus}\nGrow light: {(coveringLight != null ? "Active" : "No coverage")}";
                }

                if (outdoorEnvironment != null)
                {
                    return $"Environment: Outdoor\nDaylight: {(outdoorEnvironment.IsDaylight ? "Yes" : "Night")}\nHumidity: {outdoorEnvironment.HumidityStatus}";
                }

                return "Environment: Uncontrolled";
            }
        }

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
            outdoorEnvironment = currentRoom == null ? OutdoorEnvironment.Current : null;
            float multiplier = currentRoom != null
                ? currentRoom.GetHumidityGrowthMultiplier() * (coveringLight != null ? 1.15f : 0f)
                : outdoorEnvironment != null
                    ? outdoorEnvironment.GetGrowthMultiplier()
                    : 1f;
            plant.SetExternalGrowthMultiplier(multiplier);
        }
    }
}
