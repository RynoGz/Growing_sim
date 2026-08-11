using System;
using System.Collections.Generic;
using Growveld.Farming;
using UnityEngine;

namespace Growveld.Saving
{
    [Serializable]
    public struct Vector3Data
    {
        public float x;
        public float y;
        public float z;

        public Vector3Data(Vector3 value) { x = value.x; y = value.y; z = value.z; }
        public Vector3 ToVector3() => new(x, y, z);
    }

    [Serializable]
    public struct QuaternionData
    {
        public float x;
        public float y;
        public float z;
        public float w;

        public QuaternionData(Quaternion value) { x = value.x; y = value.y; z = value.z; w = value.w; }
        public Quaternion ToQuaternion() => new(x, y, z, w);
    }

    [Serializable]
    public sealed class GameSaveData
    {
        public int version = 1;
        public string savedAtUtc;
        public float balance;
        public int day;
        public float timeOfDayHours;
        public Vector3Data playerPosition;
        public QuaternionData playerRotation;
        public List<string> ownedPlotIds = new();
        public List<InventorySlotSaveData> inventory = new();
        public int selectedInventorySlot;
        public List<PlacedObjectSaveData> placedObjects = new();
        public List<PlantSaveData> plants = new();
        public List<HarvestBatchSaveData> looseHarvestBatches = new();
        public List<DryingRackSaveData> dryingRacks = new();
        public List<StorageSaveData> storageContainers = new();
        public List<RoomSaveData> rooms = new();
        public List<DeliverySaveData> pendingDeliveries = new();
        public FarmStockSaveData farmStock = new();
        public UtilitySaveData utilities = new();
    }

    [Serializable]
    public sealed class InventorySlotSaveData
    {
        public int slotIndex;
        public string itemId;
        public int quantity;
    }

    [Serializable]
    public sealed class PlacedObjectSaveData
    {
        public string persistentId;
        public string placeableId;
        public Vector3Data position;
        public QuaternionData rotation;
    }

    [Serializable]
    public sealed class PlantSaveData
    {
        public string containerKey;
        public float elapsedGrowthSeconds;
        public float water;
        public float nutrients;
        public float health;
        public float qualityScore;
        public float yieldPotential;
        public float accumulatedCareScore;
        public float careSampleSeconds;
    }

    [Serializable]
    public sealed class HarvestBatchSaveData
    {
        public string batchId;
        public float weightKilograms;
        public QualityGrade qualityGrade;
        public HarvestStatus status;
        public Vector3Data position;
        public QuaternionData rotation;
    }

    [Serializable]
    public sealed class DryingRackSaveData
    {
        public string placedObjectId;
        public List<DryingBatchSaveData> batches = new();
    }

    [Serializable]
    public sealed class DryingBatchSaveData
    {
        public int slotIndex;
        public float remainingSeconds;
        public HarvestBatchSaveData batch;
    }

    [Serializable]
    public sealed class StorageSaveData
    {
        public string placedObjectId;
        public float storedKilograms;
    }

    [Serializable]
    public sealed class RoomSaveData
    {
        public string placedObjectId;
        public float humidity;
    }

    [Serializable]
    public sealed class DeliverySaveData
    {
        public string deliveryId;
        public string itemId;
        public int quantity;
        public float remainingGameMinutes;
    }

    [Serializable]
    public sealed class FarmStockSaveData
    {
        public float low;
        public float standard;
        public float premium;
        public float topGrade;
    }

    [Serializable]
    public sealed class UtilitySaveData
    {
        public float electricityKilowattHours;
        public float waterLitres;
        public int currentDay;
    }
}
