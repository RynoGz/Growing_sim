using System;
using System.Collections.Generic;
using Growveld.Building;
using Growveld.Economy;
using Growveld.Environment;
using Growveld.Interaction;
using Growveld.Inventory;
using Growveld.Player;
using Growveld.Saving;
using Growveld.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Growveld.Editor
{
    public static class Phase25UxGameplaySetup
    {
        private const string ScenePath = "Assets/_Project/Scenes/PrototypeFarm.unity";
        private const string ExistingLightPrefabPath = "Assets/_Project/Prefabs/Equipment/Grow Light.prefab";
        private const string ExistingLightDefinitionPath = "Assets/_Project/ScriptableObjects/Placeables/Placeable_grow_light.asset";
        private const string ExistingLightItemPath = "Assets/_Project/ScriptableObjects/Items/Item_grow_light.asset";
        private const string CeilingLightPrefabPath = "Assets/_Project/Prefabs/Equipment/Ceiling Grow Light.prefab";
        private const string CeilingLightDefinitionPath = "Assets/_Project/ScriptableObjects/Placeables/Placeable_ceiling_grow_light.asset";
        private const string CeilingLightItemPath = "Assets/_Project/ScriptableObjects/Items/Item_ceiling_grow_light.asset";
        private const string EquipmentMaterialPath = "Assets/_Project/Materials/M_PlaceableEquipment.mat";
        private const string CoverageMaterialPath = "Assets/_Project/Materials/M_LightCoverage.mat";

        private static readonly Color GrowLightColor = new(0.72f, 0.82f, 1f, 1f);

        [MenuItem("Growveld/Phase 25/Apply UX and Gameplay Fixes")]
        public static void ConfigureUxGameplayFixes()
        {
            AssetDatabase.Refresh();
            ConfigureExistingGrowLight();
            (ItemDefinition ceilingItem, PlaceableDefinition ceilingDefinition) = ConfigureCeilingGrowLight();

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject player = FindRoot(scene, "Player");
            GameObject systems = FindRoot(scene, "Game Systems");
            if (player == null || systems == null) throw new MissingReferenceException("Player and Game Systems are required.");

            PlayerInventory inventory = player.GetComponent<PlayerInventory>();
            PlacementController placement = player.GetComponent<PlacementController>();
            PlayerInteractor interactor = player.GetComponent<PlayerInteractor>();
            BusinessTabletController tablet = player.GetComponent<BusinessTabletController>();
            ConstructionModeController construction = player.GetComponent<ConstructionModeController>()
                ?? player.AddComponent<ConstructionModeController>();

            SerializedObject inventorySettings = new(inventory);
            inventorySettings.FindProperty("capacity").intValue = 24;
            inventorySettings.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject constructionSettings = new(construction);
            constructionSettings.FindProperty("placementController").objectReferenceValue = placement;
            constructionSettings.FindProperty("playerInteractor").objectReferenceValue = interactor;
            constructionSettings.ApplyModifiedPropertiesWithoutUndo();

            TabletInventoryUI inventoryUI = ConfigureTabletInventory(scene, player, inventory, construction, tablet);
            ConfigurePlayerInputSuspension(player, tablet, inventoryUI, construction);
            ConfigureConstructionHud(construction);
            ConfigureShop(systems.GetComponent<ShopManager>(), ceilingItem, scene);
            ConfigureSaveCatalog(systems.GetComponent<SaveSystem>());
            ConfigureWorldTextOcclusion(scene);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            ValidateConfiguredProject(ceilingItem, ceilingDefinition);
            Debug.Log("Growveld Phase 25 setup complete: occluded building labels, tablet inventory placement, construction mode, repaired grow lights, ceiling light, and carry-on-harvest are configured.");
        }

        [MenuItem("Growveld/Phase 25/Validate UX and Gameplay Fixes")]
        public static void ValidateUxGameplayFixes()
        {
            ValidateConfiguredProject(
                AssetDatabase.LoadAssetAtPath<ItemDefinition>(CeilingLightItemPath),
                AssetDatabase.LoadAssetAtPath<PlaceableDefinition>(CeilingLightDefinitionPath));
        }

        private static void ConfigureExistingGrowLight()
        {
            GameObject prefab = PrefabUtility.LoadPrefabContents(ExistingLightPrefabPath);
            try
            {
                GrowLight growLight = prefab.GetComponent<GrowLight>() ?? prefab.AddComponent<GrowLight>();
                Light source = prefab.GetComponentInChildren<Light>(true);
                if (source == null)
                {
                    GameObject sourceObject = new("Plant Light Source", typeof(Light));
                    sourceObject.transform.SetParent(prefab.transform, false);
                    sourceObject.transform.localPosition = new Vector3(0f, 2.35f, 0f);
                    source = sourceObject.GetComponent<Light>();
                }

                source.gameObject.SetActive(true);
                source.enabled = true;
                source.type = LightType.Point;
                source.color = GrowLightColor;
                source.intensity = 300f;
                source.range = 8f;
                source.useColorTemperature = false;
                source.shadows = LightShadows.None;

                Transform coverage = prefab.transform.Find("Coverage Preview");
                if (coverage != null)
                {
                    coverage.localScale = new Vector3(12f, 0.025f, 12f);
                    coverage.gameObject.SetActive(false);
                }

                ConfigureGrowLightComponent(growLight, source, LightType.Point);
                PrefabUtility.SaveAsPrefabAsset(prefab, ExistingLightPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefab);
            }

            PlaceableDefinition definition = AssetDatabase.LoadAssetAtPath<PlaceableDefinition>(ExistingLightDefinitionPath);
            SerializedObject definitionSettings = new(definition);
            definitionSettings.FindProperty("lightCoverageRadius").floatValue = 6f;
            definitionSettings.FindProperty("placementSurface").enumValueIndex = (int)PlacementSurface.Floor;
            definitionSettings.ApplyModifiedPropertiesWithoutUndo();

            ItemDefinition item = AssetDatabase.LoadAssetAtPath<ItemDefinition>(ExistingLightItemPath);
            SerializedObject itemSettings = new(item);
            itemSettings.FindProperty("description").stringValue = "Floor grow light with six-metre plant coverage and automatic scheduling.";
            itemSettings.FindProperty("displayColor").colorValue = GrowLightColor;
            itemSettings.ApplyModifiedPropertiesWithoutUndo();
        }

        private static (ItemDefinition, PlaceableDefinition) ConfigureCeilingGrowLight()
        {
            ItemDefinition item = AssetDatabase.LoadAssetAtPath<ItemDefinition>(CeilingLightItemPath);
            if (item == null)
            {
                item = ScriptableObject.CreateInstance<ItemDefinition>();
                AssetDatabase.CreateAsset(item, CeilingLightItemPath);
            }

            PlaceableDefinition definition = AssetDatabase.LoadAssetAtPath<PlaceableDefinition>(CeilingLightDefinitionPath);
            if (definition == null)
            {
                definition = ScriptableObject.CreateInstance<PlaceableDefinition>();
                AssetDatabase.CreateAsset(definition, CeilingLightDefinitionPath);
            }

            GameObject prefab = CreateCeilingLightPrefab(definition);

            SerializedObject definitionSettings = new(definition);
            definitionSettings.FindProperty("placeableId").stringValue = "ceiling_grow_light";
            definitionSettings.FindProperty("itemDefinition").objectReferenceValue = item;
            definitionSettings.FindProperty("prefab").objectReferenceValue = prefab;
            definitionSettings.FindProperty("footprintSize").vector3Value = new Vector3(1.7f, 0.32f, 0.65f);
            definitionSettings.FindProperty("placementOffset").vector3Value = new Vector3(0f, -0.02f, 0f);
            definitionSettings.FindProperty("placementSurface").enumValueIndex = (int)PlacementSurface.Ceiling;
            definitionSettings.FindProperty("rotationStep").floatValue = 15f;
            definitionSettings.FindProperty("sellRefundFraction").floatValue = 0.7f;
            definitionSettings.FindProperty("lightCoverageRadius").floatValue = 6f;
            definitionSettings.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject itemSettings = new(item);
            itemSettings.FindProperty("itemId").stringValue = "ceiling_grow_light";
            itemSettings.FindProperty("displayName").stringValue = "Ceiling Grow Light";
            itemSettings.FindProperty("description").stringValue = "Ceiling-mounted grow light with six-metre plant coverage.";
            itemSettings.FindProperty("category").enumValueIndex = (int)ItemCategory.Equipment;
            itemSettings.FindProperty("stackable").boolValue = true;
            itemSettings.FindProperty("maximumStack").intValue = 20;
            itemSettings.FindProperty("purchasePrice").floatValue = 3600f;
            itemSettings.FindProperty("displayColor").colorValue = GrowLightColor;
            itemSettings.FindProperty("placeableDefinition").objectReferenceValue = definition;
            itemSettings.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(item);
            EditorUtility.SetDirty(definition);
            return (item, definition);
        }

        private static GameObject CreateCeilingLightPrefab(PlaceableDefinition definition)
        {
            GameObject root = new("Ceiling Grow Light", typeof(PlacedObject), typeof(GrowLight));
            Material equipmentMaterial = AssetDatabase.LoadAssetAtPath<Material>(EquipmentMaterialPath);
            Material coverageMaterial = AssetDatabase.LoadAssetAtPath<Material>(CoverageMaterialPath);

            CreatePrimitivePart(root.transform, "Fixture", PrimitiveType.Cube, new Vector3(0f, -0.14f, 0f), new Vector3(1.7f, 0.22f, 0.65f), equipmentMaterial, true);
            CreatePrimitivePart(root.transform, "Lamp Panel", PrimitiveType.Cube, new Vector3(0f, -0.27f, 0f), new Vector3(1.48f, 0.055f, 0.48f), equipmentMaterial, false);

            GameObject sourceObject = new("Plant Light Source", typeof(Light));
            sourceObject.transform.SetParent(root.transform, false);
            sourceObject.transform.localPosition = new Vector3(0f, -0.3f, 0f);
            sourceObject.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            Light source = sourceObject.GetComponent<Light>();
            source.type = LightType.Spot;
            source.spotAngle = 110f;
            source.innerSpotAngle = 75f;
            source.color = GrowLightColor;
            source.intensity = 300f;
            source.range = 8f;
            source.useColorTemperature = false;
            source.shadows = LightShadows.None;

            GameObject coverage = CreatePrimitivePart(root.transform, "Coverage Preview", PrimitiveType.Cylinder, new Vector3(0f, -3.15f, 0f), new Vector3(12f, 0.025f, 12f), coverageMaterial, false);
            coverage.SetActive(false);

            SerializedObject placedSettings = new(root.GetComponent<PlacedObject>());
            placedSettings.FindProperty("definition").objectReferenceValue = definition;
            placedSettings.ApplyModifiedPropertiesWithoutUndo();
            ConfigureGrowLightComponent(root.GetComponent<GrowLight>(), source, LightType.Spot);

            GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(root, CeilingLightPrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
            return savedPrefab;
        }

        private static void ConfigureGrowLightComponent(GrowLight growLight, Light source, LightType type)
        {
            SerializedObject settings = new(growLight);
            settings.FindProperty("coverageRadius").floatValue = 6f;
            settings.FindProperty("powerConsumptionKilowatts").floatValue = 1.2f;
            settings.FindProperty("lightSource").objectReferenceValue = source;
            settings.FindProperty("visualIntensity").floatValue = 300f;
            settings.FindProperty("visualRange").floatValue = 8f;
            settings.FindProperty("visualColor").colorValue = GrowLightColor;
            settings.FindProperty("visualLightType").enumValueIndex = (int)type;
            settings.FindProperty("spotAngle").floatValue = 110f;
            settings.FindProperty("automaticSchedule").boolValue = true;
            settings.FindProperty("fallbackCycleRealSeconds").floatValue = 1800f;
            settings.FindProperty("fallbackActiveRealSeconds").floatValue = 1200f;
            settings.ApplyModifiedPropertiesWithoutUndo();
        }

        private static GameObject CreatePrimitivePart(Transform parent, string name, PrimitiveType type, Vector3 localPosition, Vector3 localScale, Material material, bool keepCollider)
        {
            GameObject part = GameObject.CreatePrimitive(type);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.transform.localScale = localScale;
            if (material != null) part.GetComponent<Renderer>().sharedMaterial = material;
            if (!keepCollider)
            {
                Collider collider = part.GetComponent<Collider>();
                if (collider != null) UnityEngine.Object.DestroyImmediate(collider);
            }
            return part;
        }

        private static TabletInventoryUI ConfigureTabletInventory(Scene scene, GameObject player, PlayerInventory inventory, ConstructionModeController construction, BusinessTabletController tabletController)
        {
            GameObject tabletCanvas = FindRoot(scene, "Business Tablet UI");
            Transform tablet = tabletCanvas != null ? tabletCanvas.transform.Find("Tablet") : null;
            Transform content = tablet != null ? tablet.Find("Content") : null;
            BusinessTabletUI tabletUI = tablet != null ? tablet.GetComponent<BusinessTabletUI>() : null;
            if (tablet == null || content == null || tabletUI == null) throw new MissingReferenceException("Business tablet hierarchy is incomplete.");

            Transform oldSection = content.Find("Inventory");
            if (oldSection != null) UnityEngine.Object.DestroyImmediate(oldSection.gameObject);
            Transform oldTab = tablet.Find("Inventory Tab");
            if (oldTab != null) UnityEngine.Object.DestroyImmediate(oldTab.gameObject);

            GameObject section = new("Inventory", typeof(RectTransform), typeof(TabletInventoryUI));
            section.transform.SetParent(content, false);
            RectTransform sectionRect = section.GetComponent<RectTransform>();
            Stretch(sectionRect, 0f);

            Text instructions = CreateText(section.transform, "Instructions", 21, TextAnchor.UpperLeft);
            RectTransform instructionsRect = instructions.rectTransform;
            instructionsRect.anchorMin = new Vector2(0f, 1f);
            instructionsRect.anchorMax = new Vector2(1f, 1f);
            instructionsRect.pivot = new Vector2(0.5f, 1f);
            instructionsRect.anchoredPosition = new Vector2(0f, -4f);
            instructionsRect.sizeDelta = new Vector2(0f, 54f);
            instructions.text = "Owned items  •  Right-click placeable equipment and choose Place";

            GameObject scrollObject = new("Inventory Scroll", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(ScrollRect));
            scrollObject.transform.SetParent(section.transform, false);
            RectTransform scrollRectTransform = scrollObject.GetComponent<RectTransform>();
            scrollRectTransform.anchorMin = new Vector2(0f, 0f);
            scrollRectTransform.anchorMax = new Vector2(1f, 1f);
            scrollRectTransform.offsetMin = new Vector2(0f, 0f);
            scrollRectTransform.offsetMax = new Vector2(0f, -62f);
            scrollObject.GetComponent<Image>().color = new Color(0.035f, 0.065f, 0.043f, 0.9f);

            GameObject viewport = new("Viewport", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(RectMask2D));
            viewport.transform.SetParent(scrollObject.transform, false);
            Stretch(viewport.GetComponent<RectTransform>(), 8f);
            viewport.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.01f);

            GameObject rows = new("Rows", typeof(RectTransform));
            rows.transform.SetParent(viewport.transform, false);
            RectTransform rowsRect = rows.GetComponent<RectTransform>();
            rowsRect.anchorMin = new Vector2(0f, 1f);
            rowsRect.anchorMax = new Vector2(1f, 1f);
            rowsRect.pivot = new Vector2(0.5f, 1f);
            rowsRect.anchoredPosition = Vector2.zero;
            rowsRect.sizeDelta = new Vector2(0f, 590f);

            ScrollRect scroll = scrollObject.GetComponent<ScrollRect>();
            scroll.viewport = viewport.GetComponent<RectTransform>();
            scroll.content = rowsRect;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 34f;

            Text emptyLabel = CreateText(viewport.transform, "Empty Label", 24, TextAnchor.MiddleCenter);
            Stretch(emptyLabel.rectTransform, 24f);
            emptyLabel.text = "No owned items yet.\nOrders appear here after delivery.";

            GameObject contextMenu = new("Item Context Menu", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            contextMenu.transform.SetParent(section.transform, false);
            RectTransform menuRect = contextMenu.GetComponent<RectTransform>();
            menuRect.anchorMin = new Vector2(0f, 0f);
            menuRect.anchorMax = new Vector2(0f, 0f);
            menuRect.pivot = new Vector2(0f, 1f);
            menuRect.sizeDelta = new Vector2(280f, 132f);
            contextMenu.GetComponent<Image>().color = new Color(0.055f, 0.105f, 0.07f, 0.99f);

            Text contextTitle = CreateText(contextMenu.transform, "Title", 20, TextAnchor.MiddleLeft);
            RectTransform titleRect = contextTitle.rectTransform;
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.anchoredPosition = Vector2.zero;
            titleRect.sizeDelta = new Vector2(0f, 52f);
            titleRect.offsetMin = new Vector2(12f, titleRect.offsetMin.y);

            Button placeButton = CreateButton(contextMenu.transform, "Place", new Color(0.16f, 0.38f, 0.22f, 1f));
            RectTransform placeRect = placeButton.GetComponent<RectTransform>();
            placeRect.anchorMin = new Vector2(0f, 0f);
            placeRect.anchorMax = new Vector2(1f, 0f);
            placeRect.pivot = new Vector2(0.5f, 0f);
            placeRect.anchoredPosition = new Vector2(0f, 10f);
            placeRect.sizeDelta = new Vector2(-20f, 58f);
            contextMenu.SetActive(false);

            TabletInventoryUI inventoryUI = section.GetComponent<TabletInventoryUI>();
            SerializedObject inventoryUiSettings = new(inventoryUI);
            inventoryUiSettings.FindProperty("inventory").objectReferenceValue = inventory;
            inventoryUiSettings.FindProperty("constructionMode").objectReferenceValue = construction;
            inventoryUiSettings.FindProperty("tabletController").objectReferenceValue = tabletController;
            inventoryUiSettings.FindProperty("rowsRoot").objectReferenceValue = rowsRect;
            inventoryUiSettings.FindProperty("emptyLabel").objectReferenceValue = emptyLabel;
            inventoryUiSettings.FindProperty("contextMenu").objectReferenceValue = contextMenu;
            inventoryUiSettings.FindProperty("contextTitle").objectReferenceValue = contextTitle;
            inventoryUiSettings.FindProperty("placeButton").objectReferenceValue = placeButton;
            inventoryUiSettings.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject tabletUiSettings = new(tabletUI);
            SerializedProperty sections = tabletUiSettings.FindProperty("sections");
            SerializedProperty names = tabletUiSettings.FindProperty("sectionNames");
            List<GameObject> existingSections = new();
            List<string> existingNames = new();
            for (int index = 0; index < sections.arraySize; index++)
            {
                GameObject existing = sections.GetArrayElementAtIndex(index).objectReferenceValue as GameObject;
                if (existing == null || existing.name == "Inventory") continue;
                existingSections.Add(existing);
                existingNames.Add(index < names.arraySize ? names.GetArrayElementAtIndex(index).stringValue : existing.name);
            }
            existingSections.Add(section);
            existingNames.Add("Inventory");
            sections.arraySize = existingSections.Count;
            names.arraySize = existingNames.Count;
            for (int index = 0; index < existingSections.Count; index++)
            {
                sections.GetArrayElementAtIndex(index).objectReferenceValue = existingSections[index];
                names.GetArrayElementAtIndex(index).stringValue = existingNames[index];
            }
            tabletUiSettings.ApplyModifiedPropertiesWithoutUndo();

            CreateTabletTab(tablet, tabletUI, existingSections.Count - 1, "Inventory");
            ReflowTabletTabs(tablet);
            section.SetActive(false);

            Transform constructionCopy = content.Find("Construction/Section Copy");
            if (constructionCopy != null && constructionCopy.TryGetComponent(out Text constructionText))
            {
                constructionText.text = "Press B to enter Construction Mode. Aim at placed equipment and press E to move it, or Delete to sell it.\n\nNew equipment is placed from Tablet > Inventory by right-clicking the item and choosing Place.";
            }

            return inventoryUI;
        }

        private static void ConfigurePlayerInputSuspension(GameObject player, BusinessTabletController tablet, TabletInventoryUI inventoryUI, ConstructionModeController construction)
        {
            Behaviour[] tabletSuspended =
            {
                player.GetComponent<FirstPersonController>(),
                player.GetComponent<PlayerInteractor>(),
                player.GetComponent<InventoryHotbarInput>(),
                player.GetComponent<PlacementController>(),
                construction
            };
            SerializedObject tabletSettings = new(tablet);
            SetBehaviourArray(tabletSettings.FindProperty("gameplayBehaviours"), tabletSuspended);
            tabletSettings.FindProperty("placementController").objectReferenceValue = player.GetComponent<PlacementController>();
            tabletSettings.FindProperty("constructionMode").objectReferenceValue = construction;
            tabletSettings.FindProperty("inventoryUI").objectReferenceValue = inventoryUI;
            tabletSettings.ApplyModifiedPropertiesWithoutUndo();

            PauseAndHelpController pause = player.GetComponent<PauseAndHelpController>();
            if (pause == null) return;
            Behaviour[] pauseSuspended =
            {
                player.GetComponent<FirstPersonController>(),
                player.GetComponent<PlayerInteractor>(),
                player.GetComponent<InventoryHotbarInput>(),
                player.GetComponent<PlacementController>(),
                tablet,
                construction
            };
            SerializedObject pauseSettings = new(pause);
            SetBehaviourArray(pauseSettings.FindProperty("gameplayBehaviours"), pauseSuspended);
            pauseSettings.FindProperty("constructionMode").objectReferenceValue = construction;
            pauseSettings.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureConstructionHud(ConstructionModeController construction)
        {
            PlacementHUD[] huds = UnityEngine.Object.FindObjectsByType<PlacementHUD>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (PlacementHUD hud in huds)
            {
                SerializedObject settings = new(hud);
                settings.FindProperty("constructionMode").objectReferenceValue = construction;
                settings.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void ConfigureShop(ShopManager shop, ItemDefinition ceilingItem, Scene scene)
        {
            if (shop == null) throw new MissingReferenceException("ShopManager is required.");
            List<ItemDefinition> items = new(shop.AvailableItems ?? Array.Empty<ItemDefinition>());
            if (!items.Contains(ceilingItem)) items.Add(ceilingItem);
            SerializedObject shopSettings = new(shop);
            SerializedProperty array = shopSettings.FindProperty("availableItems");
            array.arraySize = items.Count;
            for (int index = 0; index < items.Count; index++) array.GetArrayElementAtIndex(index).objectReferenceValue = items[index];
            shopSettings.ApplyModifiedPropertiesWithoutUndo();

            GameObject tabletCanvas = FindRoot(scene, "Business Tablet UI");
            Transform shopSection = tabletCanvas != null ? tabletCanvas.transform.Find("Tablet/Content/Shop") : null;
            if (shopSection == null) return;
            Transform oldRow = shopSection.Find("Buy Ceiling Grow Light");
            if (oldRow != null) UnityEngine.Object.DestroyImmediate(oldRow.gameObject);
            int indexForLayout = shopSection.GetComponentsInChildren<ShopItemButton>(true).Length;

            GameObject row = new("Buy Ceiling Grow Light", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(ShopItemButton));
            row.transform.SetParent(shopSection, false);
            RectTransform rect = row.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            int column = indexForLayout / 4;
            int rowIndex = indexForLayout % 4;
            rect.anchoredPosition = new Vector2(12f + column * 495f, -12f - rowIndex * 158f);
            rect.sizeDelta = new Vector2(465f, 132f);
            row.GetComponent<Image>().color = new Color(0.1f, 0.18f, 0.12f, 1f);

            Text label = CreateText(row.transform, "Label", 20, TextAnchor.MiddleLeft);
            Stretch(label.rectTransform, 14f);
            SerializedObject rowSettings = new(row.GetComponent<ShopItemButton>());
            rowSettings.FindProperty("shop").objectReferenceValue = shop;
            rowSettings.FindProperty("item").objectReferenceValue = ceilingItem;
            rowSettings.FindProperty("label").objectReferenceValue = label;
            rowSettings.ApplyModifiedPropertiesWithoutUndo();

            ShopItemButton[] shopRows = shopSection.GetComponentsInChildren<ShopItemButton>(true);
            Array.Sort(shopRows, (left, right) => left.transform.GetSiblingIndex().CompareTo(right.transform.GetSiblingIndex()));
            for (int layoutIndex = 0; layoutIndex < shopRows.Length; layoutIndex++)
            {
                RectTransform rowRect = shopRows[layoutIndex].GetComponent<RectTransform>();
                int layoutColumn = layoutIndex % 3;
                int layoutRow = layoutIndex / 3;
                rowRect.anchoredPosition = new Vector2(12f + layoutColumn * 330f, -12f - layoutRow * 158f);
                rowRect.sizeDelta = new Vector2(315f, 132f);
            }
        }

        private static void ConfigureSaveCatalog(SaveSystem saveSystem)
        {
            if (saveSystem == null) throw new MissingReferenceException("SaveSystem is required.");
            ItemDefinition[] items = LoadAssets<ItemDefinition>("Assets/_Project/ScriptableObjects/Items");
            PlaceableDefinition[] placeables = LoadAssets<PlaceableDefinition>("Assets/_Project/ScriptableObjects/Placeables");
            SerializedObject settings = new(saveSystem);
            SetObjectArray(settings.FindProperty("itemCatalog"), items);
            SetObjectArray(settings.FindProperty("placeableCatalog"), placeables);
            settings.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureWorldTextOcclusion(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (TextMesh textMesh in root.GetComponentsInChildren<TextMesh>(true))
                {
                    WorldTextOcclusion occlusion = textMesh.GetComponent<WorldTextOcclusion>()
                        ?? textMesh.gameObject.AddComponent<WorldTextOcclusion>();
                    SerializedObject settings = new(occlusion);
                    settings.FindProperty("depthTestedShader").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Shader>("Assets/_Project/Shaders/WorldTextOccluded.shader");
                    settings.ApplyModifiedPropertiesWithoutUndo();
                }
            }
        }

        private static void ValidateConfiguredProject(ItemDefinition ceilingItem, PlaceableDefinition ceilingDefinition)
        {
            List<string> failures = new();
            GameObject existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ExistingLightPrefabPath);
            GrowLight existingLight = existingPrefab != null ? existingPrefab.GetComponent<GrowLight>() : null;
            if (existingLight == null || existingLight.LightSource == null) failures.Add("existing grow-light source reference missing");
            else
            {
                if (!Mathf.Approximately(existingLight.CoverageRadius, 6f)) failures.Add("existing grow-light coverage is not 6");
                if (!Mathf.Approximately(existingLight.VisualIntensity, 300f)) failures.Add("existing grow-light intensity is not 300");
            }
            if (ceilingItem == null || ceilingDefinition == null || ceilingDefinition.Prefab == null) failures.Add("ceiling grow-light assets missing");
            else if (ceilingDefinition.PlacementSurface != PlacementSurface.Ceiling) failures.Add("ceiling grow light is not ceiling-only");

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject player = FindRoot(scene, "Player");
            GameObject systems = FindRoot(scene, "Game Systems");
            if (player == null || player.GetComponent<ConstructionModeController>() == null) failures.Add("construction mode controller missing");
            if (UnityEngine.Object.FindFirstObjectByType<TabletInventoryUI>(FindObjectsInactive.Include) == null) failures.Add("tablet inventory page missing");
            if (UnityEngine.Object.FindObjectsByType<WorldTextOcclusion>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length < 2) failures.Add("world labels are not occlusion-controlled");
            SaveSystem save = systems != null ? systems.GetComponent<SaveSystem>() : null;
            if (save == null) failures.Add("save system missing");

            if (failures.Count > 0) throw new InvalidOperationException("Phase 25 validation failed: " + string.Join(", ", failures));
            Debug.Log("Growveld Phase 25 validation passed: inventory UI, construction mode, grow lights, ceiling placement, world-label occlusion, and save catalogues are configured.");
        }

        private static void CreateTabletTab(Transform parent, BusinessTabletUI tabletUI, int index, string labelText)
        {
            GameObject buttonObject = new($"{labelText} Tab", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(TabletTabButton));
            buttonObject.transform.SetParent(parent, false);
            buttonObject.GetComponent<Image>().color = new Color(0.12f, 0.24f, 0.15f, 1f);
            Text label = CreateText(buttonObject.transform, "Label", 20, TextAnchor.MiddleCenter);
            Stretch(label.rectTransform, 5f);
            label.text = labelText;
            SerializedObject settings = new(buttonObject.GetComponent<TabletTabButton>());
            settings.FindProperty("tabletUI").objectReferenceValue = tabletUI;
            settings.FindProperty("sectionIndex").intValue = index;
            settings.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ReflowTabletTabs(Transform tablet)
        {
            TabletTabButton[] tabs = tablet.GetComponentsInChildren<TabletTabButton>(true);
            Array.Sort(tabs, (left, right) =>
            {
                SerializedObject leftSettings = new(left);
                SerializedObject rightSettings = new(right);
                return leftSettings.FindProperty("sectionIndex").intValue.CompareTo(rightSettings.FindProperty("sectionIndex").intValue);
            });
            for (int index = 0; index < tabs.Length; index++)
            {
                RectTransform rect = tabs[index].GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0f, 1f);
                rect.anchoredPosition = new Vector2(28f, -28f - index * 82f);
                rect.sizeDelta = new Vector2(205f, 60f);
            }
        }

        private static Button CreateButton(Transform parent, string labelText, Color color)
        {
            GameObject buttonObject = new(labelText, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            buttonObject.GetComponent<Image>().color = color;
            Text label = CreateText(buttonObject.transform, "Label", 21, TextAnchor.MiddleCenter);
            Stretch(label.rectTransform, 5f);
            label.text = labelText;
            return buttonObject.GetComponent<Button>();
        }

        private static Text CreateText(Transform parent, string name, int fontSize, TextAnchor alignment)
        {
            GameObject textObject = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            textObject.transform.SetParent(parent, false);
            Text text = textObject.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = Color.white;
            return text;
        }

        private static void Stretch(RectTransform rect, float padding)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.one * padding;
            rect.offsetMax = Vector2.one * -padding;
        }

        private static void SetBehaviourArray(SerializedProperty property, Behaviour[] values)
        {
            property.arraySize = values.Length;
            for (int index = 0; index < values.Length; index++) property.GetArrayElementAtIndex(index).objectReferenceValue = values[index];
        }

        private static void SetObjectArray<T>(SerializedProperty property, T[] values) where T : UnityEngine.Object
        {
            property.arraySize = values.Length;
            for (int index = 0; index < values.Length; index++) property.GetArrayElementAtIndex(index).objectReferenceValue = values[index];
        }

        private static T[] LoadAssets<T>(string folder) where T : UnityEngine.Object
        {
            string[] guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}", new[] { folder });
            List<T> assets = new();
            foreach (string guid in guids)
            {
                T asset = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
                if (asset != null) assets.Add(asset);
            }
            assets.Sort((left, right) => string.CompareOrdinal(left.name, right.name));
            return assets.ToArray();
        }

        private static GameObject FindRoot(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects()) if (root.name == name) return root;
            return null;
        }
    }
}
