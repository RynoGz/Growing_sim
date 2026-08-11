using UnityEngine;

namespace Growveld.Interaction
{
    /// <summary>
    /// Temporary Phase 3 test object. Interacting toggles its colour so the
    /// complete raycast and input flow can be tested without later systems.
    /// </summary>
    public sealed class PrototypeInteractable : MonoBehaviour, IInteractable
    {
        [SerializeField] private string interactionPrompt = "Inspect test crate";
        [SerializeField] private Renderer targetRenderer;
        [SerializeField] private Color activatedColor = new Color(0.3f, 0.65f, 0.42f, 1f);

        private Material runtimeMaterial;
        private Color originalColor;
        private bool isActivated;

        public string InteractionPrompt => interactionPrompt;

        private void Awake()
        {
            if (targetRenderer == null)
            {
                targetRenderer = GetComponentInChildren<Renderer>();
            }

            if (targetRenderer != null)
            {
                runtimeMaterial = targetRenderer.material;
                originalColor = GetMaterialColor(runtimeMaterial);
            }
        }

        public bool CanInteract(GameObject interactor)
        {
            return isActiveAndEnabled;
        }

        public void Interact(GameObject interactor)
        {
            isActivated = !isActivated;

            if (runtimeMaterial != null)
            {
                SetMaterialColor(runtimeMaterial, isActivated ? activatedColor : originalColor);
            }

            Debug.Log($"{interactor.name} interacted with {name}.", this);
        }

        private static Color GetMaterialColor(Material material)
        {
            return material.HasProperty("_BaseColor")
                ? material.GetColor("_BaseColor")
                : material.color;
        }

        private static void SetMaterialColor(Material material, Color color)
        {
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }
            else
            {
                material.color = color;
            }
        }

        private void OnDestroy()
        {
            if (runtimeMaterial != null)
            {
                Destroy(runtimeMaterial);
            }
        }
    }
}
