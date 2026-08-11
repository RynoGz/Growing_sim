using System.Text;
using Growveld.Economy;
using UnityEngine;
using UnityEngine.UI;

namespace Growveld.UI
{
    public sealed class DeliveryHUD : MonoBehaviour
    {
        [SerializeField] private DeliveryManager deliveryManager;
        [SerializeField] private ShopManager shopManager;
        [SerializeField] private Text deliveryStatusText;
        [SerializeField] private Text notificationText;

        private float refreshTimer;
        private float notificationRemaining;

        private void OnEnable()
        {
            if (deliveryManager != null)
            {
                deliveryManager.DeliveriesChanged += Refresh;
                deliveryManager.DeliveryMessage += ShowNotification;
            }
            if (shopManager != null) shopManager.OrderResult += ShowOrderResult;
            Refresh();
        }

        private void OnDisable()
        {
            if (deliveryManager != null)
            {
                deliveryManager.DeliveriesChanged -= Refresh;
                deliveryManager.DeliveryMessage -= ShowNotification;
            }
            if (shopManager != null) shopManager.OrderResult -= ShowOrderResult;
        }

        private void Update()
        {
            refreshTimer -= Time.deltaTime;
            if (refreshTimer <= 0f)
            {
                refreshTimer = 0.25f;
                Refresh();
            }

            if (notificationText != null && notificationText.gameObject.activeSelf)
            {
                notificationRemaining -= Time.deltaTime;
                if (notificationRemaining <= 0f) notificationText.gameObject.SetActive(false);
            }
        }

        private void Refresh()
        {
            if (deliveryStatusText == null || deliveryManager == null) return;
            if (deliveryManager.PendingDeliveries.Count == 0)
            {
                deliveryStatusText.text = string.Empty;
                return;
            }

            StringBuilder builder = new("Deliveries:\n");
            int shown = 0;
            foreach (PendingDelivery delivery in deliveryManager.PendingDeliveries)
            {
                builder.AppendLine($"{delivery.Item.DisplayName} x{delivery.Quantity}: {delivery.RemainingGameMinutes:0.0} min");
                if (++shown >= 3) break;
            }
            deliveryStatusText.text = builder.ToString();
        }

        private void ShowOrderResult(string message, bool success)
        {
            ShowNotification(message);
            if (notificationText != null)
            {
                notificationText.color = success ? new Color(0.55f, 1f, 0.62f) : new Color(1f, 0.48f, 0.4f);
            }
        }

        private void ShowNotification(string message)
        {
            if (notificationText == null) return;
            notificationText.text = message;
            notificationText.gameObject.SetActive(true);
            notificationRemaining = 4f;
        }
    }
}
