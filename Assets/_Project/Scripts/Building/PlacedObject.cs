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
            ? "Move placed object"
            : $"Move {definition.DisplayName}";

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
                || !interactor.TryGetComponent(out PlacementController placementController)
                || placementController.IsPlacing)
            {
                return false;
            }

            foreach (MonoBehaviour behaviour in GetComponents<MonoBehaviour>())
            {
                if (behaviour != this
                    && behaviour is IInteractable otherInteraction
                    && otherInteraction.CanInteract(interactor))
                {
                    return false;
                }
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
