using System;
using UnityEngine;

namespace Growveld.Inventory
{
    /// <summary>
    /// One serializable inventory slot. Empty slots have no item and zero quantity.
    /// </summary>
    [Serializable]
    public sealed class InventorySlot
    {
        [SerializeField] private ItemDefinition item;
        [SerializeField, Min(0)] private int quantity;

        public ItemDefinition Item => item;
        public int Quantity => quantity;
        public bool IsEmpty => item == null || quantity <= 0;

        public void Set(ItemDefinition definition, int amount)
        {
            item = definition;
            quantity = definition == null ? 0 : Mathf.Max(0, amount);
        }

        public void Add(int amount)
        {
            quantity = Mathf.Max(0, quantity + amount);
        }

        public void Clear()
        {
            item = null;
            quantity = 0;
        }
    }
}
