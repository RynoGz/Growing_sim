using System.Text;
using Growveld.Economy;
using UnityEngine;
using UnityEngine.UI;

namespace Growveld.UI
{
    public sealed class UtilitiesUI : MonoBehaviour
    {
        [SerializeField] private UtilityManager utilities;
        [SerializeField] private Text utilityText;

        private void OnEnable()
        {
            if (utilities != null)
            {
                utilities.UsageChanged += Refresh;
                utilities.BillProcessed += HandleBill;
            }
            Refresh();
        }

        private void OnDisable()
        {
            if (utilities != null)
            {
                utilities.UsageChanged -= Refresh;
                utilities.BillProcessed -= HandleBill;
            }
        }

        private void Refresh()
        {
            if (utilities == null || utilityText == null) return;
            StringBuilder builder = new("CURRENT DAY USAGE\n\n");
            builder.AppendLine($"Electricity: {utilities.CurrentElectricityKilowattHours:0.000} kWh  |  R{utilities.CurrentElectricityCost:0.00}");
            builder.AppendLine($"Water: {utilities.CurrentWaterLitres:0.0} L  |  R{utilities.CurrentWaterCost:0.00}");
            builder.AppendLine($"Current total: R{utilities.CurrentElectricityCost + utilities.CurrentWaterCost:0.00}");

            DailyUtilityBill bill = utilities.LastBill;
            if (bill != null)
            {
                builder.AppendLine($"\nLAST DAILY BILL - DAY {bill.Day}");
                builder.AppendLine($"Electricity: R{bill.ElectricityCost:0.00}");
                builder.AppendLine($"Water: R{bill.WaterCost:0.00}");
                builder.AppendLine($"Total deducted: R{bill.TotalCost:0.00}");
            }
            utilityText.text = builder.ToString();
        }

        private void HandleBill(DailyUtilityBill bill)
        {
            Refresh();
        }
    }
}
