using UnityEngine;

namespace Growveld.Economy
{
    [CreateAssetMenu(menuName = "Growveld/Economy/Selling Settings", fileName = "SellingSettings")]
    public sealed class SellingSettings : ScriptableObject
    {
        [SerializeField, Min(0f)] private float basePricePerKilogram = 1000f;

        public float BasePricePerKilogram => basePricePerKilogram;
    }
}
