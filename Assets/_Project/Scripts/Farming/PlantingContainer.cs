using Growveld.Interaction;
using Growveld.Inventory;
using Growveld.Building;
using Growveld.UI;
using System.Collections.Generic;
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
        [SerializeField] private ItemDefinition[] seedCatalog;
        [SerializeField] private GameObject plantPrefab;
        [SerializeField] private Transform plantSocket;
        [SerializeField] private PlantInstance currentPlant;
        [SerializeField] private bool outdoor;
        [SerializeField] private bool requireOwnedLand;
        [SerializeField] private LandManager landManager;

        public PlantInstance CurrentPlant => currentPlant;
        public bool IsOutdoor => outdoor;
        public string InteractionPrompt => currentPlant == null
            ? "Choose strain seed"
            : "Inspect planted crop";
        public string ContextualInfo => currentPlant == null
            ? "Empty grow position\nInteract to choose an owned strain seed"
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
                && (!requireOwnedLand || (landManager != null && landManager.IsPositionOwned(transform.position)))
                && interactor.TryGetComponent(out PlayerInventory inventory)
                && GetOwnedSeeds(inventory).Count > 0;
        }

        public void Interact(GameObject interactor)
        {
            if (!CanInteract(interactor) || !interactor.TryGetComponent(out PlayerInventory inventory)) return;
            SeedSelectionUI.Show(this, interactor, GetOwnedSeeds(inventory));
        }

        public List<ItemDefinition> GetOwnedSeeds(PlayerInventory inventory)
        {
            List<ItemDefinition> owned = new();
            if (inventory == null) return owned;
            if (seedCatalog != null)
            {
                foreach (ItemDefinition seed in seedCatalog)
                {
                    if (seed != null && seed.StrainDefinition != null && inventory.Count(seed) > 0) owned.Add(seed);
                }
            }
            if (owned.Count == 0 && seedItem != null && inventory.Count(seedItem) > 0) owned.Add(seedItem);
            return owned;
        }

        public bool TryPlant(GameObject interactor, ItemDefinition selectedSeed)
        {
            if (currentPlant != null || selectedSeed == null || selectedSeed.StrainDefinition == null
                || interactor == null || !interactor.TryGetComponent(out PlayerInventory inventory)
                || !inventory.Remove(selectedSeed, 1)) return false;

            PlantDefinition strain = selectedSeed.StrainDefinition;
            GameObject sourcePrefab = strain.PlantPrefab != null ? strain.PlantPrefab : plantPrefab;
            if (sourcePrefab == null)
            {
                inventory.Add(selectedSeed, 1);
                return false;
            }
            GameObject plantObject = Instantiate(sourcePrefab, plantSocket.position, plantSocket.rotation, plantSocket);
            plantObject.name = strain.DisplayName;
            currentPlant = plantObject.GetComponent<PlantInstance>();
            currentPlant?.InitialiseDefinition(strain);
            return currentPlant != null;
        }

        public void ClearPlant(PlantInstance expectedPlant)
        {
            if (currentPlant == expectedPlant) currentPlant = null;
        }

        public void RestorePlant(PlantInstance plant)
        {
            currentPlant = plant;
        }

        public PlantInstance SpawnRestoredPlant(PlantDefinition strain = null)
        {
            if (plantPrefab == null || currentPlant != null)
            {
                return currentPlant;
            }

            if (plantSocket == null) plantSocket = transform;
            PlantDefinition resolved = strain != null ? strain : plantDefinition;
            GameObject sourcePrefab = resolved != null && resolved.PlantPrefab != null ? resolved.PlantPrefab : plantPrefab;
            if (sourcePrefab == null) return null;
            GameObject plantObject = Instantiate(sourcePrefab, plantSocket.position, plantSocket.rotation, plantSocket);
            plantObject.name = resolved != null ? resolved.DisplayName : "Restored Plant";
            currentPlant = plantObject.GetComponent<PlantInstance>();
            currentPlant?.InitialiseDefinition(resolved);
            return currentPlant;
        }
    }
}
