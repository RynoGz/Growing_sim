using UnityEngine;

namespace Growveld.Interaction
{
    /// <summary>
    /// Implement this interface on any object the player can interact with.
    /// </summary>
    public interface IInteractable
    {
        string InteractionPrompt { get; }

        bool CanInteract(GameObject interactor);

        void Interact(GameObject interactor);
    }
}
