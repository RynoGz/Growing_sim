using UnityEngine;

namespace Growveld.Core
{
    [CreateAssetMenu(menuName = "Growveld/Core/Time Settings", fileName = "TimeSettings")]
    public sealed class TimeSettings : ScriptableObject
    {
        [SerializeField, Min(60f)] private float realSecondsPerFullDay = 1800f;
        [SerializeField, Range(0f, 24f)] private float sunriseHour = 6f;
        [SerializeField, Range(0f, 24f)] private float sunsetHour = 22f;
        [SerializeField, Range(0f, 24f)] private float growLightOnHour = 4f;
        [SerializeField, Range(0f, 24f)] private float growLightOffHour = 22f;
        [SerializeField, Min(0f)] private float daytimeSunIntensity = 1.15f;
        [SerializeField, Min(0f)] private float nightSunIntensity = 0.03f;

        public float RealSecondsPerFullDay => realSecondsPerFullDay;
        public float SunriseHour => sunriseHour;
        public float SunsetHour => sunsetHour;
        public float GrowLightOnHour => growLightOnHour;
        public float GrowLightOffHour => growLightOffHour;
        public float DaytimeSunIntensity => daytimeSunIntensity;
        public float NightSunIntensity => nightSunIntensity;
    }
}
