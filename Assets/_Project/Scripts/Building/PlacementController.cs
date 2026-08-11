using System;
using System.Collections.Generic;
using Growveld.Economy;
using Growveld.Inventory;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Growveld.Building
{
    /// <summary>
    /// Centre-screen construction mode for placing, moving, and selling prefab equipment.
    /// </summary>
    public sealed class PlacementController : MonoBehaviour
    {
        [SerializeField] private Camera viewCamera;
        [SerializeField] private PlayerInventory inventory;
        [SerializeField] private LandManager landManager;
        [SerializeField] private EconomyManager economy;
        [SerializeField, Min(1f)] private float placementDistance = 18f;
        [SerializeField, Min(0.05f)] private float gridSize = 0.25f;
        [SerializeField] private Color validPreviewColor = new(0.18f, 0.9f, 0.35f, 0.62f);
        [SerializeField] private Color invalidPreviewColor = new(0.95f, 0.18f, 0.14f, 0.62f);

        private readonly List<Renderer> previewRenderers = new();
        private PlaceableDefinition activeDefinition;
        private GameObject previewObject;
        private PlacedObject movingObject;
        private float placementYaw;
        private bool placementValid;

        public event Action<bool> PlacementModeChanged;
        public event Action<PlaceableDefinition, bool, bool> PreviewChanged;

        public bool IsPlacing => activeDefinition != null;
        public bool IsMovingExisting => movingObject != null;
        public PlaceableDefinition ActiveDefinition => activeDefinition;
        public bool PlacementValid => placementValid;

        private void Awake()
        {
            if (viewCamera == null) viewCamera = GetComponentInChildren<Camera>(true);
            if (inventory == null) inventory = GetComponent<PlayerInventory>();
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            Mouse mouse = Mouse.current;

            if (!IsPlacing)
            {
                if (keyboard != null && keyboard.bKey.wasPressedThisFrame)
                {
                    BeginSelectedPlacement();
                }
                return;
            }

            if (keyboard != null && keyboard.rKey.wasPressedThisFrame)
            {
                placementYaw = Mathf.Repeat(placementYaw + activeDefinition.RotationStep, 360f);
            }

            UpdatePreview();

            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            {
                CancelPlacement();
                return;
            }

            if (keyboard != null && keyboard.deleteKey.wasPressedThisFrame && IsMovingExisting)
            {
                SellMovingObject();
                return;
            }

            if (mouse != null && mouse.leftButton.wasPressedThisFrame && placementValid)
            {
                ConfirmPlacement();
            }
        }

        public bool BeginSelectedPlacement()
        {
            if (economy != null && !economy.CanMakePurchases)
            {
                return false;
            }

            InventorySlot selectedSlot = inventory != null ? inventory.SelectedSlot : null;
            if (selectedSlot == null || selectedSlot.IsEmpty || selectedSlot.Item.PlaceableDefinition == null)
            {
                return false;
            }

            return BeginPlacement(selectedSlot.Item.PlaceableDefinition, null);
        }

        public bool BeginMove(PlacedObject placedObject)
        {
            if (placedObject == null || placedObject.Definition == null || IsPlacing)
            {
                return false;
            }

            movingObject = placedObject;
            placementYaw = placedObject.transform.eulerAngles.y;
            bool began = BeginPlacement(placedObject.Definition, placedObject);
            if (began)
            {
                placedObject.gameObject.SetActive(false);
            }
            return began;
        }

        public void CancelPlacement()
        {
            if (!IsPlacing) return;
            if (movingObject != null)
            {
                movingObject.gameObject.SetActive(true);
            }
            FinishPlacementMode();
        }

        private bool BeginPlacement(PlaceableDefinition definition, PlacedObject existingObject)
        {
            if (definition == null || definition.Prefab == null)
            {
                movingObject = null;
                return false;
            }

            activeDefinition = definition;
            movingObject = existingObject;
            if (existingObject == null)
            {
                placementYaw = transform.eulerAngles.y;
            }

            previewObject = Instantiate(definition.Prefab);
            previewObject.name = $"{definition.DisplayName} Placement Preview";
            PreparePreview(previewObject);
            landManager?.SetConstructionBoundariesVisible(true);
            PlacementModeChanged?.Invoke(true);
            UpdatePreview();
            return true;
        }

        private void PreparePreview(GameObject preview)
        {
            previewRenderers.Clear();
            previewRenderers.AddRange(preview.GetComponentsInChildren<Renderer>(true));

            foreach (Collider previewCollider in preview.GetComponentsInChildren<Collider>(true))
            {
                previewCollider.enabled = false;
            }

            foreach (Rigidbody previewBody in preview.GetComponentsInChildren<Rigidbody>(true))
            {
                previewBody.isKinematic = true;
            }

            foreach (MonoBehaviour behaviour in preview.GetComponentsInChildren<MonoBehaviour>(true))
            {
                behaviour.enabled = false;
            }

            Transform coverage = preview.transform.Find("Coverage Preview");
            if (coverage != null)
            {
                coverage.gameObject.SetActive(activeDefinition.LightCoverageRadius > 0f);
            }
        }

        private void UpdatePreview()
        {
            if (previewObject == null || viewCamera == null)
            {
                placementValid = false;
                PreviewChanged?.Invoke(activeDefinition, false, IsMovingExisting);
                return;
            }

            Ray ray = new(viewCamera.transform.position, viewCamera.transform.forward);
            bool hitSurface = Physics.Raycast(
                ray,
                out RaycastHit hit,
                placementDistance,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore);

            if (!hitSurface)
            {
                placementValid = false;
                SetPreviewAppearance(false);
                PreviewChanged?.Invoke(activeDefinition, false, IsMovingExisting);
                return;
            }

            Vector3 position = hit.point + activeDefinition.PlacementOffset;
            position.x = Mathf.Round(position.x / gridSize) * gridSize;
            position.z = Mathf.Round(position.z / gridSize) * gridSize;
            Quaternion rotation = Quaternion.Euler(0f, placementYaw, 0f);
            previewObject.transform.SetPositionAndRotation(position, rotation);

            placementValid = ValidatePlacement(position, rotation, hit.collider);
            SetPreviewAppearance(placementValid);
            PreviewChanged?.Invoke(activeDefinition, placementValid, IsMovingExisting);
        }

        private bool ValidatePlacement(Vector3 position, Quaternion rotation, Collider placementSurface)
        {
            Vector3 size = activeDefinition.FootprintSize;
            float radians = placementYaw * Mathf.Deg2Rad;
            Vector3 rotatedSize = new(
                Mathf.Abs(Mathf.Cos(radians)) * size.x + Mathf.Abs(Mathf.Sin(radians)) * size.z,
                size.y,
                Mathf.Abs(Mathf.Sin(radians)) * size.x + Mathf.Abs(Mathf.Cos(radians)) * size.z);
            Bounds footprint = new(position + Vector3.up * (size.y * 0.5f), rotatedSize);
            if (landManager == null || !landManager.IsFootprintOwned(footprint))
            {
                return false;
            }

            Vector3 halfExtents = size * 0.5f;
            halfExtents.x = Mathf.Max(0.05f, halfExtents.x - 0.04f);
            halfExtents.y = Mathf.Max(0.05f, halfExtents.y - 0.08f);
            halfExtents.z = Mathf.Max(0.05f, halfExtents.z - 0.04f);
            Vector3 centre = position + Vector3.up * (size.y * 0.5f + 0.08f);
            Collider[] overlaps = Physics.OverlapBox(
                centre,
                halfExtents,
                rotation,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore);

            foreach (Collider overlap in overlaps)
            {
                if (overlap == null || overlap == placementSurface) continue;
                if (overlap.transform.IsChildOf(transform)) continue;
                if (overlap.GetComponentInParent<LandPlot>() != null) continue;
                if (movingObject != null && overlap.transform.IsChildOf(movingObject.transform)) continue;
                return false;
            }

            return true;
        }

        private void SetPreviewAppearance(bool isValid)
        {
            Color color = isValid ? validPreviewColor : invalidPreviewColor;
            MaterialPropertyBlock properties = new();
            properties.SetColor("_BaseColor", color);
            properties.SetColor("_Color", color);
            foreach (Renderer previewRenderer in previewRenderers)
            {
                if (previewRenderer != null) previewRenderer.SetPropertyBlock(properties);
            }
        }

        private void ConfirmPlacement()
        {
            Vector3 position = previewObject.transform.position;
            Quaternion rotation = previewObject.transform.rotation;

            if (movingObject != null)
            {
                movingObject.transform.SetPositionAndRotation(position, rotation);
                movingObject.gameObject.SetActive(true);
            }
            else
            {
                ItemDefinition item = activeDefinition.ItemDefinition;
                if (inventory == null || item == null || !inventory.Remove(item, 1))
                {
                    return;
                }

                Instantiate(activeDefinition.Prefab, position, rotation);
            }

            Physics.SyncTransforms();
            FinishPlacementMode();
        }

        private void SellMovingObject()
        {
            float refund = activeDefinition.PurchasePrice * activeDefinition.SellRefundFraction;
            if (refund > 0f)
            {
                economy?.Credit(refund, $"Sold {activeDefinition.DisplayName}");
            }

            Destroy(movingObject.gameObject);
            movingObject = null;
            FinishPlacementMode();
        }

        private void FinishPlacementMode()
        {
            if (previewObject != null)
            {
                Destroy(previewObject);
            }
            previewObject = null;
            movingObject = null;
            activeDefinition = null;
            placementValid = false;
            previewRenderers.Clear();
            landManager?.SetConstructionBoundariesVisible(false);
            PlacementModeChanged?.Invoke(false);
        }
    }
}
