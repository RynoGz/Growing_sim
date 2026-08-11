using System;
using System.Collections.Generic;
using Growveld.Inventory;
using UnityEngine;

namespace Growveld.Economy
{
    /// <summary>
    /// Advances paid orders and transfers completed deliveries into player inventory.
    /// </summary>
    public sealed class DeliveryManager : MonoBehaviour
    {
        [SerializeField] private PlayerInventory destinationInventory;
        [SerializeField, Min(0.1f)] private float deliveryDelayGameMinutes = 5f;
        [SerializeField, Min(0.05f)] private float realSecondsPerGameMinute = 1.25f;
        [SerializeField] private List<PendingDelivery> pendingDeliveries = new();

        public event Action DeliveriesChanged;
        public event Action<PendingDelivery> DeliveryCompleted;
        public event Action<string> DeliveryMessage;

        public IReadOnlyList<PendingDelivery> PendingDeliveries => pendingDeliveries;
        public float DeliveryDelayGameMinutes => deliveryDelayGameMinutes;

        private void Update()
        {
            if (pendingDeliveries.Count == 0 || destinationInventory == null)
            {
                return;
            }

            float elapsedGameMinutes = Time.deltaTime / realSecondsPerGameMinute;
            bool changed = false;

            for (int index = pendingDeliveries.Count - 1; index >= 0; index--)
            {
                PendingDelivery delivery = pendingDeliveries[index];
                delivery.Advance(elapsedGameMinutes);
                changed = true;

                if (delivery.RemainingGameMinutes > 0f)
                {
                    continue;
                }

                if (!destinationInventory.Add(delivery.Item, delivery.Quantity))
                {
                    continue;
                }

                pendingDeliveries.RemoveAt(index);
                DeliveryCompleted?.Invoke(delivery);
                DeliveryMessage?.Invoke($"Order delivered: {delivery.Quantity} {delivery.Item.DisplayName}");
            }

            if (changed)
            {
                DeliveriesChanged?.Invoke();
            }
        }

        public PendingDelivery QueueDelivery(ItemDefinition item, int quantity)
        {
            if (item == null || quantity <= 0)
            {
                return null;
            }

            PendingDelivery delivery = new(item, quantity, deliveryDelayGameMinutes);
            pendingDeliveries.Add(delivery);
            DeliveriesChanged?.Invoke();
            DeliveryMessage?.Invoke($"Order placed: {quantity} {item.DisplayName} - delivery in {deliveryDelayGameMinutes:0} game minutes");
            return delivery;
        }

        public void ClearAndRestore(IEnumerable<PendingDelivery> restoredDeliveries)
        {
            pendingDeliveries.Clear();
            if (restoredDeliveries != null)
            {
                pendingDeliveries.AddRange(restoredDeliveries);
            }
            DeliveriesChanged?.Invoke();
        }
    }
}
