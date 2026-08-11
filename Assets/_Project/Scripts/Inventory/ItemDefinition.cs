using Growveld.Building;
using UnityEngine;

namespace Growveld.Inventory
{
    public enum ItemCategory
    {
        Seeds,
        Nutrients,
        Equipment,
        Watering,
        Drying,
        Storage,
        Building
    }

    /// <summary>
    /// Configurable data shared by every instance of an inventory item.
    /// </summary>
    [CreateAssetMenu(menuName = "Growveld/Inventory/Item Definition", fileName = "Item_")]
    public sealed class ItemDefinition : ScriptableObject
    {
        [SerializeField] private string itemId;
        [SerializeField] private string displayName;
        [SerializeField, TextArea] private string description;
        [SerializeField] private ItemCategory category;
        [SerializeField] private bool stackable = true;
        [SerializeField, Min(1)] private int maximumStack = 20;
        [SerializeField, Min(0f)] private float purchasePrice;
        [SerializeField] private Color displayColor = Color.white;
        [SerializeField] private Sprite icon;
        [SerializeField] private PlaceableDefinition placeableDefinition;

        public string ItemId => itemId;
        public string DisplayName => displayName;
        public string Description => description;
        public ItemCategory Category => category;
        public bool Stackable => stackable;
        public int MaximumStack => stackable ? Mathf.Max(1, maximumStack) : 1;
        public float PurchasePrice => purchasePrice;
        public Color DisplayColor => displayColor;
        public Sprite Icon => icon;
        public PlaceableDefinition PlaceableDefinition => placeableDefinition;
    }
}
