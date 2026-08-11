using UnityEngine;
using UnityEngine.UI;

namespace Growveld.UI
{
    /// <summary>
    /// Displays the short contextual prompt supplied by the current interactable.
    /// </summary>
    public sealed class InteractionPromptUI : MonoBehaviour
    {
        [SerializeField] private CanvasGroup promptGroup;
        [SerializeField] private Text promptText;

        private void Awake()
        {
            HidePrompt();
        }

        public void ShowPrompt(string message)
        {
            if (promptText != null)
            {
                promptText.text = message;
            }

            SetVisible(true);
        }

        public void HidePrompt()
        {
            SetVisible(false);
        }

        private void SetVisible(bool isVisible)
        {
            if (promptGroup == null)
            {
                return;
            }

            promptGroup.alpha = isVisible ? 1f : 0f;
            promptGroup.interactable = false;
            promptGroup.blocksRaycasts = false;
        }
    }
}
