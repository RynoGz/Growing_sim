using System;
using Growveld.Economy;
using UnityEngine;

namespace Growveld.Building
{
    /// <summary>
    /// Central ownership query used by construction and planting.
    /// </summary>
    public sealed class LandManager : MonoBehaviour
    {
        [SerializeField] private EconomyManager economy;
        [SerializeField] private LandPlot[] plots;

        public event Action<LandPlot> PlotPurchased;

        public LandPlot[] Plots => plots;

        public bool CanPurchase(LandPlot plot)
        {
            return plot != null
                && !plot.IsOwned
                && economy != null
                && economy.CanAfford(plot.PurchasePrice);
        }

        public bool TryPurchase(LandPlot plot)
        {
            if (!CanPurchase(plot)
                || !economy.TrySpend(plot.PurchasePrice, $"Purchased {plot.DisplayName}"))
            {
                return false;
            }

            plot.SetOwned(true);
            PlotPurchased?.Invoke(plot);
            return true;
        }

        public bool IsPositionOwned(Vector3 worldPosition)
        {
            if (plots == null)
            {
                return false;
            }

            foreach (LandPlot plot in plots)
            {
                if (plot != null && plot.IsOwned && plot.ContainsPoint(worldPosition))
                {
                    return true;
                }
            }

            return false;
        }

        public bool IsFootprintOwned(Bounds worldFootprint)
        {
            if (plots == null)
            {
                return false;
            }

            foreach (LandPlot plot in plots)
            {
                if (plot != null && plot.IsOwned && plot.ContainsFootprint(worldFootprint))
                {
                    return true;
                }
            }

            return false;
        }

        public void SetConstructionBoundariesVisible(bool visible)
        {
            if (plots == null)
            {
                return;
            }

            foreach (LandPlot plot in plots)
            {
                plot?.SetConstructionHighlight(visible);
            }
        }

        public LandPlot FindPlot(string plotId)
        {
            if (plots == null) return null;
            foreach (LandPlot plot in plots)
            {
                if (plot != null && plot.PlotId == plotId) return plot;
            }
            return null;
        }
    }
}
