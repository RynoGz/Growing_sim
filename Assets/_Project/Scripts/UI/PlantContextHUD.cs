using Growveld.Interaction;
using UnityEngine;
using UnityEngine.UI;

namespace Growveld.UI
{
    /// <summary>
    /// Displays detailed information supplied by the interactable under the raycast.
    /// </summary>
    public sealed class PlantContextHUD : MonoBehaviour
    {
        [SerializeField] private PlayerInteractor interactor;
        [SerializeField] private CanvasGroup panelGroup;
        [SerializeField] private Text detailsText;

        private void Update()
        {
            if (interactor != null && interactor.CurrentInteractable is IContextualInfoProvider provider)
            {
                if (detailsText != null) detailsText.text = provider.ContextualInfo;
                SetVisible(true);
            }
            else
            {
                SetVisible(false);
            }
        }

        private void SetVisible(bool visible)
        {
            if (panelGroup == null) return;
            panelGroup.alpha = visible ? 1f : 0f;
            panelGroup.interactable = false;
            panelGroup.blocksRaycasts = false;
        }
    }
}
