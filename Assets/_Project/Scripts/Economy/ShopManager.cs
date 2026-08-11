using System;
using Growveld.Inventory;
using UnityEngine;

namespace Growveld.Economy
{
    /// <summary>
    /// Validates menu-shop purchases, takes payment, and queues delivery.
    /// </summary>
    public sealed class ShopManager : MonoBehaviour
    {
        [SerializeField] private EconomyManager economy;
        [SerializeField] private DeliveryManager deliveryManager;
        [SerializeField] private ItemDefinition[] availableItems;

        public event Action<string, bool> OrderResult;

        public ItemDefinition[] AvailableItems => availableItems;

        public bool TryOrder(ItemDefinition item, int quantity = 1)
        {
            if (item == null || quantity <= 0 || Array.IndexOf(availableItems, item) < 0)
            {
                OrderResult?.Invoke("That item is not available in this shop.", false);
                return false;
            }

            float totalPrice = item.PurchasePrice * quantity;
            if (economy == null || !economy.TrySpend(totalPrice, $"Ordered {quantity} {item.DisplayName}"))
            {
                OrderResult?.Invoke("Purchase blocked: insufficient funds or negative balance.", false);
                return false;
            }

            if (deliveryManager == null || deliveryManager.QueueDelivery(item, quantity) == null)
            {
                economy.Credit(totalPrice, "Order refund");
                OrderResult?.Invoke("The delivery could not be queued. Your money was refunded.", false);
                return false;
            }

            OrderResult?.Invoke($"Ordered {quantity} {item.DisplayName} for R{totalPrice:N0}.", true);
            return true;
        }
    }
}
