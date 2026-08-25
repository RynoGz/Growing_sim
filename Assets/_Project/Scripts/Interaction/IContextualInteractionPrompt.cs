using UnityEngine;

namespace Growveld.Interaction
{
    /// <summary>
    /// Supplies an interaction label that depends on the player and current object state.
    /// </summary>
    public interface IContextualInteractionPrompt
    {
        string GetInteractionPrompt(GameObject interactor);
    }
}
