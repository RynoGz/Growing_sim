using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Growveld.Building;
using Growveld.Core;
using Growveld.Economy;
using Growveld.Environment;
using Growveld.Farming;
using Growveld.Inventory;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Growveld.Saving
{
    /// <summary>
    /// Captures and restores explicit serializable prototype state as JSON.
    /// </summary>
    public sealed class SaveSystem : MonoBehaviour
    {
        [Header("Scene services")]
        [SerializeField] private Transform player;
        [SerializeField] private EconomyManager economy;
        [SerializeField] private GameTimeManager gameTime;
        [SerializeField] private LandManager landManager;
        [SerializeField] private PlayerInventory inventory;
        [SerializeField] private DeliveryManager deliveries;
        [SerializeField] private FarmStockManager farmStock;
        [SerializeField] private UtilityManager utilities;

        [Header("Asset catalogues")]
        [SerializeField] private ItemDefinition[] itemCatalog;
        [SerializeField] private PlaceableDefinition[] placeableCatalog;
        [SerializeField] private GameObject harvestBatchPrefab;

        [Header("Behaviour")]
        [SerializeField, Min(10f)] private float autosaveIntervalSeconds = 120f;
        [SerializeField] private bool loadExistingSaveOnStart = true;

        private float autosaveTimer;
        private bool isLoading;

        public event Action<string, bool> SaveStatusChanged;

        public string SaveDirectory => Path.Combine(Application.persistentDataPath, "Growveld");
        public string SavePath => Path.Combine(SaveDirectory, "save.json");
        public bool HasSave => File.Exists(SavePath);

        private void Start()
        {
            autosaveTimer = autosaveIntervalSeconds;
            if (loadExistingSaveOnStart && HasSave) LoadGame();
        }

        private void Update()
        {
            if (Keyboard.current != null)
            {
                if (Keyboard.current.f5Key.wasPressedThisFrame) SaveGame(false);
                if (Keyboard.current.f9Key.wasPressedThisFrame) LoadGame();
            }

            if (isLoading) return;
            autosaveTimer -= Time.unscaledDeltaTime;
            if (autosaveTimer <= 0f)
            {
                autosaveTimer = autosaveIntervalSeconds;
                SaveGame(true);
            }
        }

        private void OnApplicationQuit()
        {
            if (!isLoading) SaveGame(true);
        }

        public bool SaveGame(bool autosave = false)
        {
            try
            {
                GameSaveData data = CaptureData();
                string json = JsonUtility.ToJson(data, true);
                Directory.CreateDirectory(SaveDirectory);
                string temporaryPath = SavePath + ".tmp";
                File.WriteAllText(temporaryPath, json);
                File.Copy(temporaryPath, SavePath, true);
                File.Delete(temporaryPath);
                SaveStatusChanged?.Invoke(autosave ? "Autosaved" : "Game saved", true);
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                SaveStatusChanged?.Invoke("Save failed - see Console", false);
                return false;
            }
        }

        public void LoadGame()
        {
            if (isLoading || !HasSave)
            {
                SaveStatusChanged?.Invoke("No save file found", false);
                return;
            }

            try
            {
                string json = File.ReadAllText(SavePath);
                GameSaveData data = JsonUtility.FromJson<GameSaveData>(json);
                if (data == null) throw new InvalidDataException("Save file contains no data.");
                StartCoroutine(LoadRoutine(data));
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                SaveStatusChanged?.Invoke("Load failed - see Console", false);
            }
        }

        private GameSaveData CaptureData()
        {
            GameSaveData data = new()
            {
                savedAtUtc = DateTime.UtcNow.ToString("O"),
                balance = economy != null ? economy.Balance : 0f,
                day = gameTime != null ? gameTime.Day : 1,
                timeOfDayHours = gameTime != null ? gameTime.TimeOfDayHours : 7f,
                playerPosition = new Vector3Data(player != null ? player.position : Vector3.zero),
                playerRotation = new QuaternionData(player != null ? player.rotation : Quaternion.identity),
                selectedInventorySlot = inventory != null ? inventory.SelectedSlotIndex : 0
            };

            CaptureLand(data);
            CaptureInventory(data);
            CapturePlacedObjects(data);
            CapturePlants(data);
            CaptureHarvestBatches(data);
            CaptureDryingRacks(data);
            CaptureStorageAndRooms(data);
            CaptureDeliveries(data);
            CaptureStockAndUtilities(data);
            return data;
        }

        private IEnumerator LoadRoutine(GameSaveData data)
        {
            isLoading = true;
            ClearDynamicWorld();
            yield return null;

            Dictionary<string, PlacedObject> placedObjects = RestorePlacedObjects(data);
            RestoreCoreState(data);
            RestorePlants(data, placedObjects);
            RestoreLooseBatches(data);
            RestoreDryingRacks(data, placedObjects);
            RestoreStorageAndRooms(data, placedObjects);
            RestoreDeliveries(data);
            autosaveTimer = autosaveIntervalSeconds;
            isLoading = false;
            SaveStatusChanged?.Invoke("Game loaded", true);
        }

        private void CaptureLand(GameSaveData data)
        {
            if (landManager?.Plots == null) return;
            foreach (LandPlot plot in landManager.Plots)
            {
                if (plot != null && plot.IsOwned) data.ownedPlotIds.Add(plot.PlotId);
            }
        }

        private void CaptureInventory(GameSaveData data)
        {
            if (inventory == null) return;
            for (int index = 0; index < inventory.Slots.Count; index++)
            {
                InventorySlot slot = inventory.Slots[index];
                if (slot == null || slot.IsEmpty) continue;
                data.inventory.Add(new InventorySlotSaveData
                {
                    slotIndex = index,
                    itemId = slot.Item.ItemId,
                    quantity = slot.Quantity
                });
            }
        }

        private static void CapturePlacedObjects(GameSaveData data)
        {
            foreach (PlacedObject placed in FindObjectsByType<PlacedObject>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (!placed.enabled || placed.Definition == null) continue;
                data.placedObjects.Add(new PlacedObjectSaveData
                {
                    persistentId = placed.PersistentId,
                    placeableId = placed.Definition.PlaceableId,
                    position = new Vector3Data(placed.transform.position),
                    rotation = new QuaternionData(placed.transform.rotation)
                });
            }
        }

        private static void CapturePlants(GameSaveData data)
        {
            foreach (PlantingContainer container in FindObjectsByType<PlantingContainer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                PlantInstance plant = container.CurrentPlant;
                if (plant == null) continue;
                data.plants.Add(new PlantSaveData
                {
                    containerKey = GetContainerKey(container),
                    elapsedGrowthSeconds = plant.ElapsedGrowthSeconds,
                    water = plant.WaterLevel,
                    nutrients = plant.NutrientLevel,
                    health = plant.Health,
                    qualityScore = plant.QualityScore,
                    yieldPotential = plant.YieldPotential,
                    accumulatedCareScore = plant.AccumulatedCareScore,
                    careSampleSeconds = plant.CareSampleSeconds
                });
            }
        }

        private static void CaptureHarvestBatches(GameSaveData data)
        {
            foreach (HarvestBatch batch in FindObjectsByType<HarvestBatch>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (batch.GetComponentInParent<DryingRack>() != null) continue;
                data.looseHarvestBatches.Add(CreateBatchSaveData(batch));
            }
        }

        private static void CaptureDryingRacks(GameSaveData data)
        {
            foreach (DryingRack rack in FindObjectsByType<DryingRack>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                PlacedObject placed = rack.GetComponent<PlacedObject>();
                if (placed == null) continue;
                DryingRackSaveData rackData = new() { placedObjectId = placed.PersistentId };
                for (int index = 0; index < rack.Slots.Count; index++)
                {
                    DryingSlotState slot = rack.Slots[index];
                    if (slot.IsEmpty) continue;
                    rackData.batches.Add(new DryingBatchSaveData
                    {
                        slotIndex = index,
                        remainingSeconds = slot.RemainingSeconds,
                        batch = CreateBatchSaveData(slot.Batch)
                    });
                }
                data.dryingRacks.Add(rackData);
            }
        }

        private static void CaptureStorageAndRooms(GameSaveData data)
        {
            foreach (StorageContainer storage in FindObjectsByType<StorageContainer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                PlacedObject placed = storage.GetComponent<PlacedObject>();
                if (placed != null) data.storageContainers.Add(new StorageSaveData { placedObjectId = placed.PersistentId, storedKilograms = storage.StoredKilograms });
            }
            foreach (GrowRoomEnvironment room in FindObjectsByType<GrowRoomEnvironment>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                PlacedObject placed = room.GetComponent<PlacedObject>();
                if (placed != null) data.rooms.Add(new RoomSaveData { placedObjectId = placed.PersistentId, humidity = room.Humidity });
            }
        }

        private void CaptureDeliveries(GameSaveData data)
        {
            if (deliveries == null) return;
            foreach (PendingDelivery delivery in deliveries.PendingDeliveries)
            {
                data.pendingDeliveries.Add(new DeliverySaveData
                {
                    deliveryId = delivery.DeliveryId,
                    itemId = delivery.Item.ItemId,
                    quantity = delivery.Quantity,
                    remainingGameMinutes = delivery.RemainingGameMinutes
                });
            }
        }

        private void CaptureStockAndUtilities(GameSaveData data)
        {
            if (farmStock != null)
            {
                data.farmStock.low = farmStock.GetWeight(QualityGrade.Low);
                data.farmStock.standard = farmStock.GetWeight(QualityGrade.Standard);
                data.farmStock.premium = farmStock.GetWeight(QualityGrade.Premium);
                data.farmStock.topGrade = farmStock.GetWeight(QualityGrade.TopGrade);
            }
            if (utilities != null)
            {
                data.utilities.electricityKilowattHours = utilities.CurrentElectricityKilowattHours;
                data.utilities.waterLitres = utilities.CurrentWaterLitres;
                data.utilities.currentDay = utilities.CurrentDay;
            }
        }

        private void ClearDynamicWorld()
        {
            foreach (PlantInstance plantInstance in FindObjectsByType<PlantInstance>(FindObjectsInactive.Include, FindObjectsSortMode.None)) Destroy(plantInstance.gameObject);
            foreach (HarvestBatch batch in FindObjectsByType<HarvestBatch>(FindObjectsInactive.Include, FindObjectsSortMode.None)) Destroy(batch.gameObject);
            foreach (PlacedObject placed in FindObjectsByType<PlacedObject>(FindObjectsInactive.Include, FindObjectsSortMode.None)) Destroy(placed.gameObject);
            foreach (PlantingContainer container in FindObjectsByType<PlantingContainer>(FindObjectsInactive.Include, FindObjectsSortMode.None)) container.ClearPlant(container.CurrentPlant);
        }

        private Dictionary<string, PlacedObject> RestorePlacedObjects(GameSaveData data)
        {
            Dictionary<string, PlacedObject> restored = new();
            foreach (PlacedObjectSaveData saved in data.placedObjects)
            {
                PlaceableDefinition definition = FindPlaceable(saved.placeableId);
                if (definition == null || definition.Prefab == null) continue;
                GameObject instance = Instantiate(definition.Prefab, saved.position.ToVector3(), saved.rotation.ToQuaternion());
                PlacedObject placed = instance.GetComponent<PlacedObject>();
                if (placed == null) continue;
                placed.RestorePersistentId(saved.persistentId);
                restored[saved.persistentId] = placed;
            }
            return restored;
        }

        private void RestoreCoreState(GameSaveData data)
        {
            economy?.RestoreBalance(data.balance);
            gameTime?.RestoreTime(data.day, data.timeOfDayHours);
            if (player != null)
            {
                CharacterController controller = player.GetComponent<CharacterController>();
                if (controller != null) controller.enabled = false;
                player.SetPositionAndRotation(data.playerPosition.ToVector3(), data.playerRotation.ToQuaternion());
                if (controller != null) controller.enabled = true;
            }
            if (landManager?.Plots != null)
            {
                foreach (LandPlot plot in landManager.Plots) plot?.SetOwned(data.ownedPlotIds.Contains(plot.PlotId));
            }
            if (inventory != null)
            {
                inventory.ClearAll();
                foreach (InventorySlotSaveData slot in data.inventory)
                {
                    inventory.RestoreSlot(slot.slotIndex, FindItem(slot.itemId), slot.quantity);
                }
                inventory.SelectSlot(data.selectedInventorySlot);
                inventory.NotifyRestored();
            }
            farmStock?.RestoreStock(data.farmStock.low, data.farmStock.standard, data.farmStock.premium, data.farmStock.topGrade);
            utilities?.RestoreUsage(data.utilities.electricityKilowattHours, data.utilities.waterLitres, data.utilities.currentDay, 0f);
        }

        private static void RestorePlants(GameSaveData data, Dictionary<string, PlacedObject> placedObjects)
        {
            Dictionary<string, PlantingContainer> containers = new();
            foreach (PlantingContainer container in FindObjectsByType<PlantingContainer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                containers[GetContainerKey(container)] = container;
            }

            foreach (PlantSaveData saved in data.plants)
            {
                if (!containers.TryGetValue(saved.containerKey, out PlantingContainer container)) continue;
                PlantInstance plant = container.SpawnRestoredPlant();
                if (plant == null) continue;
                plant.RestoreGrowth(saved.elapsedGrowthSeconds);
                plant.RestoreCare(saved.water, saved.nutrients, saved.health);
                plant.RestoreQuality(saved.qualityScore, saved.yieldPotential, saved.accumulatedCareScore, saved.careSampleSeconds);
            }
        }

        private void RestoreLooseBatches(GameSaveData data)
        {
            foreach (HarvestBatchSaveData saved in data.looseHarvestBatches)
            {
                CreateRestoredBatch(saved, saved.position.ToVector3(), saved.rotation.ToQuaternion());
            }
        }

        private void RestoreDryingRacks(GameSaveData data, Dictionary<string, PlacedObject> placedObjects)
        {
            foreach (DryingRackSaveData savedRack in data.dryingRacks)
            {
                if (!placedObjects.TryGetValue(savedRack.placedObjectId, out PlacedObject placed)) continue;
                DryingRack rack = placed.GetComponent<DryingRack>();
                if (rack == null) continue;
                foreach (DryingBatchSaveData savedBatch in savedRack.batches)
                {
                    HarvestBatch batch = CreateRestoredBatch(savedBatch.batch, placed.transform.position, Quaternion.identity);
                    rack.RestoreBatchAtSlot(savedBatch.slotIndex, batch, savedBatch.remainingSeconds);
                }
            }
        }

        private static void RestoreStorageAndRooms(GameSaveData data, Dictionary<string, PlacedObject> placedObjects)
        {
            foreach (StorageSaveData saved in data.storageContainers)
            {
                if (placedObjects.TryGetValue(saved.placedObjectId, out PlacedObject placed)) placed.GetComponent<StorageContainer>()?.RestoreStoredKilograms(saved.storedKilograms);
            }
            foreach (RoomSaveData saved in data.rooms)
            {
                if (placedObjects.TryGetValue(saved.placedObjectId, out PlacedObject placed)) placed.GetComponent<GrowRoomEnvironment>()?.SetHumidity(saved.humidity);
            }
        }

        private void RestoreDeliveries(GameSaveData data)
        {
            if (deliveries == null) return;
            List<PendingDelivery> restored = new();
            foreach (DeliverySaveData saved in data.pendingDeliveries)
            {
                ItemDefinition item = FindItem(saved.itemId);
                if (item != null) restored.Add(PendingDelivery.CreateRestored(saved.deliveryId, item, saved.quantity, saved.remainingGameMinutes));
            }
            deliveries.ClearAndRestore(restored);
        }

        private HarvestBatch CreateRestoredBatch(HarvestBatchSaveData saved, Vector3 position, Quaternion rotation)
        {
            if (harvestBatchPrefab == null || saved == null) return null;
            GameObject instance = Instantiate(harvestBatchPrefab, position, rotation);
            HarvestBatch batch = instance.GetComponent<HarvestBatch>();
            batch?.RestoreBatch(saved.batchId, saved.weightKilograms, saved.qualityGrade, saved.status);
            return batch;
        }

        private ItemDefinition FindItem(string itemId)
        {
            if (itemCatalog == null) return null;
            foreach (ItemDefinition item in itemCatalog) if (item != null && item.ItemId == itemId) return item;
            return null;
        }

        private PlaceableDefinition FindPlaceable(string placeableId)
        {
            if (placeableCatalog == null) return null;
            foreach (PlaceableDefinition definition in placeableCatalog) if (definition != null && definition.PlaceableId == placeableId) return definition;
            return null;
        }

        private static HarvestBatchSaveData CreateBatchSaveData(HarvestBatch batch)
        {
            return new HarvestBatchSaveData
            {
                batchId = batch.BatchId,
                weightKilograms = batch.WeightKilograms,
                qualityGrade = batch.QualityGrade,
                status = batch.Status,
                position = new Vector3Data(batch.transform.position),
                rotation = new QuaternionData(batch.transform.rotation)
            };
        }

        private static string GetContainerKey(PlantingContainer container)
        {
            PlacedObject placed = container.GetComponentInParent<PlacedObject>();
            return placed != null ? $"placed:{placed.PersistentId}" : $"scene:{GetHierarchyPath(container.transform)}";
        }

        private static string GetHierarchyPath(Transform transformToDescribe)
        {
            string path = transformToDescribe.name;
            Transform current = transformToDescribe.parent;
            while (current != null)
            {
                path = $"{current.name}/{path}";
                current = current.parent;
            }
            return path;
        }
    }
}
