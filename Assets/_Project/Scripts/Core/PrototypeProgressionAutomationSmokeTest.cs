using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Growveld.Automation;
using Growveld.Building;
using Growveld.Carrying;
using Growveld.Economy;
using Growveld.Environment;
using Growveld.Farming;
using Growveld.Inventory;
using Growveld.Progression;
using Growveld.Saving;
using Growveld.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Growveld.Core
{
    /// <summary>Standalone full-loop regression for the strain/progression/automation expansion.</summary>
    public sealed class PrototypeProgressionAutomationSmokeTest : MonoBehaviour
    {
        private const string CommandLineFlag = "--progression-automation-smoke-test";
        private readonly List<string> failures = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (Application.isEditor || !System.Environment.GetCommandLineArgs().Contains(CommandLineFlag)) return;
            new GameObject("Progression Automation Smoke Test").AddComponent<PrototypeProgressionAutomationSmokeTest>();
        }

        private IEnumerator Start()
        {
            for (int index = 0; index < 6; index++) yield return null;

            SaveSystem save = FindFirstObjectByType<SaveSystem>();
            string savePath = save != null ? save.SavePath : string.Empty;
            bool hadOriginalSave = !string.IsNullOrEmpty(savePath) && File.Exists(savePath);
            string originalSave = hadOriginalSave ? File.ReadAllText(savePath) : null;

            GameObject player = GameObject.FindWithTag("Player");
            PlayerInventory inventory = player != null ? player.GetComponent<PlayerInventory>() : null;
            PlayerCarryController carry = player != null ? player.GetComponent<PlayerCarryController>() : null;
            ShopManager shop = FindFirstObjectByType<ShopManager>();
            DeliveryManager deliveries = FindFirstObjectByType<DeliveryManager>();
            BusinessProgression progression = FindFirstObjectByType<BusinessProgression>();
            EconomyManager economy = FindFirstObjectByType<EconomyManager>();
            FarmStockManager stock = FindFirstObjectByType<FarmStockManager>();
            SellingManager selling = FindFirstObjectByType<SellingManager>();
            UtilityManager utilities = FindFirstObjectByType<UtilityManager>();

            Require(save != null && player != null && inventory != null && carry != null, "core player/save references missing");
            Require(shop != null && deliveries != null && progression != null && economy != null && stock != null && selling != null && utilities != null, "progression economy references missing");

            List<InventorySnapshot> originalInventory = CaptureInventory(inventory);
            float originalBalance = economy != null ? economy.Balance : 0f;
            int originalLevel = progression != null ? progression.CurrentLevel : 1;
            int originalXp = progression != null ? progression.CurrentExperience : 0;
            int originalLifetimeXp = progression != null ? progression.LifetimeExperience : 0;
            float originalSales = progression != null ? progression.LifetimeSales : 0f;

            if (failures.Count == 0)
            {
                inventory.ClearAll();
                stock.ClearAll();
                economy.RestoreBalance(250000f);
                progression.Restore(1, 0, 0, 0f);
                yield return TestConfigurationAndShop(player, inventory, shop, deliveries, progression, economy);
                yield return TestFourStrainProduction(player, inventory, carry, shop, stock, selling, progression);
                yield return TestUnlockCurve(shop, progression);
                yield return TestAutomation(player, inventory, shop, utilities);
                yield return TestSavePersistence(player, inventory, shop, stock, progression, save);
            }

            if (hadOriginalSave && save != null)
            {
                File.WriteAllText(savePath, originalSave);
                save.LoadGame();
                for (int index = 0; index < 6; index++) yield return null;
                File.WriteAllText(savePath, originalSave);
            }
            else
            {
                RestoreInventory(inventory, originalInventory);
                economy?.RestoreBalance(originalBalance);
                progression?.Restore(originalLevel, originalXp, originalLifetimeXp, originalSales);
                if (!string.IsNullOrEmpty(savePath) && File.Exists(savePath)) File.Delete(savePath);
            }

            if (save != null) Destroy(save);
            yield return null;
            if (failures.Count == 0)
            {
                Debug.Log("Growveld progression/automation runtime smoke test passed: four-strain buy-grow-harvest-dry-store-sell, quality XP, unlock curve, locked shop/tooltips, irrigation, humidity, nutrient dosing, placement data, and save/load persistence succeeded.");
                Application.Quit(0);
            }
            else
            {
                Debug.LogError("Growveld progression/automation runtime smoke test failed: " + string.Join("; ", failures));
                Application.Quit(1);
            }
        }

        private IEnumerator TestConfigurationAndShop(GameObject player, PlayerInventory inventory, ShopManager shop, DeliveryManager deliveries, BusinessProgression progression, EconomyManager economy)
        {
            ItemDefinition northern = FindItem(shop, "seed_northern_lights");
            ItemDefinition blue = FindItem(shop, "seed_blue_dream");
            ItemDefinition sour = FindItem(shop, "seed_sour_diesel");
            ItemDefinition og = FindItem(shop, "seed_og_kush");
            Require(progression.CurrentLevel == 1 && progression.CurrentExperience == 0, "new business did not start at Level 1 with zero XP");
            Require(northern != null && progression.IsUnlocked(northern), "Northern Lights is not available at Level 1");
            Require(blue != null && sour != null && og != null && !progression.IsUnlocked(blue) && !progression.IsUnlocked(sour) && !progression.IsUnlocked(og), "future strains are not locked at Level 1");
            Require(shop.AvailableItems.Count(item => item?.StrainDefinition != null) == 4, "shop does not contain all four visible seed strains");
            Require(og != null && og.BuildShopTooltip().Contains("Market Value") && og.BuildShopTooltip().Contains("Level 10"), "strain tooltip is missing market/unlock guidance");

            float beforeBlocked = economy.Balance;
            Require(!shop.TryOrder(og, 1) && Mathf.Approximately(economy.Balance, beforeBlocked), "locked OG Kush could be purchased early");
            int deliveryCount = deliveries.PendingDeliveries.Count;
            Require(shop.TryOrder(northern, 1), "Northern Lights seed order failed");
            Require(deliveries.PendingDeliveries.Count == deliveryCount + 1, "paid seed order did not enter delivery queue");
            PendingDelivery delivery = deliveries.PendingDeliveries.Last();
            delivery.Advance(999f);
            yield return null;
            Require(inventory.Count(northern) == 1, "Northern Lights delivery did not enter tablet inventory");

            BusinessTabletUI tablet = FindFirstObjectByType<BusinessTabletUI>(FindObjectsInactive.Include);
            BusinessTabletController controller = player.GetComponent<BusinessTabletController>();
            controller.SetOpen(true);
            tablet.ShowSection(1);
            yield return null;
            ShopItemButton ogButton = FindObjectsByType<ShopItemButton>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .FirstOrDefault(button => button.name == "Buy OG Kush Seeds");
            TabletTooltipUI tooltip = FindFirstObjectByType<TabletTooltipUI>(FindObjectsInactive.Include);
            Require(ogButton != null && !ogButton.GetComponent<Button>().interactable, "locked OG Kush row is not visibly non-purchasable");
            if (ogButton != null)
            {
                PointerEventData pointer = new(EventSystem.current) { position = new Vector2(Screen.width * .65f, Screen.height * .55f) };
                ogButton.OnPointerEnter(pointer);
                Canvas.ForceUpdateCanvases();
                Require(tooltip != null && tooltip.IsVisible && tooltip.transform.name == "Popup Layer", "locked-item hover tooltip is not visible on the popup layer");
                ogButton.OnPointerExit(pointer);
            }
            controller.SetOpen(false);
        }

        private IEnumerator TestFourStrainProduction(GameObject player, PlayerInventory inventory, PlayerCarryController carry, ShopManager shop, FarmStockManager stock, SellingManager selling, BusinessProgression progression)
        {
            ItemDefinition potItem = FindItem(shop, "grow_pot");
            ItemDefinition rackItem = FindItem(shop, "large_drying_rack");
            ItemDefinition storageItem = FindItem(shop, "large_storage");
            Require(potItem?.PlaceableDefinition?.Prefab != null && rackItem?.PlaceableDefinition?.Prefab != null && storageItem?.PlaceableDefinition?.Prefab != null, "production test prefabs missing");
            if (potItem?.PlaceableDefinition?.Prefab == null || rackItem?.PlaceableDefinition?.Prefab == null || storageItem?.PlaceableDefinition?.Prefab == null) yield break;

            GameObject rackObject = Instantiate(rackItem.PlaceableDefinition.Prefab, new Vector3(1200f, 0f, 1200f), Quaternion.identity);
            GameObject storageObject = Instantiate(storageItem.PlaceableDefinition.Prefab, new Vector3(1203f, 0f, 1200f), Quaternion.identity);
            DryingRack rack = rackObject.GetComponent<DryingRack>();
            StorageContainer storage = storageObject.GetComponent<StorageContainer>();
            Require(rack.Capacity >= 8 && storage.CapacityKilograms >= 100f, "large drying/storage progression capacities are incorrect");

            ItemDefinition[] seeds = shop.AvailableItems.Where(item => item?.StrainDefinition != null).OrderBy(item => item.RequiredLevel).ToArray();
            foreach (ItemDefinition seed in seeds)
            {
                progression.Restore(Mathf.Max(1, seed.RequiredLevel), 0, progression.LifetimeExperience, progression.LifetimeSales);
                inventory.Add(seed, 1);
                GameObject potObject = Instantiate(potItem.PlaceableDefinition.Prefab, new Vector3(1200f + seed.RequiredLevel, 0f, 1205f), Quaternion.identity);
                PlantingContainer container = potObject.GetComponentInChildren<PlantingContainer>(true);
                Require(container != null && container.GetOwnedSeeds(inventory).Contains(seed), $"{seed.DisplayName} did not appear in available planting choices");

                if (seed == seeds[0])
                {
                    container.Interact(player);
                    yield return null;
                    SeedSelectionUI chooser = FindFirstObjectByType<SeedSelectionUI>(FindObjectsInactive.Include);
                    Button choice = chooser?.GetComponentsInChildren<Button>(true).FirstOrDefault(button => button.name == $"Plant {seed.DisplayName}");
                    Require(chooser != null && chooser.IsOpen && choice != null, "empty-pot seed selection UI did not show owned strains");
                    choice?.onClick.Invoke();
                }
                else Require(container.TryPlant(player, seed), $"could not plant {seed.DisplayName}");

                yield return null;
                PlantInstance plant = container.CurrentPlant;
                Require(plant != null && plant.Definition == seed.StrainDefinition, $"{seed.DisplayName} identity was lost at planting");
                plant.AdvanceGrowth(float.MaxValue);
                HarvestBatch batch = plant.Harvest();
                Require(batch != null && batch.Strain == seed.StrainDefinition, $"{seed.DisplayName} identity was lost at harvest");
                Require(rack.RestoreBatchAtSlot(0, batch, 0f), $"{seed.DisplayName} could not enter drying");
                rack.Slots[0].Advance(1f);
                HarvestBatch dried = rack.ReleaseFirstReadyBatch();
                Require(dried != null && dried.Status == HarvestStatus.Dried && dried.Strain == seed.StrainDefinition, $"{seed.DisplayName} identity was lost during drying");
                Require(carry.TryPickUp(dried.GetComponent<CarryableObject>()), $"could not carry dried {seed.DisplayName}");
                storage.Interact(player);
                Require(stock.GetWeight(seed.StrainDefinition, dried.QualityGrade) > 0f, $"{seed.DisplayName} was not stored separately by strain and quality");
                Destroy(potObject);
                yield return null;
            }

            Require(stock.Entries.Count(entry => entry != null && entry.WeightKilograms > 0f) == 4, "farm stock did not retain four distinct strain/quality records");
            string projected = selling.BuildProjectedSummary();
            foreach (ItemDefinition seed in seeds) Require(projected.Contains(seed.StrainDefinition.DisplayName), $"sale breakdown omitted {seed.StrainDefinition.DisplayName}");
            float value = selling.CalculateTotalSaleValue();
            int lifetimeBefore = progression.LifetimeExperience;
            Require(value > 0f && selling.SellAllStock(), "strain-aware sale failed");
            Require(selling.LastExperienceAwarded > 0 && progression.LifetimeExperience > lifetimeBefore, "successful crop sale did not award quality-weighted XP");
            Require(Mathf.Approximately(stock.TotalKilograms, 0f), "sold farm stock was not cleared");
            Destroy(rackObject); Destroy(storageObject);
            yield return null;
        }

        private IEnumerator TestUnlockCurve(ShopManager shop, BusinessProgression progression)
        {
            progression.Restore(1, 0, 0, 0f);
            Dictionary<int, string[]> unlocks = new()
            {
                [3] = new[] { "seed_blue_dream" }, [4] = new[] { "irrigation_controller" },
                [5] = new[] { "humidifier", "dehumidifier", "large_drying_rack" },
                [6] = new[] { "seed_sour_diesel", "large_storage" }, [7] = new[] { "nutrient_doser" },
                [10] = new[] { "seed_og_kush" }
            };
            while (progression.CurrentLevel < 10)
            {
                int next = progression.CurrentLevel + 1;
                progression.AddExperience(progression.ExperienceRequiredForNextLevel);
                Require(progression.CurrentLevel == next, $"XP threshold did not advance to Level {next}");
                if (unlocks.TryGetValue(next, out string[] itemIds))
                {
                    foreach (string id in itemIds) Require(progression.IsUnlocked(FindItem(shop, id)), $"{id} did not unlock automatically at Level {next}");
                }
            }
            yield return null;
        }

        private IEnumerator TestAutomation(GameObject player, PlayerInventory inventory, ShopManager shop, UtilityManager utilities)
        {
            ItemDefinition plantSeed = FindItem(shop, "seed_northern_lights");
            GameObject plantPrefab = plantSeed.StrainDefinition.PlantPrefab;
            GameObject plantObject = Instantiate(plantPrefab, new Vector3(1300f, .2f, 1300f), Quaternion.identity);
            PlantInstance plant = plantObject.GetComponent<PlantInstance>(); plant.InitialiseDefinition(plantSeed.StrainDefinition); plant.RestoreCare(0f, 0f, 100f);

            ItemDefinition irrigationItem = FindItem(shop, "irrigation_controller");
            GameObject irrigationObject = Instantiate(irrigationItem.PlaceableDefinition.Prefab, new Vector3(1300f, 0f, 1300f), Quaternion.identity);
            float waterUsageBefore = utilities.CurrentWaterLitres;
            yield return null;
            Require(plant.WaterLevel > 0f && utilities.CurrentWaterLitres > waterUsageBefore, "irrigation did not periodically water a covered plant or record farm water");
            Require(IrrigationController.FindCovering(plant.transform.position) != null, "plant did not report Irrigation Connected");

            ItemDefinition nutrients = FindItem(shop, "nutrients"); inventory.Add(nutrients, 2);
            ItemDefinition doserItem = FindItem(shop, "nutrient_doser");
            GameObject doserObject = Instantiate(doserItem.PlaceableDefinition.Prefab, new Vector3(1301f, 0f, 1300f), Quaternion.identity);
            NutrientDoser doser = doserObject.GetComponent<NutrientDoser>(); doser.Interact(player);
            float tankAfterRefill = doser.CurrentNutrients;
            yield return null;
            Require(tankAfterRefill > 0f && doser.CurrentNutrients < tankAfterRefill && plant.NutrientLevel > 0f, "nutrient doser did not refill, dose a covered plant, and consume tank supply");
            Require(NutrientDoser.FindCovering(plant.transform.position) != null, "plant did not report Nutrient Doser Connected");

            ItemDefinition roomItem = FindItem(shop, "grow_room");
            GameObject roomObject = Instantiate(roomItem.PlaceableDefinition.Prefab, new Vector3(1400f, 0f, 1400f), Quaternion.identity);
            GrowRoomEnvironment room = roomObject.GetComponent<GrowRoomEnvironment>();
            ItemDefinition humidifierItem = FindItem(shop, "humidifier");
            GameObject humidifierObject = Instantiate(humidifierItem.PlaceableDefinition.Prefab, roomObject.transform.position, Quaternion.identity);
            room.SetHumidity(40f); float lowHumidity = room.Humidity; yield return null; yield return null;
            Require(room.Humidity > lowHumidity, "humidifier did not raise existing room humidity");
            Destroy(humidifierObject); yield return null;
            ItemDefinition dehumidifierItem = FindItem(shop, "dehumidifier");
            GameObject dehumidifierObject = Instantiate(dehumidifierItem.PlaceableDefinition.Prefab, roomObject.transform.position, Quaternion.identity);
            room.SetHumidity(80f); float highHumidity = room.Humidity; yield return null; yield return null;
            Require(room.Humidity < highHumidity, "dehumidifier did not lower existing room humidity");

            Require(irrigationItem.PlaceableDefinition.CoverageRadius == 6f && doserItem.PlaceableDefinition.CoverageRadius == 6f, "automation placement coverage is not configured at six metres");
            Require(irrigationObject.transform.Find("Coverage Preview") != null && doserObject.transform.Find("Coverage Preview") != null, "coverage equipment lacks placement coverage indicators");
            Destroy(plantObject); Destroy(irrigationObject); Destroy(doserObject); Destroy(roomObject); Destroy(dehumidifierObject);
            yield return null;
        }

        private IEnumerator TestSavePersistence(GameObject player, PlayerInventory inventory, ShopManager shop, FarmStockManager stock, BusinessProgression progression, SaveSystem save)
        {
            ItemDefinition blueSeed = FindItem(shop, "seed_blue_dream");
            ItemDefinition potItem = FindItem(shop, "grow_pot");
            ItemDefinition irrigationItem = FindItem(shop, "irrigation_controller");
            ItemDefinition doserItem = FindItem(shop, "nutrient_doser");
            ItemDefinition nutrients = FindItem(shop, "nutrients");
            progression.Restore(7, 321, 2500, 45678f);
            inventory.Add(blueSeed, 3); inventory.Add(nutrients, 1);
            stock.ClearAll(); stock.AddStock(blueSeed.StrainDefinition, QualityGrade.Premium, 2.75f);

            GameObject potObject = Instantiate(potItem.PlaceableDefinition.Prefab, new Vector3(1500f, 0f, 1500f), Quaternion.identity);
            PlantingContainer container = potObject.GetComponentInChildren<PlantingContainer>(true); container.TryPlant(player, blueSeed); container.CurrentPlant.RestoreGrowth(345f);
            GameObject irrigationObject = Instantiate(irrigationItem.PlaceableDefinition.Prefab, new Vector3(1502f, 0f, 1500f), Quaternion.identity);
            GameObject doserObject = Instantiate(doserItem.PlaceableDefinition.Prefab, new Vector3(1504f, 0f, 1500f), Quaternion.identity);
            PlacedObject potPlaced = potObject.GetComponent<PlacedObject>(); PlacedObject irrigationPlaced = irrigationObject.GetComponent<PlacedObject>(); PlacedObject doserPlaced = doserObject.GetComponent<PlacedObject>();
            string potId = potPlaced.PersistentId; string irrigationId = irrigationPlaced.PersistentId; string doserId = doserPlaced.PersistentId;
            NutrientDoser doser = doserObject.GetComponent<NutrientDoser>(); doser.Interact(player); float savedTank = doser.CurrentNutrients;

            Require(save.SaveGame(), "expanded save could not be written");
            progression.Restore(1, 0, 0, 0f); stock.ClearAll(); inventory.ClearAll();
            save.LoadGame();
            for (int index = 0; index < 7; index++) yield return null;

            Require(progression.CurrentLevel == 7 && progression.CurrentExperience == 321 && progression.LifetimeExperience == 2500 && Mathf.Approximately(progression.LifetimeSales, 45678f), "business progression did not survive save/load");
            Require(inventory.Count(blueSeed) == 2, "strain seed inventory did not survive save/load");
            Require(Mathf.Approximately(stock.GetWeight(blueSeed.StrainDefinition, QualityGrade.Premium), 2.75f), "strain/quality farm stock did not survive save/load");
            PlacedObject restoredPot = FindPlaced(potId); PlacedObject restoredIrrigation = FindPlaced(irrigationId); PlacedObject restoredDoser = FindPlaced(doserId);
            PlantInstance restoredPlant = restoredPot != null ? restoredPot.GetComponentInChildren<PlantInstance>(true) : null;
            Require(restoredPlant != null && restoredPlant.Definition == blueSeed.StrainDefinition && Mathf.Abs(restoredPlant.ElapsedGrowthSeconds - 345f) < 0.1f, "planted strain identity/growth did not survive save/load");
            Require(restoredIrrigation?.GetComponent<IrrigationController>() != null, "irrigation equipment did not survive save/load");
            Require(restoredDoser?.GetComponent<NutrientDoser>() != null && Mathf.Abs(restoredDoser.GetComponent<NutrientDoser>().CurrentNutrients - savedTank) < .1f, "nutrient tank state did not survive save/load");
        }

        private static ItemDefinition FindItem(ShopManager shop, string id) => shop?.AvailableItems?.FirstOrDefault(item => item != null && item.ItemId == id);
        private static PlacedObject FindPlaced(string id) => FindObjectsByType<PlacedObject>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).FirstOrDefault(placed => placed.PersistentId == id);
        private void Require(bool condition, string message) { if (!condition) failures.Add(message); }

        private static List<InventorySnapshot> CaptureInventory(PlayerInventory inventory)
        {
            List<InventorySnapshot> result = new(); if (inventory == null) return result;
            for (int index = 0; index < inventory.Slots.Count; index++) { InventorySlot slot = inventory.Slots[index]; if (slot != null && !slot.IsEmpty) result.Add(new InventorySnapshot(index, slot.Item, slot.Quantity)); }
            return result;
        }

        private static void RestoreInventory(PlayerInventory inventory, List<InventorySnapshot> values)
        {
            if (inventory == null) return; inventory.ClearAll(); foreach (InventorySnapshot value in values) inventory.RestoreSlot(value.Index, value.Item, value.Quantity); inventory.NotifyRestored();
        }

        private readonly struct InventorySnapshot
        {
            public InventorySnapshot(int index, ItemDefinition item, int quantity) { Index = index; Item = item; Quantity = quantity; }
            public int Index { get; }
            public ItemDefinition Item { get; }
            public int Quantity { get; }
        }
    }
}
