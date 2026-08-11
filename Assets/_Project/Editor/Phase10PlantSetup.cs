using System.IO;
using Growveld.Farming;
using Growveld.Interaction;
using Growveld.Inventory;
using Growveld.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Growveld.Editor
{
    public static class Phase10PlantSetup
    {
        private const string ScenePath = "Assets/_Project/Scenes/PrototypeFarm.unity";
        private const string PlantFolder = "Assets/_Project/ScriptableObjects/Plants";
        private const string PlantDefinitionPath = PlantFolder + "/Plant_GenericCrop.asset";
        private const string PlantPrefabFolder = "Assets/_Project/Prefabs/Plants";
        private const string PlantPrefabPath = PlantPrefabFolder + "/Generic Plant.prefab";
        private const string PotPrefabPath = "Assets/_Project/Prefabs/Equipment/Grow Pot.prefab";
        private const string PlantMaterialPath = "Assets/_Project/Materials/M_GenericPlant.mat";

        [MenuItem("Growveld/Phase 10/Rebuild Plant Foundation")]
        public static void ConfigurePlants()
        {
            EnsureFolder(PlantFolder);
            EnsureFolder(PlantPrefabFolder);
            PlantDefinition definition = CreatePlantDefinition();
            GameObject plantPrefab = CreatePlantPrefab(definition);
            ConfigureGrowPotPrefab(definition, plantPrefab);

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject player = FindRoot(scene, "Player");
            RemoveRoot(scene, "Plant Growth Test Pot");
            GameObject potPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PotPrefabPath);
            GameObject testPot = (GameObject)PrefabUtility.InstantiatePrefab(potPrefab, scene);
            testPot.name = "Plant Growth Test Pot";
            testPot.transform.position = new Vector3(4.5f, 0.1f, 4f);

            RemoveRoot(scene, "Plant Context UI");
            CreatePlantContextUI(player.GetComponent<PlayerInteractor>());

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = testPot;
            Debug.Log("Growveld Phase 10 setup complete: configurable 30-minute plant lifecycle, five visual stages, grow-pot planting, and contextual HUD created.");
        }

        private static PlantDefinition CreatePlantDefinition()
        {
            PlantDefinition definition = AssetDatabase.LoadAssetAtPath<PlantDefinition>(PlantDefinitionPath);
            if (definition == null)
            {
                definition = ScriptableObject.CreateInstance<PlantDefinition>();
                AssetDatabase.CreateAsset(definition, PlantDefinitionPath);
            }

            SerializedObject settings = new(definition);
            settings.FindProperty("plantId").stringValue = "generic_crop";
            settings.FindProperty("displayName").stringValue = "Generic Cannabis Plant";
            settings.FindProperty("germinationSeconds").floatValue = 120f;
            settings.FindProperty("seedlingSeconds").floatValue = 240f;
            settings.FindProperty("vegetativeSeconds").floatValue = 540f;
            settings.FindProperty("floweringSeconds").floatValue = 900f;
            settings.FindProperty("baseYieldKilograms").floatValue = 0.45f;
            settings.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(definition);
            return definition;
        }

        private static GameObject CreatePlantPrefab(PlantDefinition definition)
        {
            Material plantMaterial = CreateMaterial();
            GameObject root = new("Generic Plant", typeof(SphereCollider), typeof(PlantInstance));
            SphereCollider collider = root.GetComponent<SphereCollider>();
            collider.center = new Vector3(0f, 0.8f, 0f);
            collider.radius = 0.7f;

            GameObject visualRoot = new("Visual Root");
            visualRoot.transform.SetParent(root.transform, false);
            GameObject[] stages = new GameObject[5];
            stages[0] = CreateStage(visualRoot.transform, "Germination", PrimitiveType.Sphere, new Vector3(0f, 0.08f, 0f), new Vector3(0.14f, 0.08f, 0.14f), plantMaterial);
            stages[1] = CreateStage(visualRoot.transform, "Seedling", PrimitiveType.Cylinder, new Vector3(0f, 0.28f, 0f), new Vector3(0.18f, 0.28f, 0.18f), plantMaterial);
            stages[2] = CreateStage(visualRoot.transform, "Vegetative", PrimitiveType.Sphere, new Vector3(0f, 0.75f, 0f), new Vector3(0.8f, 0.9f, 0.8f), plantMaterial);
            stages[3] = CreateStage(visualRoot.transform, "Flowering", PrimitiveType.Sphere, new Vector3(0f, 1.05f, 0f), new Vector3(1.05f, 1.35f, 1.05f), plantMaterial);
            stages[4] = CreateStage(visualRoot.transform, "Harvest Ready", PrimitiveType.Sphere, new Vector3(0f, 1.2f, 0f), new Vector3(1.25f, 1.55f, 1.25f), plantMaterial);

            PlantInstance plant = root.GetComponent<PlantInstance>();
            SerializedObject settings = new(plant);
            settings.FindProperty("definition").objectReferenceValue = definition;
            settings.FindProperty("visualRoot").objectReferenceValue = visualRoot.transform;
            SerializedProperty visualArray = settings.FindProperty("stageVisuals");
            visualArray.arraySize = stages.Length;
            for (int index = 0; index < stages.Length; index++) visualArray.GetArrayElementAtIndex(index).objectReferenceValue = stages[index];
            settings.FindProperty("elapsedGrowthSeconds").floatValue = 0f;
            settings.FindProperty("externalGrowthMultiplier").floatValue = 1f;
            settings.FindProperty("simulationEnabled").boolValue = true;
            settings.ApplyModifiedPropertiesWithoutUndo();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PlantPrefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static GameObject CreateStage(Transform parent, string name, PrimitiveType type, Vector3 position, Vector3 scale, Material material)
        {
            GameObject stage = GameObject.CreatePrimitive(type);
            stage.name = name;
            stage.transform.SetParent(parent, false);
            stage.transform.localPosition = position;
            stage.transform.localScale = scale;
            stage.GetComponent<MeshRenderer>().sharedMaterial = material;
            Object.DestroyImmediate(stage.GetComponent<Collider>());
            stage.SetActive(false);
            return stage;
        }

        private static void ConfigureGrowPotPrefab(PlantDefinition definition, GameObject plantPrefab)
        {
            GameObject pot = PrefabUtility.LoadPrefabContents(PotPrefabPath);
            PlantingContainer container = pot.GetComponent<PlantingContainer>() ?? pot.AddComponent<PlantingContainer>();
            Transform socket = pot.transform.Find("Plant Socket");
            if (socket == null)
            {
                GameObject socketObject = new("Plant Socket");
                socketObject.transform.SetParent(pot.transform, false);
                socketObject.transform.localPosition = new Vector3(0f, 0.8f, 0f);
                socket = socketObject.transform;
            }

            ItemDefinition seedItem = AssetDatabase.LoadAssetAtPath<ItemDefinition>("Assets/_Project/ScriptableObjects/Items/Item_seed.asset");
            SerializedObject settings = new(container);
            settings.FindProperty("plantDefinition").objectReferenceValue = definition;
            settings.FindProperty("seedItem").objectReferenceValue = seedItem;
            settings.FindProperty("plantPrefab").objectReferenceValue = plantPrefab;
            settings.FindProperty("plantSocket").objectReferenceValue = socket;
            settings.FindProperty("currentPlant").objectReferenceValue = null;
            settings.FindProperty("outdoor").boolValue = false;
            settings.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(pot, PotPrefabPath);
            PrefabUtility.UnloadPrefabContents(pot);
        }

        private static void CreatePlantContextUI(PlayerInteractor interactor)
        {
            GameObject canvasObject = new("Plant Context UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 9;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            GameObject panel = new("Plant Details", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup), typeof(PlantContextHUD));
            panel.transform.SetParent(canvasObject.transform, false);
            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 0.5f);
            rect.anchorMax = new Vector2(1f, 0.5f);
            rect.pivot = new Vector2(1f, 0.5f);
            rect.anchoredPosition = new Vector2(-28f, 30f);
            rect.sizeDelta = new Vector2(390f, 190f);
            panel.GetComponent<Image>().color = new Color(0.025f, 0.05f, 0.03f, 0.9f);

            GameObject textObject = new("Details", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            textObject.transform.SetParent(panel.transform, false);
            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(16f, 14f);
            textRect.offsetMax = new Vector2(-16f, -14f);
            Text text = textObject.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 22;
            text.alignment = TextAnchor.UpperLeft;
            text.color = Color.white;

            PlantContextHUD hud = panel.GetComponent<PlantContextHUD>();
            SerializedObject settings = new(hud);
            settings.FindProperty("interactor").objectReferenceValue = interactor;
            settings.FindProperty("panelGroup").objectReferenceValue = panel.GetComponent<CanvasGroup>();
            settings.FindProperty("detailsText").objectReferenceValue = text;
            settings.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Material CreateMaterial()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            Material material = AssetDatabase.LoadAssetAtPath<Material>(PlantMaterialPath);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, PlantMaterialPath);
            }
            material.shader = shader;
            material.SetColor("_BaseColor", new Color(0.2f, 0.58f, 0.24f));
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
