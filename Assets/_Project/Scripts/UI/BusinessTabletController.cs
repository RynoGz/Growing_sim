using Growveld.Building;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Growveld.UI
{
    /// <summary>
    /// Opens the business tablet and temporarily suspends first-person gameplay input.
    /// </summary>
    public sealed class BusinessTabletController : MonoBehaviour
    {
        [SerializeField] private GameObject tabletRoot;
        [SerializeField] private Behaviour[] gameplayBehaviours;
        [SerializeField] private PlacementController placementController;
        [SerializeField] private ConstructionModeController constructionMode;
        [SerializeField] private TabletInventoryUI inventoryUI;

        public bool IsOpen { get; private set; }

        private void Start()
        {
            SetOpen(false);
        }

        private void Update()
        {
            if (Keyboard.current == null)
            {
                return;
            }

            if (Keyboard.current.tKey.wasPressedThisFrame)
            {
                SetOpen(!IsOpen);
            }
            else if (IsOpen && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                if (inventoryUI != null && inventoryUI.TryCloseContextMenu()) return;
                SetOpen(false);
            }
        }

        public void SetOpen(bool open)
        {
            if (open)
            {
                constructionMode?.ExitMode();
                placementController?.CancelPlacement();
            }

            IsOpen = open;
            if (tabletRoot != null)
            {
                tabletRoot.SetActive(open);
            }

            if (gameplayBehaviours != null)
            {
                foreach (Behaviour behaviour in gameplayBehaviours)
                {
                    if (behaviour != null) behaviour.enabled = !open;
                }
            }

            Cursor.lockState = open ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = open;
        }
    }
}
