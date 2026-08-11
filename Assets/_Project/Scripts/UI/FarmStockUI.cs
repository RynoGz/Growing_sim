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
            foreach (QualityGrade grade in System.Enum.GetValues(typeof(QualityGrade)))
            {
                string name = qualitySettings != null ? qualitySettings.GetDisplayName(grade) : grade.ToString();
                builder.AppendLine($"{name}: {farmStock.GetWeight(grade):0.00} kg");
            }
            builder.AppendLine($"\nTotal: {farmStock.TotalKilograms:0.00} kg");
            stockText.text = builder.ToString();
        }
    }
}
