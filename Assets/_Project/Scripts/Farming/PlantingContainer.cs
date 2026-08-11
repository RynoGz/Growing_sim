using Growveld.Interaction;
using Growveld.Inventory;
using Growveld.Building;
using UnityEngine;

namespace Growveld.Farming
{
    /// <summary>
    /// A pot or soil point that consumes one seed and hosts one physical plant instance.
    /// </summary>
    public sealed class PlantingContainer : MonoBehaviour, IInteractable, IContextualInfoProvider
    {
        [SerializeField] private PlantDefinition plantDefinition;
        [SerializeField] private ItemDefinition seedItem;
        [SerializeField] private GameObject plantPrefab;
        [SerializeField] private Transform plantSocket;
        [SerializeField] private PlantInstance currentPlant;
        [SerializeField] private bool outdoor;
        [SerializeField] private bool requireOwnedLand;
        [SerializeField] private LandManager landManager;

        public PlantInstance CurrentPlant => currentPlant;
        public bool IsOutdoor => outdoor;
        public string InteractionPrompt => currentPlant == null
            ? $"Plant {seedItem?.DisplayName ?? "seed"}"
            : "Inspect planted crop";
        public string ContextualInfo => currentPlant == null
            ? $"Empty grow position\nRequires: {seedItem?.DisplayName ?? "seed"}"
            : currentPlant.ContextualInfo;

        private void Awake()
        {
            if (plantSocket == null) plantSocket = transform;
            if (currentPlant == null) currentPlant = GetComponentInChildren<PlantInstance>(true);
        }

        public bool CanInteract(GameObject interactor)
        {
            return currentPlant == null
                && plantPrefab != null
                && seedItem != null
                && (!requireOwnedLand || (landManager != null && landManager.IsPositionOwned(transform.position)))
                && interactor.TryGetComponent(out PlayerInventory inventory)
                && inventory.Count(seedItem) > 0;
        }

        public void Interact(GameObject interactor)
        {
            if (!CanInteract(interactor)
                || !interactor.TryGetComponent(out PlayerInventory inventory)
                || !inventory.Remove(seedItem, 1))
            {
                return;
            }

            GameObject plantObject = Instantiate(plantPrefab, plantSocket.position, plantSocket.rotation, plantSocket);
            plantObject.name = plantDefinition != null ? plantDefinition.DisplayName : "Plant";
            currentPlant = plantObject.GetComponent<PlantInstance>();
        }

        public void ClearPlant(PlantInstance expectedPlant)
        {
            if (currentPlant == expectedPlant) currentPlant = null;
        }

        public void RestorePlant(PlantInstance plant)
        {
            currentPlant = plant;
        }

        public PlantInstance SpawnRestoredPlant()
        {
            if (plantPrefab == null || currentPlant != null)
            {
                return currentPlant;
            }

            if (plantSocket == null) plantSocket = transform;
            GameObject plantObject = Instantiate(plantPrefab, plantSocket.position, plantSocket.rotation, plantSocket);
            plantObject.name = plantDefinition != null ? plantDefinition.DisplayName : "Restored Plant";
            currentPlant = plantObject.GetComponent<PlantInstance>();
            return currentPlant;
        }
    }
}
