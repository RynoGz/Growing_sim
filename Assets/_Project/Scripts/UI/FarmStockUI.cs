using System.Text;
using Growveld.Economy;
using Growveld.Farming;
using UnityEngine;
using UnityEngine.UI;

namespace Growveld.UI
{
    public sealed class FarmStockUI : MonoBehaviour
    {
        [SerializeField] private FarmStockManager farmStock;
        [SerializeField] private Growveld.Farming.QualitySettings qualitySettings;
        [SerializeField] private Text stockText;

        private void OnEnable()
        {
            if (farmStock != null) farmStock.StockChanged += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            if (farmStock != null) farmStock.StockChanged -= Refresh;
        }

        private void Refresh()
        {
            if (farmStock == null || stockText == null) return;
            StringBuilder builder = new("DRIED FARM STOCK\n\n");
            foreach (FarmStockEntry entry in farmStock.Entries)
            {
                if (entry == null || entry.WeightKilograms <= 0f) continue;
                string name = qualitySettings != null ? qualitySettings.GetDisplayName(entry.QualityGrade) : entry.QualityGrade.ToString();
                builder.AppendLine($"{entry.Strain?.DisplayName ?? "Northern Lights"}\n{name}: {entry.WeightKilograms:0.00} kg\n");
            }
            builder.AppendLine($"\nTotal: {farmStock.TotalKilograms:0.00} kg");
            stockText.text = builder.ToString();
        }
    }
}
