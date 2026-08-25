using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Growveld.Building;
using Growveld.Carrying;
using Growveld.Economy;
using Growveld.Environment;
using Growveld.Farming;
using Growveld.Inventory;
using Growveld.Saving;
using Growveld.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Growveld.Core
{
    /// <summary>
    /// Standalone-only regression path used by automated prototype builds.
    /// It restores the player's original local save before exiting.
    /// </summary>
    public sealed class PrototypeUxRuntimeSmokeTest : MonoBehaviour
    {
        private const string CommandLineFlag = "--ux-gameplay-smoke-test";
        private readonly List<string> failures = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (Application.isEditor || !System.Environment.GetCommandLineArgs().Contains(CommandLineFlag)) return;
            new GameObject("UX Gameplay Smoke Test").AddComponent<PrototypeUxRuntimeSmokeTest>();
        }

        private IEnumerator Start()
        {
            yield return null;
            yield return null;
            yield return null;
            yield return null;

            SaveSystem saveSystem = FindFirstObjectByType<SaveSystem>();
            string savePath = saveSystem != null ? saveSystem.SavePath : string.Empty;
            bool hadOriginalSave = !string.IsNullOrEmpty(savePath) && File.Exists(savePath);
            string originalSave = hadOriginalSave ? File.ReadAllText(savePath) : null;

            GameObject player = GameObject.FindWithTag("Player");
            PlayerInventory inventory = player != null ? player.GetComponent<PlayerInventory>() : null;
            PlacementController placement = player != null ? player.GetComponent<PlacementController>() : null;
            ConstructionModeController construction = player != null ? player.GetComponent<ConstructionModeController>() : null;
            PlayerCarryController carry = player != null ? player.GetComponent<PlayerCarryController>() : null;
            BusinessTabletController tablet = player != null ? player.GetComponent<BusinessTabletController>() : null;
            TabletInventoryUI tabletInventory = FindFirstObjectByType<TabletInventoryUI>(FindObjectsInactive.Include);
            ShopManager shop = FindFirstObjectByType<ShopManager>();
            ItemDefinition ceilingItem = shop?.AvailableItems?.FirstOrDefault(item => item != null && item.ItemId == "ceiling_grow_light");

            Require(saveSystem != null, "save system missing");
            Require(player != null && inventory != null && placement != null && construction != null, "player construction references missing");
            Require(tablet != null && tabletInventory != null, "tablet inventory references missing");
            Require(ceilingItem != null && ceilingItem.PlaceableDefinition != null, "ceiling grow-light item missing");
            TestFollowUpSceneConfiguration(player, tabletInventory);

            List<InventorySnapshot> originalInventory = CaptureInventory(inventory);
            if (inventory != null) inventory.ClearAll();

            if (failures.Count == 0)
            {
                yield return StartCoroutine(TestTabletPlacementEntry(inventory, ceilingItem, tablet, tabletInventory, construction, placement));
                yield return StartCoroutine(TestPlacementAndSave(inventory, ceilingItem, construction, placement, saveSystem, player));
                yield return StartCoroutine(TestInventoryDrivenPlantCare(player, inventory, shop));
                yield return StartCoroutine(TestHarvestCarry(player, carry));
                yield return StartCoroutine(TestGrowLights(ceilingItem));
                yield return StartCoroutine(TestWorldTextOcclusion());
            }

            if (hadOriginalSave && saveSystem != null)
            {
                File.WriteAllText(savePath, originalSave);
                saveSystem.LoadGame();
                yield return null;
                yield return null;
                yield return null;
                yield return null;
                File.WriteAllText(savePath, originalSave);
            }
            else
            {
                RestoreInventory(inventory, originalInventory);
                if (!string.IsNullOrEmpty(savePath) && File.Exists(savePath)) File.Delete(savePath);
            }

            if (saveSystem != null) Destroy(saveSystem);
            yield return null;

            if (failures.Count == 0)
            {
                Debug.Log("Growveld UX/gameplay runtime smoke test passed: tablet right-click placement, cancel/consume rules, construction-only move/sell, save/load persistence, grow lights, harvest carry, and world-text occlusion succeeded.");
                Application.Quit(0);
            }
            else
            {
                Debug.LogError("Growveld UX/gameplay runtime smoke test failed: " + string.Join("; ", failures));
                Application.Quit(1);
            }
        }

        private IEnumerator TestTabletPlacementEntry(
            PlayerInventory inventory,
            ItemDefinition item,
            BusinessTabletController tablet,
            TabletInventoryUI tabletInventory,
            ConstructionModeController construction,
            PlacementController placement)
        {
            Require(inventory.Add(item, 1), "could not add ceiling light for tablet test");

            tablet.SetOpen(true);
            tabletInventory.gameObject.SetActive(true);
            tabletInventory.Refresh();
            yield return null;

            TabletInventoryItemRow row = tabletInventory.GetComponentsInChildren<TabletInventoryItemRow>(true)
                .FirstOrDefault(candidate => candidate.name == $"Owned {item.DisplayName}");
            Require(row != null, "owned ceiling light did not appear in tablet inventory");
            if (row != null)
            {
                PointerEventData pointer = new(EventSystem.current)
                {
                    button = PointerEventData.InputButton.Right,
                    position = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f)
                };
                row.OnPointerClick(pointer);
                Require(tabletInventory.IsContextMenuOpen, "right-click did not open the inventory context menu");
                Canvas.ForceUpdateCanvases();
                yield return null;

                GameObject contextMenu = GetPrivateField<GameObject>(tabletInventory, "contextMenu");
                RectTransform menuRect = contextMenu != null ? contextMenu.GetComponent<RectTransform>() : null;
                RectTransform popupLayer = menuRect != null ? menuRect.parent as RectTransform : null;
                Require(popupLayer != null && popupLayer.name == "Popup Layer", "context menu is not in the top-level Popup Layer");
                if (menuRect != null && popupLayer != null)
                {
                    Vector3[] menuCorners = new Vector3[4];
                    Vector3[] layerCorners = new Vector3[4];
                    menuRect.GetWorldCorners(menuCorners);
                    popupLayer.GetWorldCorners(layerCorners);
                    Require(menuCorners.All(corner => corner.x >= layerCorners[0].x - 0.1f
                        && corner.x <= layerCorners[2].x + 0.1f
                        && corner.y >= layerCorners[0].y - 0.1f
                        && corner.y <= layerCorners[2].y + 0.1f), "context menu escaped the tablet's visible popup bounds");
                }

                Button placeButton = GetPrivateField<Button>(tabletInventory, "placeButton");
                Require(placeButton != null, "Place context action missing");
                placeButton?.onClick.Invoke();
            }

            Require(!tablet.IsOpen && placement.IsPlacing && construction.IsActive, "Place did not close the tablet and enter placement mode");
            Require(inventory.Count(item) == 1, "starting placement consumed inventory too early");
            construction.ExitMode();
            yield return null;
            Require(!placement.IsPlacing && inventory.Count(item) == 1, "cancelled placement consumed the item");
            inventory.Remove(item, 1);
        }

        private IEnumerator TestPlacementAndSave(
            PlayerInventory inventory,
            ItemDefinition item,
            ConstructionModeController construction,
            PlacementController placement,
            SaveSystem saveSystem,
            GameObject player)
        {
            Require(inventory.Add(item, 1), "could not add ceiling light for placement test");
            HashSet<string> existingIds = FindObjectsByType<PlacedObject>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .Select(placed => placed.PersistentId)
                .ToHashSet();

            construction.EnterMode();
            Require(placement.BeginInventoryPlacement(item), "inventory placement did not begin");
            Vector3 savedPosition = player.transform.position + new Vector3(7.25f, 2.9f, 7.5f);
            Require(ForceConfirmPlacement(placement, savedPosition, Quaternion.Euler(0f, 30f, 0f)), "could not drive the existing placement confirmation path");
            yield return null;

            PlacedObject placedObject = FindObjectsByType<PlacedObject>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .FirstOrDefault(candidate => candidate.Definition == item.PlaceableDefinition && !existingIds.Contains(candidate.PersistentId));
            Require(placedObject != null, "successful placement did not create the ceiling light");
            Require(inventory.Count(item) == 0, "successful placement did not consume exactly one item");

            if (placedObject == null) yield break;
            string placedId = placedObject.PersistentId;
            placedObject.transform.SetPositionAndRotation(savedPosition, Quaternion.Euler(0f, 30f, 0f));

            construction.ExitMode();
            Require(!placedObject.CanInteract(player), "placed object exposed Move/Sell outside construction mode");
            construction.EnterMode();
            Require(placedObject.CanInteract(player), "placed object was not selectable in construction mode");
            Require(item.PlaceableDefinition.PlacementSurface == PlacementSurface.Ceiling, "ceiling light is not ceiling-surface restricted");

            Require(inventory.Add(item, 2), "could not add purchased inventory for persistence test");
            Require(saveSystem.SaveGame(), "save failed during persistence test");
            saveSystem.LoadGame();
            yield return null;
            yield return null;
            yield return null;
            yield return null;

            PlacedObject restored = FindPlacedObject(placedId);
            Require(restored != null, "placed ceiling light did not survive save/load");
            Require(restored != null && Vector3.Distance(restored.transform.position, savedPosition) < 0.02f, "moved object position did not survive save/load");
            Require(inventory.Count(item) == 2, "purchased tablet inventory quantity did not survive save/load");

            construction.EnterMode();
            if (restored != null) Require(placement.SellPlacedObject(restored), "construction Sell failed");
            yield return null;
            Require(saveSystem.SaveGame(), "save after Sell failed");
            saveSystem.LoadGame();
            yield return null;
            yield return null;
            yield return null;
            yield return null;
            Require(FindPlacedObject(placedId) == null, "sold object returned after loading");
            inventory.Remove(item, inventory.Count(item));
            construction.ExitMode();
        }

        private IEnumerator TestHarvestCarry(GameObject player, PlayerCarryController carry)
        {
            PlantingContainer sourceContainer = FindFirstObjectByType<PlantingContainer>();
            GameObject plantPrefab = GetPrivateField<GameObject>(sourceContainer, "plantPrefab");
            Require(carry != null && plantPrefab != null, "harvest carry test references missing");
            if (carry == null || plantPrefab == null) yield break;

            GameObject firstPlantObject = Instantiate(plantPrefab, player.transform.position + Vector3.up * 20f, Quaternion.identity);
            PlantInstance firstPlant = firstPlantObject.GetComponent<PlantInstance>();
            firstPlant.AdvanceGrowth(float.MaxValue);
            HarvestBatch harvested = firstPlant.Harvest(player);
            Require(harvested != null && carry.IsCarrying, "harvest did not immediately enter the carry system");

            GameObject secondPlantObject = Instantiate(plantPrefab, player.transform.position + Vector3.up * 22f, Quaternion.identity);
            PlantInstance secondPlant = secondPlantObject.GetComponent<PlantInstance>();
            secondPlant.AdvanceGrowth(float.MaxValue);
            HarvestBatch blockedHarvest = secondPlant.Harvest(player);
            Require(blockedHarvest == null && secondPlant != null, "harvesting with full hands replaced or deleted the held object");

            CarryableObject carried = carry.ReleaseHeldObjectForTransfer();
            if (carried != null) Destroy(carried.gameObject);
            if (secondPlant != null) Destroy(secondPlant.gameObject);
            yield return null;
        }

        private IEnumerator TestInventoryDrivenPlantCare(GameObject player, PlayerInventory inventory, ShopManager shop)
        {
            ItemDefinition nutrients = shop?.AvailableItems?.FirstOrDefault(item => item != null && item.ItemId == "nutrients");
            ItemDefinition wateringCan = shop?.AvailableItems?.FirstOrDefault(item => item != null && item.ItemId == "watering_can");
            PlantingContainer sourceContainer = FindFirstObjectByType<PlantingContainer>();
            GameObject plantPrefab = GetPrivateField<GameObject>(sourceContainer, "plantPrefab");
            Require(nutrients != null && wateringCan != null && plantPrefab != null, "inventory-driven plant-care references missing");
            if (nutrients == null || wateringCan == null || plantPrefab == null) yield break;

            inventory.ClearAll();
            Require(inventory.Add(nutrients, 2) && inventory.Add(wateringCan, 1), "could not prepare plant-care inventory");
            int nutrientSlot = FindInventorySlot(inventory, nutrients);
            int wateringSlot = FindInventorySlot(inventory, wateringCan);

            GameObject plantObject = Instantiate(plantPrefab, player.transform.position + Vector3.up * 18f, Quaternion.identity);
            PlantInstance plant = plantObject.GetComponent<PlantInstance>();
            Require(plant != null && plant.Definition != null, "plant-care test plant definition missing");
            if (plant == null || plant.Definition == null)
            {
                Destroy(plantObject);
                inventory.ClearAll();
                yield break;
            }
            plant.RestoreCare(0f, plant.Definition.MaximumNutrients, 100f);
            inventory.SelectSlot(nutrientSlot);
            Require(plant.GetInteractionPrompt(player) == "Water plant", "low-water plant did not show the Water prompt");
            float waterBefore = plant.WaterLevel;
            plant.Interact(player);
            Require(plant.WaterLevel > waterBefore, "plant care still depends on the selected hotbar slot instead of the owned watering can");

            plant.RestoreCare(plant.Definition.MaximumWater, 0f, 100f);
            inventory.SelectSlot(wateringSlot);
            int nutrientsBefore = inventory.Count(nutrients);
            Require(plant.GetInteractionPrompt(player) == "Add nutrients", "low-nutrient plant did not show the Nutrients prompt");
            plant.Interact(player);
            Require(plant.NutrientLevel > 0f && inventory.Count(nutrients) == nutrientsBefore - 1, "inventory-driven nutrient care failed or consumed the wrong quantity");

            Destroy(plantObject);
            inventory.ClearAll();
            yield return null;
        }

        private IEnumerator TestGrowLights(ItemDefinition ceilingItem)
        {
            ItemDefinition floorItem = FindFirstObjectByType<ShopManager>()?.AvailableItems?
                .FirstOrDefault(item => item != null && item.ItemId == "grow_light");
            foreach (ItemDefinition item in new[] { floorItem, ceilingItem })
            {
                Require(item?.PlaceableDefinition?.Prefab != null, $"{item?.DisplayName ?? "grow light"} prefab missing");
                if (item?.PlaceableDefinition?.Prefab == null) continue;
                GameObject instance = Instantiate(item.PlaceableDefinition.Prefab, new Vector3(1000f, 1000f, 1000f), Quaternion.identity);
                GrowLight light = instance.GetComponent<GrowLight>();
                light?.SetExternalSchedule(true);
                Require(light != null && Mathf.Approximately(light.CoverageRadius, 6f), $"{item.DisplayName} coverage is not 6");
                Require(light != null && Mathf.Approximately(light.VisualIntensity, GrowLight.SharedVisualIntensity), $"{item.DisplayName} intensity is not {GrowLight.SharedVisualIntensity}");
                Require(light != null && Mathf.Approximately(light.VisualRange, GrowLight.SharedVisualRange), $"{item.DisplayName} range is not {GrowLight.SharedVisualRange}");
                Require(light != null && Vector4.Distance(light.VisualColor, GrowLight.SharedVisualColor) < 0.01f, $"{item.DisplayName} does not use the shared cool-white color");
                Require(light != null && light.LightSource != null && light.LightSource.enabled, $"{item.DisplayName} visual Light did not turn on");
                Require(light != null && light.LightSource != null && light.LightSource.shadows == LightShadows.Soft, $"{item.DisplayName} does not use wall-occluding soft shadows");
                Destroy(instance);
            }
            yield return null;
        }

        private IEnumerator TestWorldTextOcclusion()
        {
            GameObject label = new("Smoke Test World Label", typeof(TextMesh), typeof(WorldTextOcclusion));
            label.transform.position = new Vector3(1500f, 1f, 1502f);
            label.GetComponent<TextMesh>().text = "TEST";
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = "Smoke Test Occluding Wall";
            wall.transform.position = new Vector3(1500f, 1f, 1500f);
            wall.transform.localScale = new Vector3(4f, 4f, 0.4f);
            Physics.SyncTransforms();
            yield return null;

            WorldTextOcclusion occlusion = label.GetComponent<WorldTextOcclusion>();
            Require(!occlusion.IsVisibleFrom(new Vector3(1500f, 1f, 1498f)), "world text remained visible through a wall");
            Require(occlusion.IsVisibleFrom(new Vector3(1500f, 1f, 1504f)), "world text was hidden from its clear front side");
            Require(!occlusion.IsVisibleFrom(new Vector3(1500f, 1f, 1600f)), "world text ignored its distance limit");
            Shader shader = label.GetComponent<MeshRenderer>().material.shader;
            Require(shader != null && shader.name == "Growveld/World Text Occluded", "world text is not using the depth-tested shader");

            Destroy(label);
            Destroy(wall);
            yield return null;
        }

        private static bool ForceConfirmPlacement(PlacementController placement, Vector3 position, Quaternion rotation)
        {
            try
            {
                BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
                GameObject preview = typeof(PlacementController).GetField("previewObject", flags)?.GetValue(placement) as GameObject;
                if (preview == null) return false;
                preview.transform.SetPositionAndRotation(position, rotation);
                typeof(PlacementController).GetField("placementValid", flags)?.SetValue(placement, true);
                typeof(PlacementController).GetMethod("ConfirmPlacement", flags)?.Invoke(placement, null);
                return !placement.IsPlacing;
            }
            catch
            {
                return false;
            }
        }

        private static T GetPrivateField<T>(object instance, string fieldName) where T : class
        {
            if (instance == null) return null;
            return instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(instance) as T;
        }

        private void TestFollowUpSceneConfiguration(GameObject player, TabletInventoryUI tabletInventory)
        {
            Require(player != null && player.GetComponent<InventoryHotbarInput>() == null, "hotbar number/wheel input is still attached");
            Require(FindFirstObjectByType<InventoryHotbarUI>(FindObjectsInactive.Include) == null, "bottom hotbar UI still exists");
            Require(!FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None).Any(canvas => canvas.name == "Inventory UI"), "legacy Inventory UI root still exists");
            Require(!FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None).Any(canvas => canvas.name == "Placement UI"), "legacy construction instruction panel still exists");

            BusinessTabletUI tabletUI = FindFirstObjectByType<BusinessTabletUI>(FindObjectsInactive.Include);
            string[] names = GetPrivateField<string[]>(tabletUI, "sectionNames");
            string[] expected = { "Dashboard", "Shop", "Farm Stock", "Sell", "Finances", "Utilities", "Inventory", "Game Guide" };
            Require(names != null && names.SequenceEqual(expected), "tablet navigation pages or order are incorrect");
            Transform tabletRoot = tabletUI != null ? tabletUI.transform : null;
            Transform content = tabletRoot != null ? tabletRoot.Find("Content") : null;
            Require(tabletRoot != null && tabletRoot.Find("Land Tab") == null && tabletRoot.Find("Construction Tab") == null, "removed Land/Construction tabs still exist");
            Require(content != null && content.Find("Game Guide") != null, "Game Guide section is missing");

            InteractionPromptUI prompt = FindFirstObjectByType<InteractionPromptUI>(FindObjectsInactive.Include);
            Require(prompt != null && prompt.GetComponent<PlacementHUD>() != null, "unified contextual placement prompt is missing");
            if (prompt != null && tabletInventory != null)
            {
                PlacementHUD placementPrompt = prompt.GetComponent<PlacementHUD>();
                prompt.SetPrompt(this, "carry", InteractionPromptUI.CarryPriority);
                prompt.SetPrompt(placementPrompt, "placement", InteractionPromptUI.PlacementPriority);
                Require(prompt.CurrentMessage == "placement", "prompt priority does not prefer placement");
                prompt.ClearPrompt(placementPrompt);
                Require(prompt.CurrentMessage == "carry", "prompt priority did not fall back to carry/drop");
                prompt.ClearPrompt(this);
            }
        }

        private static PlacedObject FindPlacedObject(string persistentId)
        {
            return FindObjectsByType<PlacedObject>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .FirstOrDefault(placed => placed.PersistentId == persistentId);
        }

        private static int FindInventorySlot(PlayerInventory inventory, ItemDefinition item)
        {
            for (int index = 0; index < inventory.Slots.Count; index++)
            {
                if (inventory.Slots[index]?.Item == item) return index;
            }
            return 0;
        }

        private static List<InventorySnapshot> CaptureInventory(PlayerInventory inventory)
        {
            List<InventorySnapshot> snapshots = new();
            if (inventory == null) return snapshots;
            for (int index = 0; index < inventory.Slots.Count; index++)
            {
                InventorySlot slot = inventory.Slots[index];
                if (slot != null && !slot.IsEmpty) snapshots.Add(new InventorySnapshot(index, slot.Item, slot.Quantity));
            }
            return snapshots;
        }

        private static void RestoreInventory(PlayerInventory inventory, List<InventorySnapshot> snapshots)
        {
            if (inventory == null) return;
            inventory.ClearAll();
            foreach (InventorySnapshot snapshot in snapshots) inventory.RestoreSlot(snapshot.Index, snapshot.Item, snapshot.Quantity);
            inventory.NotifyRestored();
        }

        private void Require(bool condition, string message)
        {
            if (!condition) failures.Add(message);
        }

        private readonly struct InventorySnapshot
        {
            public InventorySnapshot(int index, ItemDefinition item, int quantity)
            {
                Index = index;
                Item = item;
                Quantity = quantity;
            }

            public int Index { get; }
            public ItemDefinition Item { get; }
            public int Quantity { get; }
        }
    }
}
