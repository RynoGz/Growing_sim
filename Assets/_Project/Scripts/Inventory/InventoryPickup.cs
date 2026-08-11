using Growveld.Interaction;
using UnityEngine;

namespace Growveld.Inventory
{
    /// <summary>
    /// Simple world pickup used to verify small-item inventory behaviour.
    /// </summary>
    public sealed class InventoryPickup : MonoBehaviour, IInteractable
    {
        [SerializeField] private ItemDefinition item;
        [SerializeField, Min(1)] private int quantity = 1;

        public string InteractionPrompt => item == null
            ? "Pick up item"
            : $"Pick up {quantity} {item.DisplayName}";

        public bool CanInteract(GameObject interactor)
        {
            return item != null
                && interactor.TryGetComponent(out PlayerInventory inventory)
                && inventory.CanAdd(item, quantity);
        }

        public void Interact(GameObject interactor)
        {
            if (item != null
                && interactor.TryGetComponent(out PlayerInventory inventory)
                && inventory.Add(item, quantity))
            {
                Destroy(gameObject);
            }
        }
    }
}
