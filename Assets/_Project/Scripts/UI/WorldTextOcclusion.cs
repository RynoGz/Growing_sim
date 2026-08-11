using UnityEngine;

namespace Growveld.UI
{
    /// <summary>
    /// Gives legacy world TextMesh labels depth-tested rendering and line-of-sight visibility.
    /// </summary>
    [RequireComponent(typeof(TextMesh), typeof(MeshRenderer))]
    public sealed class WorldTextOcclusion : MonoBehaviour
    {
        [SerializeField, Min(1f)] private float maximumVisibleDistance = 45f;
        [SerializeField] private LayerMask occlusionLayers = ~0;
        [SerializeField, Min(0f)] private float targetTolerance = 0.12f;
        [SerializeField] private Shader depthTestedShader;

        private Camera viewCamera;
        private MeshRenderer meshRenderer;
        private Material depthTestedMaterial;

        private void Awake()
        {
            meshRenderer = GetComponent<MeshRenderer>();
            viewCamera = Camera.main;
            ApplyDepthTestedMaterial();
        }

        private void LateUpdate()
        {
            if (meshRenderer == null) return;
            if (viewCamera == null) viewCamera = Camera.main;
            if (viewCamera == null)
            {
                meshRenderer.enabled = false;
                return;
            }

            meshRenderer.enabled = IsVisibleFrom(viewCamera.transform.position);
        }

        public bool IsVisibleFrom(Vector3 origin)
        {
            if (meshRenderer == null) meshRenderer = GetComponent<MeshRenderer>();
            Vector3 target = meshRenderer.bounds.center;
            Vector3 offset = target - origin;
            float distance = offset.magnitude;
            bool visible = distance <= maximumVisibleDistance;

            if (visible && distance > 0.01f)
            {
                RaycastHit[] hits = Physics.RaycastAll(
                    origin,
                    offset / distance,
                    distance,
                    occlusionLayers,
                    QueryTriggerInteraction.Ignore);

                foreach (RaycastHit hit in hits)
                {
                    if (hit.collider == null || hit.collider.transform.IsChildOf(transform)) continue;
                    if (hit.distance < distance - targetTolerance)
                    {
                        visible = false;
                        break;
                    }
                }
            }

            return visible;
        }

        private void OnDestroy()
        {
            if (depthTestedMaterial != null) Destroy(depthTestedMaterial);
        }

        private void ApplyDepthTestedMaterial()
        {
            TextMesh textMesh = GetComponent<TextMesh>();
            Shader shader = depthTestedShader != null
                ? depthTestedShader
                : Shader.Find("Growveld/World Text Occluded");
            Material sourceMaterial = textMesh != null && textMesh.font != null
                ? textMesh.font.material
                : meshRenderer.sharedMaterial;
            if (shader == null || sourceMaterial == null) return;

            depthTestedMaterial = new Material(sourceMaterial)
            {
                name = $"{name} Occluded Text Material",
                shader = shader
            };
            depthTestedMaterial.SetColor("_Color", Color.white);
            meshRenderer.material = depthTestedMaterial;
        }
    }
}
