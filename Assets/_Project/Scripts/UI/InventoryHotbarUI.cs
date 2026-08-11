using Growveld.Inventory;
using UnityEngine;
using UnityEngine.UI;

namespace Growveld.UI
{
    /// <summary>
    /// Minimal five-slot hotbar and selected-item readout.
    /// </summary>
    public sealed class InventoryHotbarUI : MonoBehaviour
    {
        [SerializeField] private PlayerInventory inventory;
        [SerializeField] private Text[] slotLabels;
        [SerializeField] private Text heldItemLabel;

        private void OnEnable()
        {
            if (inventory != null)
            {
                inventory.InventoryChanged += Refresh;
                inventory.SelectionChanged += HandleSelectionChanged;
            }

            Refresh();
        }

        private void OnDisable()
        {
            if (inventory != null)
            {
                inventory.InventoryChanged -= Refresh;
                inventory.SelectionChanged -= HandleSelectionChanged;
            }
        }

        private void HandleSelectionChanged(int _)
        {
            Refresh();
        }

        private void Refresh()
        {
            if (inventory == null || slotLabels == null)
            {
                return;
            }

            for (int index = 0; index < slotLabels.Length; index++)
            {
                if (slotLabels[index] == null)
                {
                    continue;
                }

                int slotIndex = inventory.GetHotbarSlotIndex(index, slotLabels.Length);
                InventorySlot slot = slotIndex >= 0 ? inventory.Slots[slotIndex] : null;
                string itemText = slot == null || slot.IsEmpty
                    ? "Empty"
                    : $"{slot.Item.DisplayName}\nx{slot.Quantity}";
                string selectionMarker = slotIndex == inventory.SelectedSlotIndex ? ">" : string.Empty;
                slotLabels[index].text = $"{selectionMarker}[{index + 1}]\n{itemText}";
            }

            if (heldItemLabel != null)
            {
                InventorySlot selectedSlot = inventory.SelectedSlot;
                heldItemLabel.text = selectedSlot == null || selectedSlot.IsEmpty || selectedSlot.Item.PlaceableDefinition != null
                    ? "Selected: Empty"
                    : $"Selected: {selectedSlot.Item.DisplayName} x{selectedSlot.Quantity}";
            }
        }
    }
}
