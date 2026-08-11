using UnityEngine;

namespace Growveld.Economy
{
    [CreateAssetMenu(menuName = "Growveld/Economy/Utility Settings", fileName = "UtilitySettings")]
    public sealed class UtilitySettings : ScriptableObject
    {
        [SerializeField, Min(0f)] private float electricityRandPerKilowattHour = 3.25f;
        [SerializeField, Min(0f)] private float waterLitresPerWatering = 15f;
        [SerializeField, Min(0f)] private float waterRandPerLitre = 0.06f;
        [SerializeField, Min(1f)] private float fallbackDayRealSeconds = 1800f;

        public float ElectricityRandPerKilowattHour => electricityRandPerKilowattHour;
        public float WaterLitresPerWatering => waterLitresPerWatering;
        public float WaterRandPerLitre => waterRandPerLitre;
        public float FallbackDayRealSeconds => fallbackDayRealSeconds;
    }
}
