using Growveld.Interaction;
using UnityEngine;

namespace Growveld.Building
{
    /// <summary>
    /// Marks a constructed world object and lets the player move it for free.
    /// </summary>
    public sealed class PlacedObject : MonoBehaviour, IInteractable
    {
        [SerializeField] private PlaceableDefinition definition;
        [SerializeField] private string persistentId;

        public PlaceableDefinition Definition => definition;
        public string PersistentId => persistentId;
        public string InteractionPrompt => definition == null
            ? "Move placed object  |  [Delete] Sell"
            : $"Move {definition.DisplayName}  |  [Delete] Sell R{definition.PurchasePrice * definition.SellRefundFraction:N0}";

        private void Awake()
        {
            if (string.IsNullOrWhiteSpace(persistentId))
            {
                persistentId = System.Guid.NewGuid().ToString("N");
            }
        }

        public bool CanInteract(GameObject interactor)
        {
            if (definition == null
                || !interactor.TryGetComponent(out ConstructionModeController constructionMode)
                || !constructionMode.IsActive
                || !interactor.TryGetComponent(out PlacementController placementController)
                || placementController.IsPlacing)
            {
                return false;
            }
            return true;
        }

        public void Interact(GameObject interactor)
        {
            if (interactor.TryGetComponent(out PlacementController placementController))
            {
                placementController.BeginMove(this);
            }
        }

        public void RestorePersistentId(string restoredId)
        {
            persistentId = string.IsNullOrWhiteSpace(restoredId)
                ? System.Guid.NewGuid().ToString("N")
                : restoredId;
        }
    }
}
