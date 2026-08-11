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
        [SerializeField] private ConstructionModeController constructionMode;

        private void OnEnable()
        {
            if (placementController != null)
            {
                placementController.PlacementModeChanged += HandlePlacementModeChanged;
                placementController.PreviewChanged += UpdateDetails;
            }
            if (constructionMode != null) constructionMode.ModeChanged += HandleConstructionModeChanged;
            RefreshModeView();
        }

        private void OnDisable()
        {
            if (placementController != null)
            {
                placementController.PlacementModeChanged -= HandlePlacementModeChanged;
                placementController.PreviewChanged -= UpdateDetails;
            }
            if (constructionMode != null) constructionMode.ModeChanged -= HandleConstructionModeChanged;
        }

        private void SetVisible(bool visible)
        {
            if (panelGroup == null) return;
            panelGroup.alpha = visible ? 1f : 0f;
            panelGroup.blocksRaycasts = false;
            panelGroup.interactable = false;
        }

        private void HandleConstructionModeChanged(bool _)
        {
            RefreshModeView();
        }

        private void HandlePlacementModeChanged(bool placing)
        {
            if (placing) SetVisible(true);
            else RefreshModeView();
        }

        private void RefreshModeView()
        {
            bool active = constructionMode != null && constructionMode.IsActive;
            SetVisible(active);
            if (active && (placementController == null || !placementController.IsPlacing) && detailsText != null)
            {
                detailsText.text = "CONSTRUCTION MODE\n[E] Move selected object  |  [Delete] Sell\n[B] or [Esc] Exit";
                detailsText.color = new Color(0.72f, 1f, 0.76f);
            }
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
            string surfaceLine = definition.PlacementSurface == PlacementSurface.Ceiling
                ? "\nSurface: Ceiling only"
                : string.Empty;
            detailsText.text = $"CONSTRUCTION MODE\n{definition.DisplayName}\n{priceLine}{coverageLine}{surfaceLine}\n{(valid ? "VALID - Left click to place" : "INVALID POSITION")}" +
                "\nR rotate  |  Esc cancel and exit  |  Delete sell while moving";
            detailsText.color = valid ? new Color(0.72f, 1f, 0.76f) : new Color(1f, 0.58f, 0.52f);
        }
    }
}
