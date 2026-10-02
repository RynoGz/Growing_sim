using System;
using System.Collections.Generic;
using System.Linq;
using Growveld.Automation;
using Growveld.Building;
using Growveld.Economy;
using Growveld.Farming;
using Growveld.Inventory;
using Growveld.Progression;
using Growveld.Saving;
using Growveld.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Growveld.Editor
{
    public static class Phase27ProgressionAutomationSetup
    {
        private const string ScenePath = "Assets/_Project/Scenes/PrototypeFarm.unity";
        private const string PlantPrefabPath = "Assets/_Project/Prefabs/Plants/Generic Plant.prefab";
        private const string QualityPath = "Assets/_Project/ScriptableObjects/Plants/QualitySettings.asset";
        private const string ProgressionPath = "Assets/_Project/ScriptableObjects/Progression/BusinessProgressionSettings.asset";
        private const string ItemFolder = "Assets/_Project/ScriptableObjects/Items";
        private const string PlaceableFolder = "Assets/_Project/ScriptableObjects/Placeables";
        private const string EquipmentFolder = "Assets/_Project/Prefabs/Equipment";

        private static readonly string[] StrainIds = { "northern_lights", "blue_dream", "sour_diesel", "og_kush" };

        [MenuItem("Growveld/Phase 27/Apply Strains Levels and Automation")]
        public static void ConfigureExpansion()
        {
            EnsureFolder("Assets/_Project/ScriptableObjects", "Progression");
            BusinessProgressionSettings progressionSettings = ConfigureProgressionSettings();
            PlantDefinition[] strains = ConfigureStrains();
            ItemDefinition[] seedItems = ConfigureSeedItems(strains);
            ConfigurePlantPrefab(strains[0]);

            ItemDefinition nutrientItem = AssetDatabase.LoadAssetAtPath<ItemDefinition>($"{ItemFolder}/Item_nutrients.asset");
            ConfigureAutomationAndUpgrades(nutrientItem);

            ItemDefinition[] allItems = LoadAll<ItemDefinition>(ItemFolder)
                .OrderBy(item => item.RequiredLevel).ThenBy(item => item.Category).ThenBy(item => item.DisplayName).ToArray();
            PlaceableDefinition[] allPlaceables = LoadAll<PlaceableDefinition>(PlaceableFolder)
                .OrderBy(placeable => placeable.PlaceableId).ToArray();

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject systems = FindRoot(scene, "Game Systems");
            GameObject player = FindRoot(scene, "Player");
            GameObject tabletCanvas = FindRoot(scene, "Business Tablet UI");
            if (systems == null || player == null || tabletCanvas == null) throw new MissingReferenceException("Phase 27 requires Game Systems, Player, and Business Tablet UI.");

            BusinessProgression progression = systems.GetComponent<BusinessProgression>() ?? systems.AddComponent<BusinessProgression>();
            SetObject(progression, "settings", progressionSettings);
            SetObjectArray(progression, "unlockCatalog", allItems);

            ShopManager shop = systems.GetComponent<ShopManager>();
            SetObjectArray(shop, "availableItems", allItems);
            SetObject(shop, "progression", progression);

            FarmStockManager stock = systems.GetComponent<FarmStockManager>();
            SetObject(stock, "defaultStrain", strains[0]);

            SellingManager selling = systems.GetComponent<SellingManager>();
            SetObject(selling, "progression", progression);

            SaveSystem save = systems.GetComponent<SaveSystem>();
            SetObject(save, "progression", progression);
            SetObjectArray(save, "itemCatalog", allItems);
            SetObjectArray(save, "placeableCatalog", allPlaceables);
            SetObjectArray(save, "strainCatalog", strains);
            SetObject(save, "defaultStrain", strains[0]);

            ProgressionNotificationUI notification = systems.GetComponent<ProgressionNotificationUI>() ?? systems.AddComponent<ProgressionNotificationUI>();
            SetObject(notification, "progression", progression);

            ConfigurePlantingContainers(scene, strains[0], seedItems);
            ConfigureTablet(tabletCanvas, systems, progression, shop, stock);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            ValidateExpansion();
            Debug.Log("Growveld Phase 27 setup complete: four strains, business XP/unlocks, strain-aware production, irrigation, humidity control, nutrient dosing, and capacity upgrades configured.");
        }

        [MenuItem("Growveld/Phase 27/Validate Strains Levels and Automation")]
        public static void ValidateExpansion()
        {
            List<string> failures = new();
            PlantDefinition[] strains = LoadAll<PlantDefinition>("Assets/_Project/ScriptableObjects/Plants")
                .Where(strain => StrainIds.Contains(strain.PlantId)).OrderBy(strain => strain.UnlockLevel).ToArray();
            if (strains.Length != 4) failures.Add("exactly four configured strain assets were not found");
            int[] strainUnlocks = { 1, 3, 6, 10 };
            for (int index = 0; index < Mathf.Min(strains.Length, strainUnlocks.Length); index++)
            {
                if (strains[index].UnlockLevel != strainUnlocks[index]) failures.Add($"{strains[index].DisplayName} unlock level is incorrect");
                if (strains[index].PlantPrefab == null) failures.Add($"{strains[index].DisplayName} plant prefab is missing");
            }

            BusinessProgressionSettings settings = AssetDatabase.LoadAssetAtPath<BusinessProgressionSettings>(ProgressionPath);
            int[] expectedXp = { 100, 175, 275, 400, 550, 750, 1000, 1300, 1650 };
            if (settings == null) failures.Add("progression settings asset missing");
            else for (int level = 1; level <= expectedXp.Length; level++) if (settings.GetExperienceRequired(level) != expectedXp[level - 1]) failures.Add($"level {level} XP target incorrect");

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject systems = FindRoot(scene, "Game Systems");
            BusinessProgression progression = systems != null ? systems.GetComponent<BusinessProgression>() : null;
            ShopManager shop = systems != null ? systems.GetComponent<ShopManager>() : null;
            SaveSystem save = systems != null ? systems.GetComponent<SaveSystem>() : null;
            if (progression == null || shop == null || save == null) failures.Add("progression/shop/save services are incomplete");
            if (shop != null)
            {
                Dictionary<string, int> expected = new()
                {
                    ["seed_northern_lights"] = 1, ["seed_blue_dream"] = 3, ["irrigation_controller"] = 4,
                    ["humidifier"] = 5, ["dehumidifier"] = 5, ["large_drying_rack"] = 5,
                    ["seed_sour_diesel"] = 6, ["large_storage"] = 6, ["nutrient_doser"] = 7,
                    ["seed_og_kush"] = 10
                };
                foreach ((string id, int level) in expected)
                {
                    ItemDefinition item = shop.AvailableItems.FirstOrDefault(candidate => candidate != null && candidate.ItemId == id);
                    if (item == null || item.RequiredLevel != level) failures.Add($"shop unlock missing or incorrect: {id}");
                }
            }

            ValidateEquipment<IrrigationController>("Irrigation Controller.prefab", 6f, failures);
            ValidateEquipment<NutrientDoser>("Nutrient Doser.prefab", 6f, failures);
            ValidateEquipment<HumidityController>("Humidifier.prefab", 0f, failures);
            ValidateEquipment<HumidityController>("Dehumidifier.prefab", 0f, failures);
            GameObject largeRack = AssetDatabase.LoadAssetAtPath<GameObject>($"{EquipmentFolder}/Large Drying Rack.prefab");
            if (largeRack == null || GetArraySize(largeRack.GetComponent<DryingRack>(), "slotAnchors") < 8) failures.Add("large drying rack does not have eight slots");
            GameObject largeStorage = AssetDatabase.LoadAssetAtPath<GameObject>($"{EquipmentFolder}/Large Storage.prefab");
            if (largeStorage == null || largeStorage.GetComponent<StorageContainer>()?.CapacityKilograms < 100f) failures.Add("large storage capacity is below 100 kg");

            Transform tablet = FindRoot(scene, "Business Tablet UI")?.transform.Find("Tablet");
            if (tablet?.Find("Popup Layer/Shop Tooltip") == null) failures.Add("shop tooltip is not under the tablet popup layer");
            if (tablet?.Find("Content/Dashboard")?.GetComponent<BusinessProgressionDashboard>() == null) failures.Add("dashboard progression display missing");
            if (tablet?.Find("Content/Shop")?.GetComponentInChildren<ScrollRect>(true) == null) failures.Add("scrollable shop UI missing");
            if (UnityEngine.Object.FindObjectsByType<PlantingContainer>(FindObjectsInactive.Include, FindObjectsSortMode.None).Any(container => GetArraySize(container, "seedCatalog") != 4)) failures.Add("a planting container does not expose all four seed types");

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                int missing = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(root);
                if (missing > 0) failures.Add($"{missing} missing script(s) under {root.name}");
            }
            if (failures.Count > 0) throw new InvalidOperationException("Phase 27 validation failed:\n- " + string.Join("\n- ", failures));
            Debug.Log("Growveld Phase 27 validation passed: data assets, XP curve, unlock table, shop/tooltips, plant seed catalog, automation prefabs, upgrades, and save services are configured.");
        }

        private static BusinessProgressionSettings ConfigureProgressionSettings()
        {
            BusinessProgressionSettings settings = LoadOrCreate<BusinessProgressionSettings>(ProgressionPath);
            SerializedObject serialized = new(settings);
            int[] curve = { 100, 175, 275, 400, 550, 750, 1000, 1300, 1650 };
            SerializedProperty levels = serialized.FindProperty("experiencePerLevel");
            levels.arraySize = curve.Length;
            for (int index = 0; index < curve.Length; index++) levels.GetArrayElementAtIndex(index).intValue = curve[index];
            serialized.FindProperty("baseExperiencePerKilogram").floatValue = 50f;
            serialized.FindProperty("lowQualityMultiplier").floatValue = 0.75f;
            serialized.FindProperty("standardQualityMultiplier").floatValue = 1f;
            serialized.FindProperty("premiumQualityMultiplier").floatValue = 1.25f;
            serialized.FindProperty("topGradeQualityMultiplier").floatValue = 1.5f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(settings);
            return settings;
        }

        private static PlantDefinition[] ConfigureStrains()
        {
            Growveld.Farming.QualitySettings quality = AssetDatabase.LoadAssetAtPath<Growveld.Farming.QualitySettings>(QualityPath);
            GameObject plantPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlantPrefabPath);
            return new[]
            {
                ConfigureStrain("Strain_NorthernLights.asset", "northern_lights", "Northern Lights", "A classic, forgiving strain and a good choice for a new grower. It performs especially well indoors and provides reliable yields without demanding perfect conditions.", "Easy and forgiving; strongest indoors.", "Easy", "Fast", "High", "Excellent", "Good", 1, 180f, 1200f, new[]{90f,180f,360f,570f}, .55f, 1.12f, .95f, 1f, .95f, 50f, 68f, .75f, quality, plantPrefab),
                ConfigureStrain("Strain_BlueDream.asset", "blue_dream", "Blue Dream", "A popular hybrid known for strong yields and versatility. It performs well both indoors and outdoors, making it a dependable option when expanding your farm.", "Easy-Moderate; versatile high yield.", "Easy-Moderate", "Medium", "High", "Very Good", "Very Good", 3, 300f, 1550f, new[]{120f,240f,420f,720f}, .72f, 1.05f, 1.05f, 1.1f, 1.15f, 52f, 66f, .9f, quality, plantPrefab),
                ConfigureStrain("Strain_SourDiesel.asset", "sour_diesel", "Sour Diesel", "A higher-value strain with strong growth potential, but it rewards growers who maintain good conditions. Best suited to an established farm with reliable water, nutrients and environmental control.", "Moderate; higher demand and strong outdoors.", "Moderate", "Slow", "High", "Good", "Excellent", 6, 480f, 2050f, new[]{150f,270f,480f,900f}, .68f, 1f, 1.12f, 1.18f, 1.3f, 50f, 62f, 1.1f, quality, plantPrefab),
                ConfigureStrain("Strain_OGKush.asset", "og_kush", "OG Kush", "A premium crop for experienced growers. It commands a high market value but requires consistent care to achieve Premium or Top Grade harvests.", "Hard; narrow tolerances and premium value.", "Hard", "Slow", "Medium", "Excellent", "Good", 10, 750f, 2850f, new[]{150f,300f,540f,960f}, .58f, 1.15f, .9f, 1.25f, 1.35f, 52f, 60f, 1.3f, quality, plantPrefab)
            };
        }

        private static PlantDefinition ConfigureStrain(string file, string id, string displayName, string description, string guidance, string difficulty, string growth, string yield, string indoor, string outdoor, int unlock, float seedPrice, float salePrice, float[] stages, float baseYield, float indoorModifier, float outdoorModifier, float waterModifier, float nutrientModifier, float humidityMin, float humidityMax, float sensitivity, Growveld.Farming.QualitySettings quality, GameObject prefab)
        {
            PlantDefinition strain = LoadOrCreate<PlantDefinition>($"Assets/_Project/ScriptableObjects/Plants/{file}");
            SerializedObject data = new(strain);
            Set(data, "plantId", id); Set(data, "displayName", displayName); Set(data, "shortDescription", description); Set(data, "guidanceText", guidance);
            Set(data, "difficulty", difficulty); Set(data, "growthLabel", growth); Set(data, "yieldLabel", yield); Set(data, "indoorSuitability", indoor); Set(data, "outdoorSuitability", outdoor);
            Set(data, "unlockLevel", unlock); Set(data, "seedPurchasePrice", seedPrice); Set(data, "baseSellingPricePerKilogram", salePrice); Set(data, "maximumQualityPotential", 100f);
            Set(data, "germinationSeconds", stages[0]); Set(data, "seedlingSeconds", stages[1]); Set(data, "vegetativeSeconds", stages[2]); Set(data, "floweringSeconds", stages[3]); Set(data, "baseYieldKilograms", baseYield);
            Set(data, "indoorGrowthModifier", indoorModifier); Set(data, "outdoorGrowthModifier", outdoorModifier); Set(data, "waterConsumptionModifier", waterModifier); Set(data, "nutrientConsumptionModifier", nutrientModifier);
            Set(data, "preferredHumidityMinimum", humidityMin); Set(data, "preferredHumidityMaximum", humidityMax); Set(data, "qualitySensitivity", sensitivity);
            Set(data, "waterConsumptionPerRealMinute", 4f); Set(data, "nutrientConsumptionPerRealMinute", 2f); Set(data, "qualitySettings", quality); Set(data, "plantPrefab", prefab);
            data.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(strain);
            return strain;
        }

        private static ItemDefinition[] ConfigureSeedItems(PlantDefinition[] strains)
        {
            string[] paths =
            {
                $"{ItemFolder}/Item_seed.asset", $"{ItemFolder}/Item_seed_blue_dream.asset",
                $"{ItemFolder}/Item_seed_sour_diesel.asset", $"{ItemFolder}/Item_seed_og_kush.asset"
            };
            ItemDefinition[] items = new ItemDefinition[strains.Length];
            for (int index = 0; index < strains.Length; index++)
            {
                PlantDefinition strain = strains[index];
                ItemDefinition item = LoadOrCreate<ItemDefinition>(paths[index]);
                SerializedObject data = new(item);
                Set(data, "itemId", $"seed_{strain.PlantId}"); Set(data, "displayName", $"{strain.DisplayName} Seeds"); Set(data, "description", strain.GuidanceText);
                Set(data, "category", (int)ItemCategory.Seeds); Set(data, "stackable", true); Set(data, "maximumStack", 50); Set(data, "purchasePrice", strain.SeedPurchasePrice);
                Set(data, "displayColor", SeedColour(index)); Set(data, "requiredLevel", strain.UnlockLevel); Set(data, "strainDefinition", strain); Set(data, "placeableDefinition", null);
                data.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(item);
                items[index] = item;
            }
            return items;
        }

        private static void ConfigurePlantPrefab(PlantDefinition defaultStrain)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(PlantPrefabPath);
            SetObject(root.GetComponent<PlantInstance>(), "definition", defaultStrain);
            PrefabUtility.SaveAsPrefabAsset(root, PlantPrefabPath);
            PrefabUtility.UnloadPrefabContents(root);
        }

        private static void ConfigureAutomationAndUpgrades(ItemDefinition nutrientItem)
        {
            ConfigureAutomation<IrrigationController>("Irrigation Controller", "irrigation_controller", ItemCategory.Equipment, 4, 8500f,
                "Automatically waters plants inside its coverage area, reducing manual watering.", "Coverage: Medium\nWater Usage: Moderate\nElectricity Usage: Low", 6f, new Color(.18f, .5f, .78f), null);
            ConfigureAutomation<HumidityController>("Humidifier", "humidifier", ItemCategory.Equipment, 5, 6200f,
                "Raises grow-room humidity toward the recommended strain target.", "Room device\nElectricity Usage: Low\nAutomatic recommended target", 0f, new Color(.38f, .72f, .92f), device => Set(device, "mode", (int)HumidityControlMode.Humidify));
            ConfigureAutomation<HumidityController>("Dehumidifier", "dehumidifier", ItemCategory.Equipment, 5, 6800f,
                "Lowers grow-room humidity toward the recommended strain target.", "Room device\nElectricity Usage: Low\nAutomatic recommended target", 0f, new Color(.72f, .82f, .9f), device => Set(device, "mode", (int)HumidityControlMode.Dehumidify));
            ConfigureAutomation<NutrientDoser>("Nutrient Doser", "nutrient_doser", ItemCategory.Equipment, 7, 14000f,
                "Automatically maintains nutrients for covered plants. Refill it with Generic Nutrients.", "Capacity: 100\nCoverage: Medium\nElectricity Usage: Low", 6f, new Color(.54f, .3f, .76f), device => { Set(device, "tankCapacity", 100f); Set(device, "currentNutrients", 0f); Set(device, "nutrientItem", nutrientItem); });
            ConfigureLargeRack();
            ConfigureLargeStorage();
        }

        private static void ConfigureAutomation<T>(string displayName, string id, ItemCategory category, int level, float price, string description, string details, float coverage, Color colour, Action<SerializedObject> configure) where T : AutomationEquipment
        {
            string prefabPath = $"{EquipmentFolder}/{displayName}.prefab";
            bool existed = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null;
            GameObject root = existed ? PrefabUtility.LoadPrefabContents(prefabPath) : new GameObject(displayName);
            for (int index = root.transform.childCount - 1; index >= 0; index--) UnityEngine.Object.DestroyImmediate(root.transform.GetChild(index).gameObject);
            BoxCollider collider = root.GetComponent<BoxCollider>();
            if (collider == null) collider = root.AddComponent<BoxCollider>();
            collider.center = new Vector3(0f, .65f, 0f); collider.size = new Vector3(1.2f, 1.3f, 1.2f);
            PlacedObject placed = root.GetComponent<PlacedObject>();
            if (placed == null) placed = root.AddComponent<PlacedObject>();
            T device = root.GetComponent<T>();
            if (device == null) device = root.AddComponent<T>();
            SerializedObject deviceData = new(device);
            Set(deviceData, "operational", true); Set(deviceData, "coverageRadius", coverage); Set(deviceData, "powerConsumptionKilowatts", typeof(T) == typeof(HumidityController) ? .35f : .15f);
            configure?.Invoke(deviceData);
            deviceData.ApplyModifiedPropertiesWithoutUndo();
            CreatePrimitiveVisual(root.transform, "Body", PrimitiveType.Cube, new Vector3(0f, .65f, 0f), new Vector3(1.15f, 1.3f, 1.15f), colour);
            CreatePrimitiveVisual(root.transform, "Status", PrimitiveType.Cube, new Vector3(0f, 1.35f, -.36f), new Vector3(.6f, .12f, .18f), new Color(.25f, .95f, .4f));
            if (coverage > 0f) CreateCoveragePreview(root.transform);
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            if (existed) PrefabUtility.UnloadPrefabContents(root); else UnityEngine.Object.DestroyImmediate(root);

            ItemDefinition item = ConfigureItem($"{ItemFolder}/Item_{id}.asset", id, displayName, category, level, price, description, details);
            PlaceableDefinition placeable = ConfigurePlaceable($"{PlaceableFolder}/Placeable_{id}.asset", id, item, AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath), new Vector3(1.3f, 1.4f, 1.3f), coverage);
            SetObject(item, "placeableDefinition", placeable);
            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
            SetObject(prefabRoot.GetComponent<PlacedObject>(), "definition", placeable);
            PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }

        private static void ConfigureLargeRack()
        {
            string path = $"{EquipmentFolder}/Large Drying Rack.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null) AssetDatabase.CopyAsset($"{EquipmentFolder}/Drying Rack.prefab", path);
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            DryingRack rack = root.GetComponent<DryingRack>();
            Transform old = root.transform.Find("Drying Slots"); if (old != null) UnityEngine.Object.DestroyImmediate(old.gameObject);
            GameObject slotsRoot = new("Drying Slots"); slotsRoot.transform.SetParent(root.transform, false);
            Transform[] anchors = new Transform[8];
            for (int index = 0; index < anchors.Length; index++)
            {
                GameObject anchor = new($"Batch Slot {index + 1}"); anchor.transform.SetParent(slotsRoot.transform, false);
                int row = index / 2; int column = index % 2;
                anchor.transform.localPosition = new Vector3(column == 0 ? -.72f : .72f, .42f + row * .48f, -.42f);
                anchors[index] = anchor.transform;
            }
            SerializedObject rackData = new(rack); Set(rackData, "dryingDurationSeconds", 540f); SetObjectArray(rackData, "slotAnchors", anchors); rackData.FindProperty("slots").ClearArray(); rackData.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, path); PrefabUtility.UnloadPrefabContents(root);
            ItemDefinition item = ConfigureItem($"{ItemFolder}/Item_large_drying_rack.asset", "large_drying_rack", "Large Drying Rack", ItemCategory.Drying, 5, 9000f, "An eight-slot rack for larger harvest batches.", "Capacity: 8 batches\nDrying Speed: Slightly Faster");
            PlaceableDefinition placeable = ConfigurePlaceable($"{PlaceableFolder}/Placeable_large_drying_rack.asset", "large_drying_rack", item, AssetDatabase.LoadAssetAtPath<GameObject>(path), new Vector3(2.2f, 2.4f, 1.3f), 0f);
            SetObject(item, "placeableDefinition", placeable); SetPrefabDefinition(path, placeable);
        }

        private static void ConfigureLargeStorage()
        {
            string path = $"{EquipmentFolder}/Large Storage.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null) AssetDatabase.CopyAsset($"{EquipmentFolder}/Storage Bin.prefab", path);
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            SerializedObject storageData = new(root.GetComponent<StorageContainer>()); Set(storageData, "capacityKilograms", 100f); storageData.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, path); PrefabUtility.UnloadPrefabContents(root);
            ItemDefinition item = ConfigureItem($"{ItemFolder}/Item_large_storage.asset", "large_storage", "Large Storage", ItemCategory.Storage, 6, 7500f, "A higher-capacity storage bin for an expanding farm.", "Capacity: 100 kg\nNo product degradation");
            PlaceableDefinition placeable = ConfigurePlaceable($"{PlaceableFolder}/Placeable_large_storage.asset", "large_storage", item, AssetDatabase.LoadAssetAtPath<GameObject>(path), new Vector3(2f, 1.5f, 1.6f), 0f);
            SetObject(item, "placeableDefinition", placeable); SetPrefabDefinition(path, placeable);
        }

        private static ItemDefinition ConfigureItem(string path, string id, string displayName, ItemCategory category, int level, float price, string description, string details)
        {
            ItemDefinition item = LoadOrCreate<ItemDefinition>(path);
            SerializedObject data = new(item);
            Set(data, "itemId", id); Set(data, "displayName", displayName); Set(data, "description", description); Set(data, "category", (int)category); Set(data, "stackable", true); Set(data, "maximumStack", 10); Set(data, "purchasePrice", price); Set(data, "requiredLevel", level); Set(data, "tooltipDetails", details);
            data.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(item); return item;
        }

        private static PlaceableDefinition ConfigurePlaceable(string path, string id, ItemDefinition item, GameObject prefab, Vector3 footprint, float coverage)
        {
            PlaceableDefinition placeable = LoadOrCreate<PlaceableDefinition>(path);
            SerializedObject data = new(placeable);
            Set(data, "placeableId", id); Set(data, "itemDefinition", item); Set(data, "prefab", prefab); Set(data, "footprintSize", footprint); Set(data, "placementOffset", Vector3.zero); Set(data, "placementSurface", (int)PlacementSurface.Floor); Set(data, "rotationStep", 15f); Set(data, "sellRefundFraction", .7f); Set(data, "automationCoverageRadius", coverage);
            data.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(placeable); return placeable;
        }

        private static void SetPrefabDefinition(string path, PlaceableDefinition definition)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(path); SetObject(root.GetComponent<PlacedObject>(), "definition", definition); PrefabUtility.SaveAsPrefabAsset(root, path); PrefabUtility.UnloadPrefabContents(root);
        }

        private static void ConfigurePlantingContainers(Scene scene, PlantDefinition defaultStrain, ItemDefinition[] seeds)
        {
            GameObject growPot = PrefabUtility.LoadPrefabContents($"{EquipmentFolder}/Grow Pot.prefab");
            ConfigureContainer(growPot.GetComponent<PlantingContainer>(), defaultStrain, seeds);
            PrefabUtility.SaveAsPrefabAsset(growPot, $"{EquipmentFolder}/Grow Pot.prefab"); PrefabUtility.UnloadPrefabContents(growPot);
            foreach (PlantingContainer container in UnityEngine.Object.FindObjectsByType<PlantingContainer>(FindObjectsInactive.Include, FindObjectsSortMode.None)) ConfigureContainer(container, defaultStrain, seeds);
        }

        private static void ConfigureContainer(PlantingContainer container, PlantDefinition defaultStrain, ItemDefinition[] seeds)
        {
            if (container == null) return;
            SetObject(container, "plantDefinition", defaultStrain); SetObject(container, "seedItem", seeds[0]); SetObjectArray(container, "seedCatalog", seeds); SetObject(container, "plantPrefab", AssetDatabase.LoadAssetAtPath<GameObject>(PlantPrefabPath));
        }

        private static void ConfigureTablet(GameObject tabletCanvas, GameObject systems, BusinessProgression progression, ShopManager shop, FarmStockManager stock)
        {
            Transform tablet = tabletCanvas.transform.Find("Tablet"); Transform content = tablet.Find("Content"); Transform popupLayer = tablet.Find("Popup Layer");
            ConfigureDashboard(content.Find("Dashboard"), progression, systems.GetComponent<EconomyManager>(), shop);
            TabletTooltipUI tooltip = ConfigureTooltip(popupLayer);
            ConfigureShop(content.Find("Shop"), shop, progression, tooltip);
            FarmStockUI stockUI = content.Find("Farm Stock").GetComponent<FarmStockUI>(); SetObject(stockUI, "farmStock", stock);
            UpdateGameGuide(content.Find("Game Guide"));
            popupLayer.SetAsLastSibling();
        }

        private static void ConfigureDashboard(Transform dashboard, BusinessProgression progression, EconomyManager economy, ShopManager shop)
        {
            ClearChildren(dashboard);
            BusinessProgressionDashboard ui = dashboard.GetComponent<BusinessProgressionDashboard>() ?? dashboard.gameObject.AddComponent<BusinessProgressionDashboard>();
            Text heading = CreateText(dashboard, "Progression Heading", 32, FontStyle.Bold, TextAnchor.UpperLeft); SetRect(heading.rectTransform, new Vector2(20f, -14f), new Vector2(960f, 56f)); heading.text = "GROWVELD BUSINESS PROGRESSION";
            Text summary = CreateText(dashboard, "Progression Summary", 25, FontStyle.Normal, TextAnchor.UpperLeft); SetRect(summary.rectTransform, new Vector2(20f, -88f), new Vector2(960f, 430f));
            Slider bar = CreateProgressBar(dashboard);
            SetObject(ui, "progression", progression); SetObject(ui, "economy", economy); SetObject(ui, "shop", shop); SetObject(ui, "summaryText", summary); SetObject(ui, "experienceBar", bar);
        }

        private static Slider CreateProgressBar(Transform parent)
        {
            GameObject root = new("XP Progress", typeof(RectTransform), typeof(Slider)); root.transform.SetParent(parent, false); SetRect(root.GetComponent<RectTransform>(), new Vector2(20f, -540f), new Vector2(940f, 38f));
            GameObject background = new("Background", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)); background.transform.SetParent(root.transform, false); Stretch(background.GetComponent<RectTransform>(), 0f); background.GetComponent<Image>().color = new Color(.07f, .12f, .08f, 1f);
            GameObject fillArea = new("Fill Area", typeof(RectTransform)); fillArea.transform.SetParent(root.transform, false); Stretch(fillArea.GetComponent<RectTransform>(), 4f);
            GameObject fill = new("Fill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)); fill.transform.SetParent(fillArea.transform, false); Stretch(fill.GetComponent<RectTransform>(), 0f); fill.GetComponent<Image>().color = new Color(.25f, .8f, .35f, 1f);
            Slider slider = root.GetComponent<Slider>(); slider.fillRect = fill.GetComponent<RectTransform>(); slider.targetGraphic = fill.GetComponent<Image>(); slider.minValue = 0f; slider.maxValue = 1f; slider.interactable = false; return slider;
        }

        private static TabletTooltipUI ConfigureTooltip(Transform popupLayer)
        {
            Transform old = popupLayer.Find("Shop Tooltip"); if (old != null) UnityEngine.Object.DestroyImmediate(old.gameObject);
            GameObject panel = new("Shop Tooltip", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)); panel.transform.SetParent(popupLayer, false); RectTransform rect = panel.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f); rect.pivot = new Vector2(0f, 1f); rect.sizeDelta = new Vector2(480f, 430f); panel.GetComponent<Image>().color = new Color(.02f, .045f, .028f, .99f);
            Text text = CreateText(panel.transform, "Tooltip Text", 20, FontStyle.Normal, TextAnchor.UpperLeft); Stretch(text.rectTransform, 18f); text.horizontalOverflow = HorizontalWrapMode.Wrap; text.verticalOverflow = VerticalWrapMode.Truncate;
            TabletTooltipUI tooltip = popupLayer.GetComponent<TabletTooltipUI>() ?? popupLayer.gameObject.AddComponent<TabletTooltipUI>(); SetObject(tooltip, "popupLayer", popupLayer); SetObject(tooltip, "tooltipPanel", panel); SetObject(tooltip, "tooltipText", text); panel.SetActive(false); return tooltip;
        }

        private static void ConfigureShop(Transform shopSection, ShopManager shop, BusinessProgression progression, TabletTooltipUI tooltip)
        {
            ClearChildren(shopSection);
            GameObject viewport = new("Shop Viewport", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Mask), typeof(ScrollRect)); viewport.transform.SetParent(shopSection, false); Stretch(viewport.GetComponent<RectTransform>(), 8f); viewport.GetComponent<Image>().color = new Color(.025f, .05f, .032f, .8f); viewport.GetComponent<Mask>().showMaskGraphic = false;
            GameObject rows = new("Shop Rows", typeof(RectTransform)); rows.transform.SetParent(viewport.transform, false); RectTransform rowsRect = rows.GetComponent<RectTransform>(); rowsRect.anchorMin = new Vector2(0f, 1f); rowsRect.anchorMax = new Vector2(1f, 1f); rowsRect.pivot = new Vector2(.5f, 1f);
            ItemDefinition[] visibleItems = shop.AvailableItems.Where(item => item != null).ToArray();
            int rowCount = Mathf.CeilToInt(visibleItems.Length / 2f); rowsRect.sizeDelta = new Vector2(0f, rowCount * 144f + 12f);
            ScrollRect scroll = viewport.GetComponent<ScrollRect>(); scroll.content = rowsRect; scroll.viewport = viewport.GetComponent<RectTransform>(); scroll.horizontal = false; scroll.vertical = true; scroll.scrollSensitivity = 32f;
            for (int index = 0; index < visibleItems.Length; index++)
            {
                ItemDefinition item = visibleItems[index]; int column = index % 2; int rowIndex = index / 2;
                GameObject row = new($"Buy {item.DisplayName}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(ShopItemButton)); row.transform.SetParent(rows.transform, false);
                RectTransform rect = row.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f); rect.pivot = new Vector2(0f, 1f); rect.anchoredPosition = new Vector2(10f + column * 496f, -10f - rowIndex * 144f); rect.sizeDelta = new Vector2(476f, 126f);
                Text label = CreateText(row.transform, "Label", 18, FontStyle.Normal, TextAnchor.MiddleLeft); Stretch(label.rectTransform, 13f); label.horizontalOverflow = HorizontalWrapMode.Wrap;
                ShopItemButton button = row.GetComponent<ShopItemButton>(); SetObject(button, "shop", shop); SetObject(button, "item", item); SetObject(button, "label", label); SetObject(button, "progression", progression); SetObject(button, "tooltip", tooltip);
            }
        }

        private static void UpdateGameGuide(Transform guide)
        {
            Text[] texts = guide.GetComponentsInChildren<Text>(true); Text right = texts.FirstOrDefault(text => text.name == "Guide Copy" && text.transform.parent.name == "Construction and Placement");
            if (right == null) return;
            right.text += "\n\nPROGRESSION\nSell dried crops to earn XP and increase your Business Level. Higher levels unlock new strains, automation, and larger drying/storage equipment. Hover locked Shop items to preview future unlocks.";
            right.fontSize = 17;
        }

        private static void CreatePrimitiveVisual(Transform parent, string name, PrimitiveType type, Vector3 position, Vector3 scale, Color colour)
        {
            GameObject visual = GameObject.CreatePrimitive(type); visual.name = name; visual.transform.SetParent(parent, false); visual.transform.localPosition = position; visual.transform.localScale = scale; UnityEngine.Object.DestroyImmediate(visual.GetComponent<Collider>());
            Material material = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/M_PlaceableEquipment.mat"); if (material != null) visual.GetComponent<Renderer>().sharedMaterial = material;
        }

        private static void CreateCoveragePreview(Transform parent)
        {
            GameObject preview = GameObject.CreatePrimitive(PrimitiveType.Cylinder); preview.name = "Coverage Preview"; preview.transform.SetParent(parent, false); preview.transform.localPosition = new Vector3(0f, .03f, 0f); preview.transform.localScale = new Vector3(1f, .02f, 1f); UnityEngine.Object.DestroyImmediate(preview.GetComponent<Collider>());
            Material material = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/M_LightCoverage.mat"); if (material != null) preview.GetComponent<Renderer>().sharedMaterial = material; preview.SetActive(false);
        }

        private static void ValidateEquipment<T>(string file, float coverage, List<string> failures) where T : AutomationEquipment
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{EquipmentFolder}/{file}"); T component = prefab != null ? prefab.GetComponent<T>() : null;
            if (component == null) failures.Add($"{file} component missing"); else if (!Mathf.Approximately(component.CoverageRadius, coverage)) failures.Add($"{file} coverage incorrect");
        }

        private static Color SeedColour(int index) => index switch { 0 => new Color(.3f, .72f, .35f), 1 => new Color(.28f, .55f, .9f), 2 => new Color(.84f, .72f, .24f), _ => new Color(.55f, .32f, .7f) };
        private static T LoadOrCreate<T>(string path) where T : ScriptableObject { T asset = AssetDatabase.LoadAssetAtPath<T>(path); if (asset != null) return asset; asset = ScriptableObject.CreateInstance<T>(); AssetDatabase.CreateAsset(asset, path); return asset; }
        private static T[] LoadAll<T>(string folder) where T : UnityEngine.Object => AssetDatabase.FindAssets($"t:{typeof(T).Name}", new[] { folder }).Select(guid => AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid))).Where(asset => asset != null).ToArray();
        private static void EnsureFolder(string parent, string name) { string path = $"{parent}/{name}"; if (!AssetDatabase.IsValidFolder(path)) AssetDatabase.CreateFolder(parent, name); }
        private static GameObject FindRoot(Scene scene, string name) => scene.GetRootGameObjects().FirstOrDefault(root => root.name == name);
        private static void ClearChildren(Transform parent) { for (int index = parent.childCount - 1; index >= 0; index--) UnityEngine.Object.DestroyImmediate(parent.GetChild(index).gameObject); }
        private static int GetArraySize(UnityEngine.Object target, string property) => new SerializedObject(target).FindProperty(property)?.arraySize ?? -1;
        private static void SetObject(UnityEngine.Object target, string property, UnityEngine.Object value) { if (target == null) return; SerializedObject data = new(target); Set(data, property, value); data.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(target); }
        private static void SetObjectArray<T>(UnityEngine.Object target, string property, IReadOnlyList<T> values) where T : UnityEngine.Object { if (target == null) return; SerializedObject data = new(target); SetObjectArray(data, property, values); data.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(target); }
        private static void SetObjectArray<T>(SerializedObject data, string property, IReadOnlyList<T> values) where T : UnityEngine.Object { SerializedProperty array = data.FindProperty(property); array.arraySize = values.Count; for (int index = 0; index < values.Count; index++) array.GetArrayElementAtIndex(index).objectReferenceValue = values[index]; }
        private static void Set(SerializedObject data, string property, object value) { SerializedProperty field = data.FindProperty(property); if (field == null) throw new MissingFieldException(data.targetObject.GetType().Name, property); switch (value) { case null: field.objectReferenceValue = null; break; case string text: field.stringValue = text; break; case int number: field.intValue = number; break; case float number: field.floatValue = number; break; case bool flag: field.boolValue = flag; break; case Color colour: field.colorValue = colour; break; case Vector3 vector: field.vector3Value = vector; break; case UnityEngine.Object reference: field.objectReferenceValue = reference; break; default: throw new ArgumentException($"Unsupported serialized value {value.GetType().Name}"); } }
        private static Text CreateText(Transform parent, string name, int size, FontStyle style, TextAnchor alignment) { GameObject go = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text)); go.transform.SetParent(parent, false); Text text = go.GetComponent<Text>(); text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); text.fontSize = size; text.fontStyle = style; text.alignment = alignment; text.color = Color.white; return text; }
        private static void SetRect(RectTransform rect, Vector2 position, Vector2 size) { rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f); rect.pivot = new Vector2(0f, 1f); rect.anchoredPosition = position; rect.sizeDelta = size; }
        private static void Stretch(RectTransform rect, float padding) { rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = Vector2.one * padding; rect.offsetMax = Vector2.one * -padding; }
    }
}
