using System;
using System.Collections.Generic;
using System.Linq;
using Growveld.Building;
using Growveld.Carrying;
using Growveld.Core;
using Growveld.Economy;
using Growveld.Farming;
using Growveld.Interaction;
using Growveld.Inventory;
using Growveld.Player;
using Growveld.Saving;
using Growveld.UI;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Growveld.Editor
{
    public static class Phase24PolishSetup
    {
        private const string ScenePath = "Assets/_Project/Scenes/PrototypeFarm.unity";
        private const string MaterialFolder = "Assets/_Project/Materials/Polish";
        private static double tabletSmokeReadyAt;

        private static readonly string[] TestRootNames =
        {
            "Interaction Test Crate",
            "Carryable Test Crate A",
            "Carryable Test Crate B",
            "Seed Pickup",
            "Nutrient Pickup",
            "Quick Purchase Kiosk",
            "Plant Growth Test Pot",
            "Indoor Environment Test Room",
            "Indoor Test Grow Light",
            "Harvest Test Pot",
            "Drying Test Rack",
            "Fresh Batch Test",
            "Storage Test Bin",
            "Dried Batch Test"
        };

        [MenuItem("Growveld/Phase 24/Apply Final Prototype Polish")]
        public static void ConfigurePolish()
        {
            EnsureFolder(MaterialFolder);
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject player = FindRoot(scene, "Player");
            GameObject systems = FindRoot(scene, "Game Systems");
            if (player == null || systems == null) throw new MissingReferenceException("Player and Game Systems are required.");

            foreach (string testRootName in TestRootNames) RemoveRoot(scene, testRootName);
            RemoveRoot(scene, "Polished Environment");
            RemoveRoot(scene, "Polish HUD");
            RemoveRoot(scene, "Pause UI");

            ResetCleanStartingState(scene, player, systems);
            EnsureEventSystem(scene);
            CreatePolishedEnvironment(scene);
            ConfigurePolishedHUD(scene, player, systems);
            ConfigureFeedbackAudio(player, systems);
            PolishExistingHUD(scene);

            PlayerSettings.companyName = "RynoGz";
            PlayerSettings.productName = "Growveld";
            PlayerSettings.bundleVersion = "0.1.0-prototype";
            Application.targetFrameRate = 60;

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            ValidatePrototype(scene);
            Selection.activeGameObject = player;
            Debug.Log("Growveld Phase 24 setup complete: test fixtures removed, clean start restored, farm scenery and HUD polished, pause/help/carry feedback added, generated audio cues enabled, product identity finalised, and prototype validation passed.");
        }

        [MenuItem("Growveld/Phase 24/Validate Complete Prototype")]
        public static void ValidateOpenPrototype()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            ValidatePrototype(scene);
        }

        [MenuItem("Growveld/Phase 24/Repair Tablet Input")]
        public static void RepairTabletInput()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            EnsureEventSystem(scene);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            ValidatePrototype(scene);
            Debug.Log("Growveld tablet input repaired: EventSystem and Input System UI module are present and validated.");
        }

        [MenuItem("Growveld/Phase 24/Run Tablet Click Smoke Test %#F6")]
        public static void RunTabletClickSmokeTest()
        {
            if (!EditorApplication.isPlaying)
            {
                Debug.LogWarning("Enter Play Mode before running the tablet click smoke test.");
                return;
            }

            BusinessTabletController controller = UnityEngine.Object.FindFirstObjectByType<BusinessTabletController>();
            if (controller == null)
            {
                throw new MissingReferenceException("Tablet click smoke-test references are incomplete.");
            }

            controller.SetOpen(true);
            tabletSmokeReadyAt = EditorApplication.timeSinceStartup + 0.5d;
            EditorApplication.update -= CompleteTabletClickSmokeTest;
            EditorApplication.update += CompleteTabletClickSmokeTest;
        }

        private static void CompleteTabletClickSmokeTest()
        {
            if (!EditorApplication.isPlaying)
            {
                EditorApplication.update -= CompleteTabletClickSmokeTest;
                return;
            }

            if (EditorApplication.timeSinceStartup < tabletSmokeReadyAt) return;
            EditorApplication.update -= CompleteTabletClickSmokeTest;

            EventSystem eventSystem = UnityEngine.Object.FindFirstObjectByType<EventSystem>();
            InputSystemUIInputModule inputModule = eventSystem != null ? eventSystem.GetComponent<InputSystemUIInputModule>() : null;
            BusinessTabletController controller = UnityEngine.Object.FindFirstObjectByType<BusinessTabletController>();
            GameObject tabletCanvas = GameObject.Find("Business Tablet UI");
            if (eventSystem == null || inputModule == null || controller == null || tabletCanvas == null)
            {
                throw new MissingReferenceException("Tablet click smoke-test references are incomplete.");
            }

            Transform shopTabTransform = tabletCanvas.transform.Find("Tablet/Shop Tab");
            Transform shopSectionTransform = tabletCanvas.transform.Find("Tablet/Content/Shop");
            if (shopTabTransform == null || shopSectionTransform == null) throw new MissingReferenceException("Shop tablet UI is incomplete.");
            GameObject shopTab = shopTabTransform.gameObject;
            GameObject shopSection = shopSectionTransform.gameObject;
            RectTransform tabRect = shopTab.GetComponent<RectTransform>();
            Button shopButton = shopTab.GetComponent<Button>();
            Canvas.ForceUpdateCanvases();
            Vector3 tabWorldCentre = tabRect.TransformPoint(tabRect.rect.center);
            PointerEventData pointer = new(eventSystem)
            {
                position = RectTransformUtility.WorldToScreenPoint(null, tabWorldCentre),
                button = PointerEventData.InputButton.Left
            };
            List<RaycastResult> hits = new();
            eventSystem.RaycastAll(pointer, hits);
            bool tabWasHit = hits.Any(hit => hit.gameObject != null && hit.gameObject.transform.IsChildOf(shopTab.transform));
            if (!tabWasHit) throw new InvalidOperationException("The active EventSystem could not raycast the Shop tablet button.");

            shopButton.onClick.Invoke();
            if (!shopSection.activeSelf) throw new InvalidOperationException("The Shop tablet button received a click but did not change sections.");
            controller.SetOpen(false);
            Debug.Log($"Growveld tablet click smoke test passed: {hits.Count} UI raycast hits, Shop tab received a pointer click, and the Shop section opened.");
        }

        [MenuItem("Growveld/Phase 24/Toggle Pause-Help Smoke Test %#F7")]
        public static void TogglePauseHelpSmokeTest()
        {
            if (!EditorApplication.isPlaying)
            {
                Debug.LogWarning("Enter Play Mode before running the Phase 24 pause-help smoke test.");
                return;
            }

            PauseAndHelpController pause = UnityEngine.Object.FindFirstObjectByType<PauseAndHelpController>();
            if (pause == null) throw new MissingReferenceException("The active scene has no PauseAndHelpController.");
            if (pause.IsPaused) pause.Resume();
            else pause.ShowControls();
            Debug.Log($"Growveld Phase 24 pause-help smoke test passed: overlay is now {(pause.IsPaused ? "open" : "closed")}, time scale {Time.timeScale:0.0}, cursor {(Cursor.visible ? "visible" : "hidden")}.");
        }

        [MenuItem("Growveld/Phase 24/Build Windows Prototype")]
        public static void BuildWindowsPrototype()
        {
            const string buildPath = "Build/Growveld/Growveld.exe";
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(buildPath));
            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = buildPath,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            });
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
            {
                throw new InvalidOperationException($"Windows build failed: {report.summary.result}, {report.summary.totalErrors} errors.");
            }
            Debug.Log($"Growveld Windows prototype build passed: {buildPath}, {report.summary.totalSize / (1024f * 1024f):0.0} MB, {report.summary.totalTime.TotalSeconds:0.0} seconds, {report.summary.totalWarnings} warnings.");
        }

        private static void ResetCleanStartingState(Scene scene, GameObject player, GameObject systems)
        {
            PlayerInventory inventory = player.GetComponent<PlayerInventory>();
            inventory?.ClearAll();
            if (inventory != null) EditorUtility.SetDirty(inventory);

            EconomyManager economy = systems.GetComponent<EconomyManager>();
            SerializedObject economySettings = new(economy);
            economySettings.FindProperty("balance").floatValue = 0f;
            economySettings.FindProperty("initialiseOnAwake").boolValue = true;
            economySettings.ApplyModifiedPropertiesWithoutUndo();

            systems.GetComponent<FarmStockManager>()?.ClearAll();
            DeliveryManager deliveries = systems.GetComponent<DeliveryManager>();
            deliveries?.ClearAndRestore(Array.Empty<PendingDelivery>());

            foreach (LandPlot plot in UnityEngine.Object.FindObjectsByType<LandPlot>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                plot.SetOwned(plot.PlotId == "starter");
            }

            player.transform.SetPositionAndRotation(new Vector3(0f, 0.05f, -5f), Quaternion.identity);
            GameTimeManager gameTime = systems.GetComponent<GameTimeManager>();
            SerializedObject timeSettings = new(gameTime);
            timeSettings.FindProperty("day").intValue = 1;
            timeSettings.FindProperty("timeOfDayHours").floatValue = 7f;
            timeSettings.ApplyModifiedPropertiesWithoutUndo();

            UtilityManager utilities = systems.GetComponent<UtilityManager>();
            SerializedObject utilitySettings = new(utilities);
            utilitySettings.FindProperty("currentElectricityKilowattHours").floatValue = 0f;
            utilitySettings.FindProperty("currentWaterLitres").floatValue = 0f;
            utilitySettings.FindProperty("fallbackDayElapsedSeconds").floatValue = 0f;
            utilitySettings.FindProperty("currentDay").intValue = 1;
            utilitySettings.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void EnsureEventSystem(Scene scene)
        {
            EventSystem[] eventSystems = UnityEngine.Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            EventSystem eventSystem = eventSystems.FirstOrDefault(system => system.gameObject.scene == scene);
            if (eventSystem == null)
            {
                GameObject eventSystemObject = new("EventSystem", typeof(EventSystem));
                SceneManager.MoveGameObjectToScene(eventSystemObject, scene);
                eventSystem = eventSystemObject.GetComponent<EventSystem>();
            }

            foreach (EventSystem duplicate in eventSystems)
            {
                if (duplicate != null && duplicate != eventSystem && duplicate.gameObject.scene == scene)
                {
                    UnityEngine.Object.DestroyImmediate(duplicate.gameObject);
                }
            }

            StandaloneInputModule legacyModule = eventSystem.GetComponent<StandaloneInputModule>();
            if (legacyModule != null) UnityEngine.Object.DestroyImmediate(legacyModule);
            InputSystemUIInputModule inputModule = eventSystem.GetComponent<InputSystemUIInputModule>();
            if (inputModule == null) inputModule = eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
            eventSystem.sendNavigationEvents = true;
            eventSystem.pixelDragThreshold = 10;
            EditorUtility.SetDirty(eventSystem);
            EditorUtility.SetDirty(inputModule);
        }

        private static void CreatePolishedEnvironment(Scene scene)
        {
            Material road = CreateMaterial("M_DirtRoad", new Color(0.29f, 0.19f, 0.11f), 0.05f);
            Material field = CreateMaterial("M_BackgroundField", new Color(0.32f, 0.46f, 0.19f), 0.02f);
            Material fieldDark = CreateMaterial("M_BackgroundFieldDark", new Color(0.20f, 0.32f, 0.12f), 0.02f);
            Material wood = CreateMaterial("M_PolishedWood", new Color(0.28f, 0.16f, 0.08f), 0.14f);
            Material leaf = CreateMaterial("M_TreeLeaf", new Color(0.12f, 0.31f, 0.13f), 0.03f);
            Material leafLight = CreateMaterial("M_TreeLeafLight", new Color(0.20f, 0.42f, 0.18f), 0.03f);
            Material metal = CreateMaterial("M_PolishedMetal", new Color(0.33f, 0.40f, 0.42f), 0.55f, 0.25f);
            Material sign = CreateMaterial("M_GrowveldSign", new Color(0.05f, 0.24f, 0.12f), 0.22f);
            Material roof = CreateMaterial("M_ShedRoof", new Color(0.16f, 0.18f, 0.15f), 0.28f, 0.08f);

            Material ground = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/M_Ground.mat");
            if (ground != null)
            {
                ground.SetColor("_BaseColor", new Color(0.31f, 0.43f, 0.20f));
                ground.SetFloat("_Smoothness", 0.03f);
                EditorUtility.SetDirty(ground);
            }

            GameObject root = new("Polished Environment");
            CreatePrimitive(root.transform, PrimitiveType.Cube, "Farm Road", new Vector3(5f, 0.015f, -14f), new Vector3(86f, 0.03f, 5.5f), road);
            CreatePrimitive(root.transform, PrimitiveType.Cube, "Shed Track", new Vector3(-5.5f, 0.022f, -4.2f), new Vector3(3.4f, 0.035f, 17f), road);

            GameObject fields = new("Distant Crop Fields");
            fields.transform.SetParent(root.transform, false);
            CreateField(fields.transform, new Vector3(46f, 0.025f, 19f), new Vector2(20f, 38f), field, fieldDark);
            CreateField(fields.transform, new Vector3(-28f, 0.025f, 23f), new Vector2(22f, 35f), field, fieldDark);
            CreateField(fields.transform, new Vector3(15f, 0.025f, 48f), new Vector2(45f, 18f), field, fieldDark);

            GameObject entrance = new("Growveld Farm Entrance");
            entrance.transform.SetParent(root.transform, false);
            CreatePrimitive(entrance.transform, PrimitiveType.Cube, "Left Post", new Vector3(-4.5f, 1.4f, -10f), new Vector3(0.42f, 2.8f, 0.42f), wood);
            CreatePrimitive(entrance.transform, PrimitiveType.Cube, "Right Post", new Vector3(4.5f, 1.4f, -10f), new Vector3(0.42f, 2.8f, 0.42f), wood);
            CreatePrimitive(entrance.transform, PrimitiveType.Cube, "Header", new Vector3(0f, 2.65f, -10f), new Vector3(9.4f, 0.45f, 0.45f), sign);
            CreateWorldLabel(entrance.transform, "GROWVELD  •  STARTER FARM", new Vector3(0f, 2.64f, -9.74f), Quaternion.identity, 0.045f);

            GameObject scenery = new("Trees and Farm Scenery");
            scenery.transform.SetParent(root.transform, false);
            Vector3[] treePositions =
            {
                new(-16f, 0f, -8f), new(-20f, 0f, 5f), new(-18f, 0f, 33f),
                new(34f, 0f, -8f), new(38f, 0f, 10f), new(36f, 0f, 35f),
                new(-5f, 0f, 42f), new(30f, 0f, 48f)
            };
            for (int index = 0; index < treePositions.Length; index++) CreateTree(scenery.transform, treePositions[index], wood, index % 2 == 0 ? leaf : leafLight, 0.85f + index % 3 * 0.12f);

            CreatePrimitive(scenery.transform, PrimitiveType.Cylinder, "Water Tank", new Vector3(-11.5f, 2f, 4.5f), new Vector3(2.2f, 2f, 2.2f), metal);
            CreatePrimitive(scenery.transform, PrimitiveType.Cylinder, "Water Tank Cap", new Vector3(-11.5f, 4.05f, 4.5f), new Vector3(2.28f, 0.14f, 2.28f), roof);

            GameObject starterBuilding = FindRoot(scene, "Starter Building");
            if (starterBuilding != null)
            {
                Transform buildingRoof = starterBuilding.transform.Find("Roof");
                if (buildingRoof != null) buildingRoof.GetComponent<Renderer>().sharedMaterial = roof;
                CreatePrimitive(starterBuilding.transform, PrimitiveType.Cube, "Front Beam", new Vector3(-5.5f, 2.65f, 2.05f), new Vector3(6.8f, 0.52f, 0.18f), sign, true);
                CreateWorldLabel(starterBuilding.transform, "STARTER SHED", new Vector3(-5.5f, 2.64f, 1.94f), Quaternion.identity, 0.038f);
            }

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = 90f;
            RenderSettings.fogEndDistance = 330f;
            RenderSettings.fogColor = new Color(0.65f, 0.76f, 0.74f);
        }

        private static void ConfigurePolishedHUD(Scene scene, GameObject player, GameObject systems)
        {
            PlayerCarryController carryController = player.GetComponent<PlayerCarryController>();
            BusinessTabletController tablet = player.GetComponent<BusinessTabletController>();
            PlacementController placement = player.GetComponent<PlacementController>();

            GameObject hud = CreateCanvas("Polish HUD", 12);
            CreateCrosshair(hud.transform);
            CreateQuickHelp(hud.transform);

            GameObject carryPanel = CreatePanel(hud.transform, "Carry Status", new Color(0.02f, 0.06f, 0.035f, 0.91f));
            RectTransform carryRect = carryPanel.GetComponent<RectTransform>();
            Anchor(carryRect, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 132f), new Vector2(720f, 50f));
            CanvasGroup carryGroup = carryPanel.AddComponent<CanvasGroup>();
            Text carryText = CreateText(carryPanel.transform, "Carry Copy", string.Empty, 20, TextAnchor.MiddleCenter, Color.white);
            Stretch(carryText.rectTransform, 8f);
            CarryStatusUI carryUI = carryPanel.AddComponent<CarryStatusUI>();
            SerializedObject carrySettings = new(carryUI);
            carrySettings.FindProperty("carryController").objectReferenceValue = carryController;
            carrySettings.FindProperty("panelGroup").objectReferenceValue = carryGroup;
            carrySettings.FindProperty("statusText").objectReferenceValue = carryText;
            carrySettings.ApplyModifiedPropertiesWithoutUndo();

            GameObject pauseCanvas = CreateCanvas("Pause UI", 100);
            GameObject pauseRoot = CreateFullscreenPanel(pauseCanvas.transform, "Pause Menu", new Color(0.012f, 0.024f, 0.016f, 0.96f));
            CreateTextBlock(pauseRoot.transform, "GROWVELD", 52, new Vector2(0f, 242f), new Vector2(700f, 80f), FontStyle.Bold);
            CreateTextBlock(pauseRoot.transform, "PROTOTYPE PAUSED", 24, new Vector2(0f, 185f), new Vector2(700f, 44f), FontStyle.Normal, new Color(0.55f, 0.82f, 0.58f));
            Button resumeButton = CreateMenuButton(pauseRoot.transform, "Resume", "RESUME", new Vector2(0f, 55f), new Color(0.16f, 0.52f, 0.24f));
            Button controlsButton = CreateMenuButton(pauseRoot.transform, "Controls", "CONTROLS", new Vector2(0f, -45f), new Color(0.14f, 0.33f, 0.24f));
            CreateTextBlock(pauseRoot.transform, "F5 Save  •  F9 Load  •  Esc Resume", 20, new Vector2(0f, -170f), new Vector2(700f, 50f), FontStyle.Normal, new Color(0.72f, 0.78f, 0.72f));

            GameObject controlsRoot = CreateFullscreenPanel(pauseCanvas.transform, "Controls Reference", new Color(0.012f, 0.024f, 0.016f, 0.985f));
            CreateTextBlock(controlsRoot.transform, "CONTROLS", 42, new Vector2(0f, 350f), new Vector2(760f, 64f), FontStyle.Bold);
            string controlsCopy =
                "W A S D    Move                         Mouse    Look\n" +
                "Shift          Sprint                        E             Interact / pick up / drop\n" +
                "1–5 / Wheel  Select hotbar             B             Build selected equipment\n" +
                "R               Rotate preview              Left Click  Place object\n" +
                "Delete        Sell object while moving   T             Business tablet\n" +
                "F5             Save                           F9           Load\n" +
                "H               Controls                       Esc          Pause / back\n\n" +
                "FIRST FARM: Open the tablet, buy equipment and supplies, wait five game minutes for delivery,\n" +
                "place equipment on owned land, plant a seed, care for it, harvest, dry, store, then sell.";
            Text controlsText = CreateTextBlock(controlsRoot.transform, controlsCopy, 22, new Vector2(0f, 40f), new Vector2(1120f, 530f), FontStyle.Normal);
            controlsText.alignment = TextAnchor.MiddleLeft;
            Button closeControls = CreateMenuButton(controlsRoot.transform, "Back", "BACK", new Vector2(0f, -325f), new Color(0.16f, 0.52f, 0.24f));

            PauseAndHelpController pause = player.GetComponent<PauseAndHelpController>() ?? player.AddComponent<PauseAndHelpController>();
            Behaviour[] gameplayBehaviours =
            {
                player.GetComponent<FirstPersonController>(),
                player.GetComponent<PlayerInteractor>(),
                player.GetComponent<InventoryHotbarInput>(),
                placement,
                tablet
            };
            SerializedObject pauseSettings = new(pause);
            pauseSettings.FindProperty("pauseRoot").objectReferenceValue = pauseRoot;
            pauseSettings.FindProperty("controlsRoot").objectReferenceValue = controlsRoot;
            pauseSettings.FindProperty("resumeButton").objectReferenceValue = resumeButton;
            pauseSettings.FindProperty("controlsButton").objectReferenceValue = controlsButton;
            pauseSettings.FindProperty("closeControlsButton").objectReferenceValue = closeControls;
            SerializedProperty behaviours = pauseSettings.FindProperty("gameplayBehaviours");
            behaviours.arraySize = gameplayBehaviours.Length;
            for (int index = 0; index < gameplayBehaviours.Length; index++) behaviours.GetArrayElementAtIndex(index).objectReferenceValue = gameplayBehaviours[index];
            pauseSettings.FindProperty("tabletController").objectReferenceValue = tablet;
            pauseSettings.FindProperty("placementController").objectReferenceValue = placement;
            pauseSettings.ApplyModifiedPropertiesWithoutUndo();
            pauseRoot.SetActive(false);
            controlsRoot.SetActive(false);
        }

        private static void ConfigureFeedbackAudio(GameObject player, GameObject systems)
        {
            AudioSource source = systems.GetComponent<AudioSource>();
            if (source == null) source = systems.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            PrototypeFeedbackAudio feedback = systems.GetComponent<PrototypeFeedbackAudio>();
            if (feedback == null) feedback = systems.AddComponent<PrototypeFeedbackAudio>();
            SerializedObject settings = new(feedback);
            settings.FindProperty("economy").objectReferenceValue = systems.GetComponent<EconomyManager>();
            settings.FindProperty("deliveries").objectReferenceValue = systems.GetComponent<DeliveryManager>();
            settings.FindProperty("carryController").objectReferenceValue = player.GetComponent<PlayerCarryController>();
            settings.FindProperty("volume").floatValue = 0.16f;
            settings.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void PolishExistingHUD(Scene scene)
        {
            GameObject economyUI = FindRoot(scene, "Economy UI");
            if (economyUI != null)
            {
                Transform old = economyUI.transform.Find("Money Backdrop");
                if (old != null) UnityEngine.Object.DestroyImmediate(old.gameObject);
                GameObject backdrop = CreatePanel(economyUI.transform, "Money Backdrop", new Color(0.02f, 0.055f, 0.03f, 0.86f));
                Anchor(backdrop.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(20f, -18f), new Vector2(270f, 64f));
                backdrop.transform.SetAsFirstSibling();
            }
        }

        private static void ValidatePrototype(Scene scene)
        {
            List<string> failures = new();
            foreach (string testRootName in TestRootNames) if (FindRoot(scene, testRootName) != null) failures.Add($"test fixture remains: {testRootName}");
            GameObject player = FindRoot(scene, "Player");
            GameObject systems = FindRoot(scene, "Game Systems");
            if (player == null) failures.Add("Player missing");
            if (systems == null) failures.Add("Game Systems missing");

            Type[] playerTypes = { typeof(FirstPersonController), typeof(PlayerInteractor), typeof(PlayerCarryController), typeof(PlayerInventory), typeof(PlacementController), typeof(BusinessTabletController), typeof(PauseAndHelpController) };
            Type[] systemTypes = { typeof(EconomyManager), typeof(LandManager), typeof(DeliveryManager), typeof(FarmStockManager), typeof(SellingManager), typeof(UtilityManager), typeof(GameTimeManager), typeof(SaveSystem), typeof(PrototypeFeedbackAudio) };
            if (player != null) foreach (Type type in playerTypes) if (player.GetComponent(type) == null) failures.Add($"Player component missing: {type.Name}");
            if (systems != null) foreach (Type type in systemTypes) if (systems.GetComponent(type) == null) failures.Add($"System component missing: {type.Name}");

            if (UnityEngine.Object.FindObjectsByType<InventoryPickup>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length > 0) failures.Add("free inventory pickups remain");
            if (UnityEngine.Object.FindObjectsByType<PlacedObject>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length > 0) failures.Add("free placed equipment remains in the clean scene");
            if (UnityEngine.Object.FindObjectsByType<HarvestBatch>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length > 0) failures.Add("test harvest batches remain");
            if (UnityEngine.Object.FindObjectsByType<PlantingContainer>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length < 1) failures.Add("no planting positions found");
            if (FindRoot(scene, "Polished Environment") == null || FindRoot(scene, "Polish HUD") == null || FindRoot(scene, "Pause UI") == null) failures.Add("polish roots missing");
            EventSystem eventSystem = UnityEngine.Object.FindFirstObjectByType<EventSystem>(FindObjectsInactive.Include);
            if (eventSystem == null) failures.Add("UI EventSystem missing");
            else if (eventSystem.GetComponent<InputSystemUIInputModule>() == null) failures.Add("Input System UI module missing");

            string[] itemGuids = AssetDatabase.FindAssets("t:ItemDefinition", new[] { "Assets/_Project/ScriptableObjects/Items" });
            string[] placeableGuids = AssetDatabase.FindAssets("t:PlaceableDefinition", new[] { "Assets/_Project/ScriptableObjects/Placeables" });
            if (itemGuids.Length != 8) failures.Add($"expected 8 items, found {itemGuids.Length}");
            if (placeableGuids.Length != 5) failures.Add($"expected 5 placeables, found {placeableGuids.Length}");
            if (!EditorBuildSettings.scenes.Any(buildScene => buildScene.enabled && buildScene.path == ScenePath)) failures.Add("PrototypeFarm is not enabled in Build Settings");
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                int missingScripts = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(root);
                if (missingScripts > 0) failures.Add($"{missingScripts} missing scripts under {root.name}");
            }

            if (failures.Count > 0) throw new InvalidOperationException("Phase 24 validation failed:\n- " + string.Join("\n- ", failures));
            Debug.Log($"Growveld complete-prototype validation passed: {scene.rootCount} scene roots, {itemGuids.Length} shop items, {placeableGuids.Length} placeables, all 24 phase systems present, clean starting state, no test fixtures, no missing scripts, and Build Settings configured.");
        }

        private static void CreateField(Transform parent, Vector3 centre, Vector2 size, Material field, Material row)
        {
            CreatePrimitive(parent, PrimitiveType.Cube, "Field Base", centre, new Vector3(size.x, 0.03f, size.y), field);
            int rowCount = Mathf.FloorToInt(size.x / 2.2f);
            for (int index = 0; index < rowCount; index++)
            {
                float x = centre.x - size.x * 0.5f + 1.1f + index * 2.2f;
                CreatePrimitive(parent, PrimitiveType.Cube, $"Crop Row {index + 1}", new Vector3(x, centre.y + 0.09f, centre.z), new Vector3(0.8f, 0.16f, size.y - 1f), row);
            }
        }

        private static void CreateTree(Transform parent, Vector3 position, Material trunk, Material canopy, float scale)
        {
            GameObject tree = new($"Tree {parent.childCount + 1}");
            tree.transform.SetParent(parent, false);
            CreatePrimitive(tree.transform, PrimitiveType.Cylinder, "Trunk", position + Vector3.up * 1.6f * scale, new Vector3(0.42f, 1.6f, 0.42f) * scale, trunk);
            CreatePrimitive(tree.transform, PrimitiveType.Sphere, "Lower Canopy", position + Vector3.up * 3.7f * scale, new Vector3(2.2f, 1.7f, 2.2f) * scale, canopy);
            CreatePrimitive(tree.transform, PrimitiveType.Sphere, "Upper Canopy", position + new Vector3(0.35f, 4.8f, 0.1f) * scale, new Vector3(1.55f, 1.45f, 1.55f) * scale, canopy);
        }

        private static GameObject CreatePrimitive(Transform parent, PrimitiveType primitiveType, string name, Vector3 position, Vector3 scale, Material material, bool keepCollider = false)
        {
            GameObject gameObject = GameObject.CreatePrimitive(primitiveType);
            gameObject.name = name;
            gameObject.transform.SetParent(parent, false);
            gameObject.transform.position = position;
            gameObject.transform.localScale = scale;
            gameObject.GetComponent<Renderer>().sharedMaterial = material;
            Collider collider = gameObject.GetComponent<Collider>();
            if (!keepCollider && collider != null) UnityEngine.Object.DestroyImmediate(collider);
            return gameObject;
        }

        private static void CreateWorldLabel(Transform parent, string copy, Vector3 position, Quaternion rotation, float characterSize)
        {
            GameObject labelObject = new("Label", typeof(TextMesh));
            labelObject.transform.SetParent(parent, false);
            labelObject.transform.position = position;
            labelObject.transform.rotation = rotation;
            TextMesh label = labelObject.GetComponent<TextMesh>();
            label.text = copy;
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 80;
            label.characterSize = characterSize;
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.color = new Color(0.91f, 0.96f, 0.88f);
        }

        private static void CreateCrosshair(Transform parent)
        {
            GameObject root = new("Crosshair", typeof(RectTransform));
            root.transform.SetParent(parent, false);
            RectTransform rootRect = root.GetComponent<RectTransform>();
            Anchor(rootRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(30f, 30f));
            Color colour = new(1f, 1f, 1f, 0.9f);
            CreateCrosshairPart(root.transform, "Centre", Vector2.zero, new Vector2(3f, 3f), colour);
            CreateCrosshairPart(root.transform, "Top", new Vector2(0f, 8f), new Vector2(2f, 7f), colour);
            CreateCrosshairPart(root.transform, "Bottom", new Vector2(0f, -8f), new Vector2(2f, 7f), colour);
            CreateCrosshairPart(root.transform, "Left", new Vector2(-8f, 0f), new Vector2(7f, 2f), colour);
            CreateCrosshairPart(root.transform, "Right", new Vector2(8f, 0f), new Vector2(7f, 2f), colour);
        }

        private static void CreateCrosshairPart(Transform parent, string name, Vector2 position, Vector2 size, Color colour)
        {
            GameObject part = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            part.transform.SetParent(parent, false);
            Anchor(part.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, size);
            part.GetComponent<Image>().color = colour;
        }

        private static void CreateQuickHelp(Transform parent)
        {
            GameObject panel = CreatePanel(parent, "Quick Help", new Color(0.02f, 0.055f, 0.03f, 0.86f));
            Anchor(panel.GetComponent<RectTransform>(), Vector2.one, Vector2.one, Vector2.one, new Vector2(-22f, -20f), new Vector2(340f, 92f));
            Text text = CreateText(panel.transform, "Quick Help Copy", "GROWVELD\n[T] TABLET     [H] HELP     [ESC] PAUSE", 18, TextAnchor.MiddleCenter, Color.white);
            text.fontStyle = FontStyle.Bold;
            Stretch(text.rectTransform, 8f);
        }

        private static GameObject CreateCanvas(string name, int sortingOrder)
        {
            GameObject canvasObject = new(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            return canvasObject;
        }

        private static GameObject CreateFullscreenPanel(Transform parent, string name, Color colour)
        {
            GameObject panel = CreatePanel(parent, name, colour);
            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return panel;
        }

        private static GameObject CreatePanel(Transform parent, string name, Color colour)
        {
            GameObject panel = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panel.transform.SetParent(parent, false);
            panel.GetComponent<Image>().color = colour;
            return panel;
        }

        private static Text CreateTextBlock(Transform parent, string copy, int fontSize, Vector2 position, Vector2 size, FontStyle style, Color? colour = null)
        {
            Text text = CreateText(parent, "Copy", copy, fontSize, TextAnchor.MiddleCenter, colour ?? Color.white);
            text.fontStyle = style;
            Anchor(text.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, size);
            return text;
        }

        private static Text CreateText(Transform parent, string name, string copy, int fontSize, TextAnchor alignment, Color colour)
        {
            GameObject textObject = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            textObject.transform.SetParent(parent, false);
            Text text = textObject.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = colour;
            text.text = copy;
            return text;
        }

        private static Button CreateMenuButton(Transform parent, string name, string copy, Vector2 position, Color colour)
        {
            GameObject buttonObject = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            Anchor(buttonObject.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, new Vector2(360f, 72f));
            buttonObject.GetComponent<Image>().color = colour;
            Text label = CreateText(buttonObject.transform, "Label", copy, 24, TextAnchor.MiddleCenter, Color.white);
            label.fontStyle = FontStyle.Bold;
            Stretch(label.rectTransform, 4f);
            return buttonObject.GetComponent<Button>();
        }

        private static void Anchor(RectTransform rect, Vector2 min, Vector2 max, Vector2 pivot, Vector2 position, Vector2 size)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void Stretch(RectTransform rect, float inset)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(inset, inset);
            rect.offsetMax = new Vector2(-inset, -inset);
        }

        private static Material CreateMaterial(string name, Color colour, float smoothness, float metallic = 0f)
        {
            string path = $"{MaterialFolder}/{name}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            material.shader = shader;
            material.SetColor("_BaseColor", colour);
            material.SetFloat("_Smoothness", smoothness);
            material.SetFloat("_Metallic", metallic);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder)) return;
            string parent = System.IO.Path.GetDirectoryName(folder)?.Replace('\\', '/');
            string leaf = System.IO.Path.GetFileName(folder);
            if (!string.IsNullOrEmpty(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }

        private static GameObject FindRoot(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects()) if (root.name == name) return root;
            return null;
        }

        private static void RemoveRoot(Scene scene, string name)
        {
            GameObject root = FindRoot(scene, name);
            if (root != null) UnityEngine.Object.DestroyImmediate(root);
        }
    }
}
