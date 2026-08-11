using Growveld.Carrying;
using Growveld.Economy;
using Growveld.Interaction;
using UnityEngine;

namespace Growveld.Farming
{
    /// <summary>
    /// Converts carried physical Dried batches into quality-separated farm stock.
    /// </summary>
    public sealed class StorageContainer : MonoBehaviour, IHeldObjectReceiver, IContextualInfoProvider
    {
        [SerializeField] private FarmStockManager farmStock;
        [SerializeField, Min(1f)] private float capacityKilograms = 25f;
        [SerializeField, Min(0f)] private float storedKilograms;

        public float CapacityKilograms => capacityKilograms;
        public float StoredKilograms => storedKilograms;
        public string InteractionPrompt => "Store Dried harvest batch";
        public string ContextualInfo => $"Storage Bin\nStored: {storedKilograms:0.00} / {capacityKilograms:0.00} kg\nFarm stock total: {(farmStock != null ? farmStock.TotalKilograms : 0f):0.00} kg";

        private void Awake()
        {
            if (farmStock == null) farmStock = FindFirstObjectByType<FarmStockManager>();
        }

        public bool CanInteract(GameObject interactor)
        {
            if (farmStock == null
                || !interactor.TryGetComponent(out PlayerCarryController carryController)
                || carryController.HeldObject == null)
            {
                return false;
            }

            HarvestBatch batch = carryController.HeldObject.GetComponent<HarvestBatch>();
            return batch != null
                && batch.Status == HarvestStatus.Dried
                && storedKilograms + batch.WeightKilograms <= capacityKilograms;
        }

        public void Interact(GameObject interactor)
        {
            if (!CanInteract(interactor)
                || !interactor.TryGetComponent(out PlayerCarryController carryController))
            {
                return;
            }

            CarryableObject released = carryController.ReleaseHeldObjectForTransfer();
            HarvestBatch batch = released != null ? released.GetComponent<HarvestBatch>() : null;
            if (batch == null || batch.Status != HarvestStatus.Dried)
            {
                return;
            }

            storedKilograms += batch.WeightKilograms;
            farmStock.AddStock(batch.QualityGrade, batch.WeightKilograms);
            Destroy(batch.gameObject);
        }

        public void RestoreStoredKilograms(float kilograms)
        {
            storedKilograms = Mathf.Clamp(kilograms, 0f, capacityKilograms);
        }
    }
}
