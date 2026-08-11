using Growveld.Interaction;
using Growveld.Inventory;
using UnityEngine;

namespace Growveld.Economy
{
    /// <summary>
    /// Small Phase 6 purchase test. The full menu shop and delivery queue replace it in Phase 9.
    /// </summary>
    public sealed class QuickPurchaseInteractable : MonoBehaviour, IInteractable
    {
        [SerializeField] private ItemDefinition item;
        [SerializeField, Min(1)] private int quantity = 1;
        [SerializeField] private EconomyManager economy;

        public string InteractionPrompt => item == null
            ? "Purchase item"
            : $"Buy {item.DisplayName} for R{item.PurchasePrice * quantity:N0}";

        public bool CanInteract(GameObject interactor)
        {
            float totalPrice = item == null ? 0f : item.PurchasePrice * quantity;
            return item != null
                && economy != null
                && economy.CanAfford(totalPrice)
                && interactor.TryGetComponent(out PlayerInventory inventory)
                && inventory.CanAdd(item, quantity);
        }

        public void Interact(GameObject interactor)
        {
            if (!CanInteract(interactor)
                || !interactor.TryGetComponent(out PlayerInventory inventory))
            {
                return;
            }

            float totalPrice = item.PurchasePrice * quantity;
            if (!economy.TrySpend(totalPrice, $"Purchased {quantity} {item.DisplayName}"))
            {
                return;
            }

            if (!inventory.Add(item, quantity))
            {
                economy.Credit(totalPrice, "Purchase refund - inventory full");
            }
        }
    }
}
