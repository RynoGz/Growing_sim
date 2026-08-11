using System;
using Growveld.Interaction;
using Growveld.Inventory;
using Growveld.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Growveld.Building
{
    /// <summary>
    /// Authoritative state for construction-only selection, movement, selling, and placement.
    /// </summary>
    public sealed class ConstructionModeController : MonoBehaviour
    {
        [SerializeField] private PlacementController placementController;
        [SerializeField] private PlayerInteractor playerInteractor;

        public event Action<bool> ModeChanged;

        public bool IsActive { get; private set; }
        public int LastExitFrame { get; private set; } = -1;

        private void Awake()
        {
            if (placementController == null) placementController = GetComponent<PlacementController>();
            if (playerInteractor == null) playerInteractor = GetComponent<PlayerInteractor>();
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return;

            if (keyboard.bKey.wasPressedThisFrame)
            {
                if (IsActive) ExitMode();
                else EnterMode();
                return;
            }

            if (!IsActive) return;

            if (keyboard.escapeKey.wasPressedThisFrame)
            {
                ExitMode();
                return;
            }

            if (keyboard.deleteKey.wasPressedThisFrame
                && placementController != null
                && !placementController.IsPlacing
                && playerInteractor?.CurrentInteractable is PlacedObject placed)
            {
                placementController.SellPlacedObject(placed);
            }
        }

        public void EnterMode()
        {
            if (IsActive) return;
            IsActive = true;
            ModeChanged?.Invoke(true);
            GameplayMessageUI.Show("Construction Mode enabled");
        }

        public void ExitMode()
        {
            if (!IsActive && (placementController == null || !placementController.IsPlacing)) return;
            LastExitFrame = Time.frameCount;
            placementController?.CancelPlacement();
            IsActive = false;
            ModeChanged?.Invoke(false);
            GameplayMessageUI.Show("Construction Mode disabled");
        }

        public bool BeginInventoryPlacement(ItemDefinition item)
        {
            if (item == null || item.PlaceableDefinition == null) return false;

            bool wasActive = IsActive;
            EnterMode();
            if (placementController != null && placementController.BeginInventoryPlacement(item)) return true;

            if (!wasActive) ExitMode();
            return false;
        }
    }
}
