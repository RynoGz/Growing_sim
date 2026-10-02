using System;
using UnityEngine;

namespace Growveld.Player
{
    public enum PlayerInputMode
    {
        Gameplay,
        Construction,
        Placement,
        Tablet,
        SeedSelection,
        Paused,
        AdminConsole
    }

    /// <summary>
    /// Single source of truth for player input mode and cursor ownership.
    /// Construction and placement are gameplay modes; modal UI modes suspend movement and look.
    /// </summary>
    public sealed class PlayerInputStateController : MonoBehaviour
    {
        private bool tabletOpen;
        private bool seedSelectionOpen;
        private bool paused;
        private bool adminConsoleOpen;
        private bool constructionActive;
        private bool placementActive;
        private bool applicationFocused = true;

        public event Action<PlayerInputMode> ModeChanged;

        public PlayerInputMode CurrentMode { get; private set; } = PlayerInputMode.Gameplay;
        public bool AllowsMovementAndLook => CurrentMode is PlayerInputMode.Gameplay
            or PlayerInputMode.Construction
            or PlayerInputMode.Placement;
        public bool WantsLockedCursor => AllowsMovementAndLook;

        private void Awake()
        {
            applicationFocused = Application.isFocused;
            RefreshMode(true);
        }

        private void OnEnable()
        {
            RefreshMode(true);
        }

        private void LateUpdate()
        {
            ApplyCursorState();
        }

        private void OnDisable()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            applicationFocused = hasFocus;
            if (hasFocus) ApplyCursorState(true);
        }

        public void SetTabletOpen(bool open)
        {
            tabletOpen = open;
            RefreshMode();
        }

        public void SetSeedSelectionOpen(bool open)
        {
            seedSelectionOpen = open;
            RefreshMode();
        }

        public void SetPaused(bool isPaused)
        {
            paused = isPaused;
            RefreshMode();
        }

        public void SetAdminConsoleOpen(bool open)
        {
            adminConsoleOpen = open;
            RefreshMode();
        }

        public void SetConstructionActive(bool active)
        {
            constructionActive = active;
            RefreshMode();
        }

        public void SetPlacementActive(bool active)
        {
            placementActive = active;
            RefreshMode();
        }

        public void RefreshCursor()
        {
            RefreshMode(true);
        }

        private void RefreshMode(bool forceCursorRefresh = false)
        {
            PlayerInputMode nextMode = ResolveMode();
            bool changed = nextMode != CurrentMode;
            CurrentMode = nextMode;
            ApplyCursorState(forceCursorRefresh || changed);
            if (changed) ModeChanged?.Invoke(CurrentMode);
        }

        private PlayerInputMode ResolveMode()
        {
            if (paused) return PlayerInputMode.Paused;
            if (adminConsoleOpen) return PlayerInputMode.AdminConsole;
            if (tabletOpen) return PlayerInputMode.Tablet;
            if (seedSelectionOpen) return PlayerInputMode.SeedSelection;
            if (placementActive) return PlayerInputMode.Placement;
            if (constructionActive) return PlayerInputMode.Construction;
            return PlayerInputMode.Gameplay;
        }

        private void ApplyCursorState(bool force = false)
        {
            if (!applicationFocused) return;
            CursorLockMode desiredLock = WantsLockedCursor ? CursorLockMode.Locked : CursorLockMode.None;
            bool desiredVisibility = !WantsLockedCursor;
            if (force || Cursor.lockState != desiredLock) Cursor.lockState = desiredLock;
            if (force || Cursor.visible != desiredVisibility) Cursor.visible = desiredVisibility;
        }
    }
}
