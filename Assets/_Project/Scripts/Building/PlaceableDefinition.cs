using Growveld.Inventory;
using UnityEngine;

namespace Growveld.Building
{
    /// <summary>
    /// Construction-specific data for an inventory item that can become a world object.
    /// </summary>
    [CreateAssetMenu(menuName = "Growveld/Building/Placeable Definition", fileName = "Placeable_")]
    public sealed class PlaceableDefinition : ScriptableObject
    {
        [SerializeField] private string placeableId;
        [SerializeField] private ItemDefinition itemDefinition;
        [SerializeField] private GameObject prefab;
        [SerializeField] private Vector3 footprintSize = Vector3.one;
        [SerializeField] private Vector3 placementOffset;
        [SerializeField, Range(1f, 90f)] private float rotationStep = 15f;
        [SerializeField, Range(0f, 1f)] private float sellRefundFraction = 0.7f;
        [SerializeField, Min(0f)] private float lightCoverageRadius;

        public string PlaceableId => placeableId;
        public ItemDefinition ItemDefinition => itemDefinition;
        public GameObject Prefab => prefab;
        public Vector3 FootprintSize => footprintSize;
        public Vector3 PlacementOffset => placementOffset;
        public float RotationStep => rotationStep;
        public float SellRefundFraction => sellRefundFraction;
        public float LightCoverageRadius => lightCoverageRadius;
        public float PurchasePrice => itemDefinition != null ? itemDefinition.PurchasePrice : 0f;
        public string DisplayName => itemDefinition != null ? itemDefinition.DisplayName : name;
    }
}
