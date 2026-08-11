using System.Collections.Generic;
using System.IO;
using Growveld.Building;
using Growveld.Economy;
using Growveld.Inventory;
using Growveld.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Growveld.Editor
{
    public static class Phase8PlacementSetup
    {
        private const string ScenePath = "Assets/_Project/Scenes/PrototypeFarm.unity";
        private const string PlaceableFolder = "Assets/_Project/ScriptableObjects/Placeables";
        private const string PrefabFolder = "Assets/_Project/Prefabs/Equipment";
        private const string EquipmentMaterialPath = "Assets/_Project/Materials/M_PlaceableEquipment.mat";
        private const string CoverageMaterialPath = "Assets/_Project/Materials/M_LightCoverage.mat";

        [MenuItem("Growveld/Phase 8/Rebuild Placement System")]
        public static void ConfigurePlacement()
        {
            EnsureFolder("Assets/_Project/Prefabs");
            EnsureFolder(PrefabFolder);
            EnsureFolder(PlaceableFolder);

            Material equipmentMaterial = CreateMaterial(EquipmentMaterialPath, new Color(0.28f, 0.48f, 0.32f));
            Material coverageMaterial = CreateMaterial(CoverageMaterialPath, new Color(0.2f, 0.75f, 1f));

            Dictionary<string, Vector3> footprints = new()
            {
                ["grow_pot"] = new Vector3(1.2f, 0.8f, 1.2f),
                ["grow_light"] = new Vector3(1.2f, 2.7f, 1.2f),
                ["drying_rack"] = new Vector3(3f, 2.1f, 1.2f),
                ["storage_bin"] = new Vector3(1.8f, 1.4f, 1.4f),
                ["grow_room"] = new Vector3(8f, 3.4f, 8f)
            };

            Dictionary<string, PlaceableDefinition> definitions = new();
            foreach ((string id, Vector3 footprint) in footprints)
            {
                ItemDefinition item = LoadItem(id);
                float coverageRadius = id == "grow_light" ? 3.5f : 0f;
                definitions[id] = CreateDefinition(id, item, footprint, coverageRadius);
            }

            foreach ((string id, PlaceableDefinition definition) in definitions)
            {
                GameObject prefab = CreatePrefab(id, definition, equipmentMaterial, coverageMaterial);
                SerializedObject definitionSettings = new(definition);
                definitionSettings.FindProperty("prefab").objectReferenceValue = prefab;
                definitionSettings.ApplyModifiedPropertiesWithoutUndo();

                SerializedObject itemSettings = new(definition.ItemDefinition);
                itemSettings.FindProperty("placeableDefinition").objectReferenceValue = definition;
                itemSettings.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(definition);
                EditorUtility.SetDirty(definition.ItemDefinition);
            }

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject player = FindRoot(scene, "Player");
            GameObject systems = FindRoot(scene, "Game Systems");
            if (player == null || systems == null)
            {
                throw new MissingReferenceException("Player and Game Systems are required.");
            }

            PlayerInventory inventory = player.GetComponent<PlayerInventory>();
            LandManager landManager = systems.GetComponent<LandManager>();
            EconomyManager economy = systems.GetComponent<EconomyManager>();
            PlacementController controller = player.GetComponent<PlacementController>() ?? player.AddComponent<PlacementController>();
            SerializedObject controllerSettings = new(controller);
            controllerSettings.FindProperty("viewCamera").objectReferenceValue = player.GetComponentInChildren<Camera>(true);
            controllerSettings.FindProperty("inventory").objectReferenceValue = inventory;
            controllerSettings.FindProperty("landManager").objectReferenceValue = landManager;
            controllerSettings.FindProperty("economy").objectReferenceValue = economy;
            controllerSettings.FindProperty("placementDistance").floatValue = 18f;
            controllerSettings.FindProperty("gridSize").floatValue = 0.25f;
            controllerSettings.ApplyModifiedPropertiesWithoutUndo();

            // One pot is temporarily supplied so Phase 8 can be tested before the delayed shop exists.
            inventory.ClearAll();
            inventory.Add(definitions["grow_pot"].ItemDefinition, 1);
            EditorUtility.SetDirty(inventory);

            RemoveRoot(scene, "Placement UI");
            CreatePlacementUI(controller);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = player;
            Debug.Log("Growveld Phase 8 setup complete: placeable prefabs, owned-land validation, previews, moving, and selling created.");
        }

        private static PlaceableDefinition CreateDefinition(string id, ItemDefinition item, Vector3 footprint, float coverageRadius)
        {
            string path = $"{PlaceableFolder}/Placeable_{id}.asset";
            PlaceableDefinition definition = AssetDatabase.LoadAssetAtPath<PlaceableDefinition>(path);
            if (definition == null)
            {
                definition = ScriptableObject.CreateInstance<PlaceableDefinition>();
                AssetDatabase.CreateAsset(definition, path);
            }

            SerializedObject settings = new(definition);
            settings.FindProperty("placeableId").stringValue = id;
            settings.FindProperty("itemDefinition").objectReferenceValue = item;
            settings.FindProperty("footprintSize").vector3Value = footprint;
            settings.FindProperty("placementOffset").vector3Value = Vector3.zero;
            settings.FindProperty("rotationStep").floatValue = 15f;
            settings.FindProperty("sellRefundFraction").floatValue = 0.7f;
            settings.FindProperty("lightCoverageRadius").floatValue = coverageRadius;
            settings.ApplyModifiedPropertiesWithoutUndo();
            return definition;
        }

        private static GameObject CreatePrefab(string id, PlaceableDefinition definition, Material material, Material coverageMaterial)
        {
            GameObject root = new(definition.DisplayName, typeof(PlacedObject));
            SerializedObject placedSettings = new(root.GetComponent<PlacedObject>());
            placedSettings.FindProperty("definition").objectReferenceValue = definition;
            placedSettings.ApplyModifiedPropertiesWithoutUndo();

            switch (id)
            {
                case "grow_pot":
                    CreatePrimitive(root.transform, "Pot", PrimitiveType.Cylinder, new Vector3(0f, 0.4f, 0f), new Vector3(0.9f, 0.4f, 0.9f), material);
                    break;
                case "grow_light":
                    CreatePrimitive(root.transform, "Stand", PrimitiveType.Cube, new Vector3(0f, 1.25f, 0f), new Vector3(0.12f, 2.5f, 0.12f), material);
                    CreatePrimitive(root.transform, "Lamp", PrimitiveType.Cube, new Vector3(0f, 2.45f, 0f), new Vector3(1.2f, 0.16f, 0.65f), material);
                    GameObject coverage = CreatePrimitive(root.transform, "Coverage Preview", PrimitiveType.Cylinder, new Vector3(0f, 0.035f, 0f), new Vector3(7f, 0.025f, 7f), coverageMaterial);
                    Object.DestroyImmediate(coverage.GetComponent<Collider>());
                    coverage.SetActive(false);
                    break;
                case "drying_rack":
                    CreatePrimitive(root.transform, "Frame", PrimitiveType.Cube, new Vector3(0f, 1f, 0f), new Vector3(3f, 2f, 0.2f), material);
                    CreatePrimitive(root.transform, "Shelf A", PrimitiveType.Cube, new Vector3(0f, 0.55f, 0.15f), new Vector3(2.8f, 0.12f, 1.1f), material);
                    CreatePrimitive(root.transform, "Shelf B", PrimitiveType.Cube, new Vector3(0f, 1.35f, 0.15f), new Vector3(2.8f, 0.12f, 1.1f), material);
                    break;
                case "storage_bin":
                    CreatePrimitive(root.transform, "Bin", PrimitiveType.Cube, new Vector3(0f, 0.7f, 0f), new Vector3(1.8f, 1.4f, 1.4f), material);
                    break;
                case "grow_room":
                    CreatePrimitive(root.transform, "Floor", PrimitiveType.Cube, new Vector3(0f, 0.1f, 0f), new Vector3(8f, 0.2f, 8f), material);
                    CreatePrimitive(root.transform, "Back Wall", PrimitiveType.Cube, new Vector3(0f, 1.7f, 3.9f), new Vector3(8f, 3.4f, 0.2f), material);
                    CreatePrimitive(root.transform, "Left Wall", PrimitiveType.Cube, new Vector3(-3.9f, 1.7f, 0f), new Vector3(0.2f, 3.4f, 8f), material);
                    CreatePrimitive(root.transform, "Right Wall", PrimitiveType.Cube, new Vector3(3.9f, 1.7f, 0f), new Vector3(0.2f, 3.4f, 8f), material);
                    CreatePrimitive(root.transform, "Roof", PrimitiveType.Cube, new Vector3(0f, 3.3f, 0f), new Vector3(8f, 0.2f, 8f), material);
                    break;
            }

            string prefabPath = $"{PrefabFolder}/{definition.DisplayName}.prefab";
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static GameObject CreatePrimitive(Transform parent, string name, PrimitiveType type, Vector3 localPosition, Vector3 scale, Material material)
        {
            GameObject part = GameObject.CreatePrimitive(type);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.transform.localScale = scale;
            part.GetComponent<MeshRenderer>().sharedMaterial = material;
            return part;
        }

        private static void CreatePlacementUI(PlacementController controller)
        {
            GameObject canvasObject = new("Placement UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 8;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            GameObject panel = new("Construction Panel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup), typeof(PlacementHUD));
            panel.transform.SetParent(canvasObject.transform, false);
            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(1f, 1f);
            panelRect.anchorMax = new Vector2(1f, 1f);
            panelRect.pivot = new Vector2(1f, 1f);
            panelRect.anchoredPosition = new Vector2(-28f, -28f);
            panelRect.sizeDelta = new Vector2(540f, 210f);
            panel.GetComponent<Image>().color = new Color(0.02f, 0.04f, 0.025f, 0.9f);

            GameObject textObject = new("Details", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            textObject.transform.SetParent(panel.transform, false);
            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(18f, 14f);
            textRect.offsetMax = new Vector2(-18f, -14f);
            Text text = textObject.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 22;
            text.alignment = TextAnchor.UpperLeft;
            text.color = Color.white;

            PlacementHUD hud = panel.GetComponent<PlacementHUD>();
            SerializedObject settings = new(hud);
            settings.FindProperty("placementController").objectReferenceValue = controller;
            settings.FindProperty("panelGroup").objectReferenceValue = panel.GetComponent<CanvasGroup>();
            settings.FindProperty("detailsText").objectReferenceValue = text;
            settings.ApplyModifiedPropertiesWithoutUndo();
        }

        private static ItemDefinition LoadItem(string id)
        {
            ItemDefinition item = AssetDatabase.LoadAssetAtPath<ItemDefinition>($"Assets/_Project/ScriptableObjects/Items/Item_{id}.asset");
            if (item == null) throw new MissingReferenceException($"Item definition not found: {id}");
            return item;
        }

        private static Material CreateMaterial(string path, Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            material.shader = shader;
            material.SetColor("_BaseColor", color);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        private static GameObject FindRoot(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects()) if (root.name == name) return root;
            return null;
        }

        private static void RemoveRoot(Scene scene, string name)
        {
            GameObject root = FindRoot(scene, name);
            if (root != null) Object.DestroyImmediate(root);
        }
    }
}
