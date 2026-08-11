using Growveld.Building;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Growveld.UI
{
    /// <summary>
    /// Standard Escape pause menu plus an H-key controls reference.
    /// </summary>
    public sealed class PauseAndHelpController : MonoBehaviour
    {
        [SerializeField] private GameObject pauseRoot;
        [SerializeField] private GameObject controlsRoot;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button controlsButton;
        [SerializeField] private Button closeControlsButton;
        [SerializeField] private Behaviour[] gameplayBehaviours;
        [SerializeField] private BusinessTabletController tabletController;
        [SerializeField] private PlacementController placementController;

        private float previousTimeScale = 1f;

        public bool IsPaused { get; private set; }

        private void Awake()
        {
            resumeButton?.onClick.AddListener(Resume);
            controlsButton?.onClick.AddListener(() => SetControlsVisible(true));
            closeControlsButton?.onClick.AddListener(() => SetControlsVisible(false));
            SetPaused(false, false);
        }

        private void Update()
        {
            if (Keyboard.current == null) return;

            if (Keyboard.current.hKey.wasPressedThisFrame)
            {
                if (!IsPaused) SetPaused(true, true);
                else SetControlsVisible(!controlsRoot.activeSelf);
                return;
            }

            if (!Keyboard.current.escapeKey.wasPressedThisFrame) return;
            if (!IsPaused && (tabletController != null && tabletController.IsOpen)) return;
            if (!IsPaused && (placementController != null && placementController.IsPlacing)) return;

            if (IsPaused && controlsRoot != null && controlsRoot.activeSelf)
            {
                SetControlsVisible(false);
            }
            else
            {
                SetPaused(!IsPaused, false);
            }
        }

        private void OnDisable()
        {
            if (IsPaused) SetPaused(false, false);
        }

        public void Resume() => SetPaused(false, false);

        public void ShowControls() => SetPaused(true, true);

        private void SetPaused(bool paused, bool showControls)
        {
            if (paused == IsPaused && pauseRoot != null && pauseRoot.activeSelf == paused)
            {
                SetControlsVisible(showControls);
                return;
            }

            IsPaused = paused;
            if (paused)
            {
                tabletController?.SetOpen(false);
                placementController?.CancelPlacement();
                previousTimeScale = Time.timeScale > 0f ? Time.timeScale : 1f;
                Time.timeScale = 0f;
            }
            else
            {
                Time.timeScale = previousTimeScale;
            }

            if (pauseRoot != null) pauseRoot.SetActive(paused);
            if (controlsRoot != null) controlsRoot.SetActive(paused && showControls);
            if (gameplayBehaviours != null)
            {
                foreach (Behaviour behaviour in gameplayBehaviours) if (behaviour != null) behaviour.enabled = !paused;
            }

            Cursor.lockState = paused ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = paused;
        }

        private void SetControlsVisible(bool visible)
        {
            if (controlsRoot != null) controlsRoot.SetActive(IsPaused && visible);
        }
    }
}
