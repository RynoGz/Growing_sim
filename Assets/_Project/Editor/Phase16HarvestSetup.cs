using System.IO;
using Growveld.Carrying;
using Growveld.Farming;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Growveld.Editor
{
    public static class Phase16HarvestSetup
    {
        private const string ScenePath = "Assets/_Project/Scenes/PrototypeFarm.unity";
        private const string HarvestFolder = "Assets/_Project/Prefabs/Harvest";
        private const string HarvestPrefabPath = HarvestFolder + "/Harvest Container.prefab";
        private const string HarvestMaterialPath = "Assets/_Project/Materials/M_HarvestContainer.mat";
        private const string PlantPrefabPath = "Assets/_Project/Prefabs/Plants/Generic Plant.prefab";
        private const string PotPrefabPath = "Assets/_Project/Prefabs/Equipment/Grow Pot.prefab";

        [MenuItem("Growveld/Phase 16/Rebuild Harvesting")]
        public static void ConfigureHarvesting()
        {
            EnsureFolder(HarvestFolder);
            GameObject harvestPrefab = CreateHarvestPrefab();
            ConfigurePlantPrefab(harvestPrefab);

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            RemoveRoot(scene, "Harvest Test Pot");
            CreateHarvestReadyTestPlant(scene);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("Growveld Phase 16 setup complete: care-based harvesting and physical Fresh harvest containers created.");
        }

        private static GameObject CreateHarvestPrefab()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            Material material = AssetDatabase.LoadAssetAtPath<Material>(HarvestMaterialPath);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, HarvestMaterialPath);
            }
            material.shader = shader;
            material.SetColor("_BaseColor", new Color(0.28f, 0.56f, 0.22f));
            EditorUtility.SetDirty(material);

            GameObject container = GameObject.CreatePrimitive(PrimitiveType.Cube);
            container.name = "Harvest Container";
            container.transform.localScale = new Vector3(1.15f, 0.72f, 0.82f);
            container.GetComponent<MeshRenderer>().sharedMaterial = material;
            Rigidbody body = container.AddComponent<Rigidbody>();
            body.mass = 3f;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.Continuous;
            CarryableObject carryable = container.AddComponent<CarryableObject>();
            HarvestBatch batch = container.AddComponent<HarvestBatch>();

            SerializedObject carrySettings = new(carryable);
            carrySettings.FindProperty("displayName").stringValue = "harvest container";
            carrySettings.FindProperty("rigidbodyComponent").objectReferenceValue = body;
            carrySettings.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject batchSettings = new(batch);
            batchSettings.FindProperty("weightKilograms").floatValue = 0.45f;
            batchSettings.FindProperty("qualityGrade").enumValueIndex = (int)QualityGrade.Standard;
            batchSettings.FindProperty("status").enumValueIndex = (int)HarvestStatus.Fresh;
            batchSettings.FindProperty("qualitySettings").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Growveld.Farming.QualitySettings>("Assets/_Project/ScriptableObjects/Plants/QualitySettings.asset");
            batchSettings.ApplyModifiedPropertiesWithoutUndo();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(container, HarvestPrefabPath);
            Object.DestroyImmediate(container);
            return prefab;
        }

        private static void ConfigurePlantPrefab(GameObject harvestPrefab)
        {
            GameObject plantPrefab = PrefabUtility.LoadPrefabContents(PlantPrefabPath);
            PlantInstance plant = plantPrefab.GetComponent<PlantInstance>();
            SerializedObject settings = new(plant);
            settings.FindProperty("harvestBatchPrefab").objectReferenceValue = harvestPrefab;
            settings.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(plantPrefab, PlantPrefabPath);
            PrefabUtility.UnloadPrefabContents(plantPrefab);
        }

        private static void CreateHarvestReadyTestPlant(Scene scene)
        {
            GameObject potPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PotPrefabPath);
            GameObject plantPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlantPrefabPath);
            PlantDefinition definition = AssetDatabase.LoadAssetAtPath<PlantDefinition>("Assets/_Project/ScriptableObjects/Plants/Plant_GenericCrop.asset");
            GameObject pot = (GameObject)PrefabUtility.InstantiatePrefab(potPrefab, scene);
            pot.name = "Harvest Test Pot";
            pot.transform.position = new Vector3(3f, 0.1f, 5f);

            PlantingContainer container = pot.GetComponent<PlantingContainer>();
            Transform socket = pot.transform.Find("Plant Socket");
            GameObject plantObject = (GameObject)PrefabUtility.InstantiatePrefab(plantPrefab, scene);
            plantObject.name = "Harvest Ready Test Plant";
            plantObject.transform.SetParent(socket, false);
            plantObject.transform.localPosition = Vector3.zero;
            PlantInstance plant = plantObject.GetComponent<PlantInstance>();
            SerializedObject plantSettings = new(plant);
            plantSettings.FindProperty("elapsedGrowthSeconds").floatValue = definition.TotalGrowthSeconds;
            plantSettings.FindProperty("qualityScore").floatValue = 82f;
            plantSettings.FindProperty("yieldPotential").floatValue = 0.95f;
            plantSettings.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject containerSettings = new(container);
            containerSettings.FindProperty("currentPlant").objectReferenceValue = plant;
            containerSettings.ApplyModifiedPropertiesWithoutUndo();
            Selection.activeGameObject = plantObject;
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
