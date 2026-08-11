using System;
using Growveld.Inventory;
using UnityEngine;

namespace Growveld.Economy
{
    [Serializable]
    public sealed class PendingDelivery
    {
        [SerializeField] private string deliveryId;
        [SerializeField] private ItemDefinition item;
        [SerializeField, Min(1)] private int quantity;
        [SerializeField, Min(0f)] private float remainingGameMinutes;

        public string DeliveryId => deliveryId;
        public ItemDefinition Item => item;
        public int Quantity => quantity;
        public float RemainingGameMinutes => remainingGameMinutes;

        public PendingDelivery(ItemDefinition item, int quantity, float remainingGameMinutes)
        {
            deliveryId = Guid.NewGuid().ToString("N");
            this.item = item;
            this.quantity = Mathf.Max(1, quantity);
            this.remainingGameMinutes = Mathf.Max(0f, remainingGameMinutes);
        }

        public void Advance(float elapsedGameMinutes)
        {
            remainingGameMinutes = Mathf.Max(0f, remainingGameMinutes - Mathf.Max(0f, elapsedGameMinutes));
        }

        public void RestoreRemainingMinutes(float minutes)
        {
            remainingGameMinutes = Mathf.Max(0f, minutes);
        }
    }
}
