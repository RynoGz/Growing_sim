using Growveld.Building;
using UnityEngine;
using UnityEngine.UI;

namespace Growveld.UI
{
    public sealed class PlacementHUD : MonoBehaviour
    {
        [SerializeField] private PlacementController placementController;
        [SerializeField] private CanvasGroup panelGroup;
        [SerializeField] private Text detailsText;

        private void OnEnable()
        {
            if (placementController != null)
            {
                placementController.PlacementModeChanged += SetVisible;
                placementController.PreviewChanged += UpdateDetails;
            }
            SetVisible(placementController != null && placementController.IsPlacing);
        }

        private void OnDisable()
        {
            if (placementController != null)
            {
                placementController.PlacementModeChanged -= SetVisible;
                placementController.PreviewChanged -= UpdateDetails;
            }
        }

        private void SetVisible(bool visible)
        {
            if (panelGroup == null) return;
            panelGroup.alpha = visible ? 1f : 0f;
            panelGroup.blocksRaycasts = false;
            panelGroup.interactable = false;
        }

        private void UpdateDetails(PlaceableDefinition definition, bool valid, bool moving)
        {
            if (detailsText == null || definition == null) return;
            string priceLine = moving
                ? $"Moving free  |  Sell: R{definition.PurchasePrice * definition.SellRefundFraction:N0}"
                : $"Inventory item  |  Price: R{definition.PurchasePrice:N0}";
            string coverageLine = definition.LightCoverageRadius > 0f
                ? $"\nLight coverage: {definition.LightCoverageRadius:0.0} m"
                : string.Empty;
            detailsText.text = $"CONSTRUCTION MODE\n{definition.DisplayName}\n{priceLine}{coverageLine}\n{(valid ? "VALID - Left click to place" : "INVALID POSITION")}\nR rotate  |  Esc cancel  |  Delete sell while moving";
            detailsText.color = valid ? new Color(0.72f, 1f, 0.76f) : new Color(1f, 0.58f, 0.52f);
        }
    }
}
