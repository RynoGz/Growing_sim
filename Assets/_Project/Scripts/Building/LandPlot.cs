using System;
using UnityEngine;

namespace Growveld.Building
{
    /// <summary>
    /// A rectangular parcel that can answer whether world-space points are buildable.
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public sealed class LandPlot : MonoBehaviour
    {
        [SerializeField] private string plotId;
        [SerializeField] private string displayName;
        [SerializeField, Min(0f)] private float purchasePrice;
        [SerializeField] private bool startingOwned;
        [SerializeField] private bool isOwned;
        [SerializeField] private Renderer[] boundaryRenderers;

        private BoxCollider areaCollider;
        private bool constructionHighlight;

        public event Action<LandPlot> OwnershipChanged;

        public string PlotId => plotId;
        public string DisplayName => displayName;
        public float PurchasePrice => purchasePrice;
        public bool IsOwned => isOwned;
        public Bounds WorldBounds
        {
            get
            {
                if (areaCollider == null)
                {
                    areaCollider = GetComponent<BoxCollider>();
                }

                return areaCollider != null
                    ? areaCollider.bounds
                    : new Bounds(transform.position, Vector3.zero);
            }
        }

        private void Awake()
        {
            areaCollider = GetComponent<BoxCollider>();
            areaCollider.isTrigger = true;
            if (startingOwned)
            {
                isOwned = true;
            }
            RefreshBoundaryAppearance();
        }

        public bool ContainsPoint(Vector3 worldPoint)
        {
            Bounds bounds = WorldBounds;
            return worldPoint.x >= bounds.min.x
                && worldPoint.x <= bounds.max.x
                && worldPoint.z >= bounds.min.z
                && worldPoint.z <= bounds.max.z;
        }

        public bool ContainsFootprint(Bounds footprint)
        {
            Bounds bounds = WorldBounds;
            return footprint.min.x >= bounds.min.x
                && footprint.max.x <= bounds.max.x
                && footprint.min.z >= bounds.min.z
                && footprint.max.z <= bounds.max.z;
        }

        public void SetOwned(bool owned)
        {
            if (isOwned == owned)
            {
                return;
            }

            isOwned = owned;
            RefreshBoundaryAppearance();
            OwnershipChanged?.Invoke(this);
        }

        public void SetConstructionHighlight(bool highlighted)
        {
            constructionHighlight = highlighted;
            RefreshBoundaryAppearance();
        }

        private void RefreshBoundaryAppearance()
        {
            if (boundaryRenderers == null)
            {
                return;
            }

            Color baseColor = isOwned
                ? new Color(0.18f, 0.82f, 0.32f, 1f)
                : new Color(0.95f, 0.48f, 0.12f, 1f);
            Color visibleColor = constructionHighlight
                ? Color.Lerp(baseColor, Color.white, 0.25f)
                : baseColor * 0.72f;
            MaterialPropertyBlock properties = new();
            properties.SetColor("_BaseColor", visibleColor);
            properties.SetColor("_Color", visibleColor);

            foreach (Renderer boundaryRenderer in boundaryRenderers)
            {
                if (boundaryRenderer == null) continue;
                boundaryRenderer.SetPropertyBlock(properties);
            }
        }
    }
}
