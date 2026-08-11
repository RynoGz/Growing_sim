using Growveld.Carrying;
using UnityEngine;
using UnityEngine.UI;

namespace Growveld.UI
{
    /// <summary>
    /// Keeps the physical carry state visible even when the player is not aiming at a receiver.
    /// </summary>
    public sealed class CarryStatusUI : MonoBehaviour
    {
        [SerializeField] private PlayerCarryController carryController;
        [SerializeField] private CanvasGroup panelGroup;
        [SerializeField] private Text statusText;

        private void OnEnable()
        {
            if (carryController != null) carryController.HeldObjectChanged += Refresh;
            Refresh(carryController != null ? carryController.HeldObject : null);
        }

        private void OnDisable()
        {
            if (carryController != null) carryController.HeldObjectChanged -= Refresh;
        }

        private void Refresh(CarryableObject heldObject)
        {
            bool visible = heldObject != null;
            if (panelGroup != null)
            {
                panelGroup.alpha = visible ? 1f : 0f;
                panelGroup.interactable = false;
                panelGroup.blocksRaycasts = false;
            }
            if (statusText != null && visible)
            {
                statusText.text = $"CARRYING: {heldObject.DisplayName.ToUpperInvariant()}   •   [E] DROP / TRANSFER";
            }
        }
    }
}
