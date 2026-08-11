using Growveld.Economy;
using UnityEngine;
using UnityEngine.UI;

namespace Growveld.UI
{
    public sealed class SellingUI : MonoBehaviour
    {
        [SerializeField] private SellingManager sellingManager;
        [SerializeField] private FarmStockManager farmStock;
        [SerializeField] private Text summaryText;
        [SerializeField] private Button sellAllButton;

        private string completedSaleMessage;

        private void Awake()
        {
            if (sellAllButton != null) sellAllButton.onClick.AddListener(SellAll);
        }

        private void OnEnable()
        {
            if (farmStock != null) farmStock.StockChanged += Refresh;
            if (sellingManager != null) sellingManager.SaleCompleted += HandleSaleCompleted;
            Refresh();
        }

        private void OnDisable()
        {
            if (farmStock != null) farmStock.StockChanged -= Refresh;
            if (sellingManager != null) sellingManager.SaleCompleted -= HandleSaleCompleted;
        }

        private void SellAll()
        {
            sellingManager?.SellAllStock();
        }

        private void HandleSaleCompleted(string summary, float total)
        {
            completedSaleMessage = summary;
            if (summaryText != null) summaryText.text = summary;
            if (sellAllButton != null) sellAllButton.interactable = false;
        }

        private void Refresh()
        {
            if (sellingManager == null || summaryText == null) return;
            if (farmStock != null && farmStock.TotalKilograms > 0f)
            {
                completedSaleMessage = null;
            }
            summaryText.text = string.IsNullOrWhiteSpace(completedSaleMessage)
                ? sellingManager.BuildProjectedSummary()
                : completedSaleMessage;
            if (sellAllButton != null)
            {
                sellAllButton.interactable = farmStock != null && farmStock.TotalKilograms > 0f;
            }
        }
    }
}
