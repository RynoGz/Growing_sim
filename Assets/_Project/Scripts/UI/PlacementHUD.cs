using Growveld.Building;
using UnityEngine;

namespace Growveld.UI
{
    public sealed class PlacementHUD : MonoBehaviour
    {
        [SerializeField] private PlacementController placementController;
        [SerializeField] private InteractionPromptUI promptUI;

        private void OnEnable()
        {
            if (placementController == null) placementController = FindFirstObjectByType<PlacementController>();
            if (promptUI == null) promptUI = GetComponent<InteractionPromptUI>();
            if (placementController != null)
            {
                placementController.PlacementModeChanged += HandlePlacementModeChanged;
                placementController.PreviewChanged += UpdateDetails;
            }
        }

        private void OnDisable()
        {
            if (placementController != null)
            {
                placementController.PlacementModeChanged -= HandlePlacementModeChanged;
                placementController.PreviewChanged -= UpdateDetails;
            }
            promptUI?.ClearPrompt(this);
        }

        private void HandlePlacementModeChanged(bool placing)
        {
            if (!placing) promptUI?.ClearPrompt(this);
        }

        private void UpdateDetails(PlaceableDefinition definition, bool valid, bool moving)
        {
            if (promptUI == null || definition == null) return;
            string status = valid ? "Valid position" : "Invalid position";
            string controls = moving
                ? "[Left Click] Place  [R] Rotate  [Delete] Sell  [Esc] Cancel"
                : "[Left Click] Place  [R] Rotate  [Esc] Cancel";
            promptUI.SetPrompt(this, $"{definition.DisplayName} - {status}\n{controls}", InteractionPromptUI.PlacementPriority);
        }
    }
}
