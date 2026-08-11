using System.Collections.Generic;
using UnityEngine;

namespace Growveld.Environment
{
    /// <summary>
    /// One shared humidity value and containment volume for an enclosed grow room.
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public sealed class GrowRoomEnvironment : MonoBehaviour
    {
        private static readonly List<GrowRoomEnvironment> ActiveRooms = new();

        [SerializeField] private string roomId;
        [SerializeField] private string displayName = "Grow Room";
        [SerializeField, Range(0f, 100f)] private float humidity = 60f;
        [SerializeField, Range(0f, 100f)] private float ambientHumidity = 45f;
        [SerializeField, Range(0f, 100f)] private float idealMinimumHumidity = 55f;
        [SerializeField, Range(0f, 100f)] private float idealMaximumHumidity = 65f;
        [SerializeField, Min(0f)] private float driftPerRealMinute = 0.2f;

        private BoxCollider roomVolume;

        public string RoomId => roomId;
        public string DisplayName => displayName;
        public float Humidity => humidity;
        public string HumidityStatus
        {
            get
            {
                if (humidity < idealMinimumHumidity) return "Low";
                if (humidity > idealMaximumHumidity) return "High";
                return "Ideal";
            }
        }

        private void Awake()
        {
            roomVolume = GetComponent<BoxCollider>();
            roomVolume.isTrigger = true;
        }

        private void OnEnable()
        {
            if (!ActiveRooms.Contains(this)) ActiveRooms.Add(this);
        }

        private void OnDisable()
        {
            ActiveRooms.Remove(this);
        }

        private void Update()
        {
            float maximumChange = driftPerRealMinute * (Time.deltaTime / 60f);
            humidity = Mathf.MoveTowards(humidity, ambientHumidity, maximumChange);
        }

        public bool Contains(Vector3 worldPosition)
        {
            if (roomVolume == null) roomVolume = GetComponent<BoxCollider>();
            return roomVolume != null && roomVolume.bounds.Contains(worldPosition);
        }

        public float GetHumidityGrowthMultiplier()
        {
            if (humidity >= idealMinimumHumidity && humidity <= idealMaximumHumidity)
            {
                return 1.12f;
            }

            float distanceFromIdeal = humidity < idealMinimumHumidity
                ? idealMinimumHumidity - humidity
                : humidity - idealMaximumHumidity;
            return Mathf.Lerp(1f, 0.5f, Mathf.Clamp01(distanceFromIdeal / 30f));
        }

        public void SetHumidity(float newHumidity)
        {
            humidity = Mathf.Clamp(newHumidity, 0f, 100f);
        }

        public static GrowRoomEnvironment FindContainingRoom(Vector3 worldPosition)
        {
            foreach (GrowRoomEnvironment room in ActiveRooms)
            {
                if (room != null && room.isActiveAndEnabled && room.Contains(worldPosition))
                {
                    return room;
                }
            }

            return null;
        }
    }
}
