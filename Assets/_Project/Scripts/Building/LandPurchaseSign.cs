using Growveld.Interaction;
using UnityEngine;

namespace Growveld.Building
{
    /// <summary>
    /// Raycast target that purchases its associated plot through the LandManager.
    /// </summary>
    public sealed class LandPurchaseSign : MonoBehaviour, IInteractable
    {
        [SerializeField] private LandPlot plot;
        [SerializeField] private LandManager landManager;

        public string InteractionPrompt => plot == null
            ? "Purchase land"
            : $"Buy {plot.DisplayName} for R{plot.PurchasePrice:N0}";

        public bool CanInteract(GameObject interactor)
        {
            return landManager != null && landManager.CanPurchase(plot);
        }

        public void Interact(GameObject interactor)
        {
            landManager?.TryPurchase(plot);
        }
    }
}
