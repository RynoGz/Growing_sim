using UnityEngine;
using UnityEngine.InputSystem;

namespace Growveld.Inventory
{
    /// <summary>
    /// Selects the first five inventory slots with number keys or the mouse wheel.
    /// </summary>
    [RequireComponent(typeof(PlayerInventory))]
    public sealed class InventoryHotbarInput : MonoBehaviour
    {
        [SerializeField, Range(1, 10)] private int visibleSlotCount = 5;

        private PlayerInventory inventory;

        private void Awake()
        {
            inventory = GetComponent<PlayerInventory>();
        }

        private void Update()
        {
            if (Keyboard.current != null)
            {
                if (Keyboard.current.digit1Key.wasPressedThisFrame) inventory.SelectSlot(0);
                if (Keyboard.current.digit2Key.wasPressedThisFrame) inventory.SelectSlot(1);
                if (Keyboard.current.digit3Key.wasPressedThisFrame) inventory.SelectSlot(2);
                if (Keyboard.current.digit4Key.wasPressedThisFrame) inventory.SelectSlot(3);
                if (Keyboard.current.digit5Key.wasPressedThisFrame) inventory.SelectSlot(4);
            }

            if (Mouse.current == null)
            {
                return;
            }

            float scroll = Mouse.current.scroll.ReadValue().y;
            if (Mathf.Abs(scroll) < 0.01f)
            {
                return;
            }

            int slotCount = Mathf.Min(visibleSlotCount, inventory.Capacity);
            int direction = scroll > 0f ? -1 : 1;
            int nextIndex = (inventory.SelectedSlotIndex + direction + slotCount) % slotCount;
            inventory.SelectSlot(nextIndex);
        }
    }
}
