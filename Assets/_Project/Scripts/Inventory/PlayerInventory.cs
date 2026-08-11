using System;
using System.Collections.Generic;
using UnityEngine;

namespace Growveld.Inventory
{
    /// <summary>
    /// Fixed-slot inventory for seeds, consumables, tools, and unplaced equipment.
    /// Physical harvest batches deliberately do not use this inventory.
    /// </summary>
    public sealed class PlayerInventory : MonoBehaviour
    {
        [SerializeField, Min(1)] private int capacity = 8;
        [SerializeField] private List<InventorySlot> slots = new();
        [SerializeField, Min(0)] private int selectedSlotIndex;

        public event Action InventoryChanged;
        public event Action<int> SelectionChanged;

        public IReadOnlyList<InventorySlot> Slots => slots;
        public int Capacity => capacity;
        public int SelectedSlotIndex => selectedSlotIndex;
        public InventorySlot SelectedSlot => selectedSlotIndex >= 0 && selectedSlotIndex < slots.Count
            ? slots[selectedSlotIndex]
            : null;

        private void Awake()
        {
            EnsureCapacity();
        }

        private void OnValidate()
        {
            capacity = Mathf.Max(1, capacity);
            EnsureCapacity();
        }

        public bool CanAdd(ItemDefinition item, int quantity = 1)
        {
            if (item == null || quantity <= 0)
            {
                return false;
            }

            EnsureCapacity();
            int availableSpace = 0;

            foreach (InventorySlot slot in slots)
            {
                if (slot.IsEmpty)
                {
                    availableSpace += item.MaximumStack;
                }
                else if (slot.Item == item && item.Stackable)
                {
                    availableSpace += Mathf.Max(0, item.MaximumStack - slot.Quantity);
                }

                if (availableSpace >= quantity)
                {
                    return true;
                }
            }

            return false;
        }

        public bool Add(ItemDefinition item, int quantity = 1)
        {
            if (!CanAdd(item, quantity))
            {
                return false;
            }

            int remaining = quantity;

            if (item.Stackable)
            {
                foreach (InventorySlot slot in slots)
                {
                    if (slot.Item != item || slot.Quantity >= item.MaximumStack)
                    {
                        continue;
                    }

                    int amountToAdd = Mathf.Min(remaining, item.MaximumStack - slot.Quantity);
                    slot.Add(amountToAdd);
                    remaining -= amountToAdd;

                    if (remaining <= 0)
                    {
                        break;
                    }
                }
            }

            while (remaining > 0)
            {
                InventorySlot emptySlot = slots.Find(slot => slot.IsEmpty);
                int amountToAdd = Mathf.Min(remaining, item.MaximumStack);
                emptySlot.Set(item, amountToAdd);
                remaining -= amountToAdd;
            }

            InventoryChanged?.Invoke();
            return true;
        }

        public bool Remove(ItemDefinition item, int quantity = 1)
        {
            if (item == null || quantity <= 0 || Count(item) < quantity)
            {
                return false;
            }

            int remaining = quantity;
            for (int index = slots.Count - 1; index >= 0 && remaining > 0; index--)
            {
                InventorySlot slot = slots[index];
                if (slot.Item != item)
                {
                    continue;
                }

                int amountToRemove = Mathf.Min(remaining, slot.Quantity);
                slot.Add(-amountToRemove);
                remaining -= amountToRemove;

                if (slot.Quantity <= 0)
                {
                    slot.Clear();
                }
            }

            InventoryChanged?.Invoke();
            return true;
        }

        public int Count(ItemDefinition item)
        {
            int total = 0;
            foreach (InventorySlot slot in slots)
            {
                if (slot.Item == item)
                {
                    total += slot.Quantity;
                }
            }

            return total;
        }

        public void SelectSlot(int index)
        {
            EnsureCapacity();
            int clampedIndex = Mathf.Clamp(index, 0, slots.Count - 1);
            if (selectedSlotIndex == clampedIndex)
            {
                return;
            }

            selectedSlotIndex = clampedIndex;
            SelectionChanged?.Invoke(selectedSlotIndex);
        }

        public void ClearAll()
        {
            EnsureCapacity();
            foreach (InventorySlot slot in slots)
            {
                slot.Clear();
            }

            InventoryChanged?.Invoke();
        }

        public void RestoreSlot(int index, ItemDefinition item, int quantity)
        {
            EnsureCapacity();
            if (index < 0 || index >= slots.Count)
            {
                return;
            }

            slots[index].Set(item, quantity);
        }

        public void NotifyRestored()
        {
            EnsureCapacity();
            InventoryChanged?.Invoke();
            SelectionChanged?.Invoke(selectedSlotIndex);
        }

        private void EnsureCapacity()
        {
            slots ??= new List<InventorySlot>();

            while (slots.Count < capacity)
            {
                slots.Add(new InventorySlot());
            }

            if (slots.Count > capacity)
            {
                slots.RemoveRange(capacity, slots.Count - capacity);
            }

            selectedSlotIndex = Mathf.Clamp(selectedSlotIndex, 0, slots.Count - 1);
        }
    }
}
