using Growveld.Building;
using Growveld.Environment;
using Growveld.Farming;
using Growveld.Inventory;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Growveld.Editor
{
    public static class Phase14OutdoorGrowingSetup
    {
        private const string ScenePath = "Assets/_Project/Scenes/PrototypeFarm.unity";
        private const string SoilMaterialPath = "Assets/_Project/Materials/M_PlantingSoil.mat";

        [MenuItem("Growveld/Phase 14/Rebuild Outdoor Growing")]
        public static void ConfigureOutdoorGrowing()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject systems = FindRoot(scene, "Game Systems");
            LandManager landManager = systems != null ? systems.GetComponent<LandManager>() : null;
            if (systems == null || landManager == null) throw new MissingReferenceException("Game Systems and LandManager are required.");

            OutdoorEnvironment outdoor = systems.GetComponent<OutdoorEnvironment>() ?? systems.AddComponent<OutdoorEnvironment>();
            SerializedObject environmentSettings = new(outdoor);
            environmentSettings.FindProperty("globalHumidity").floatValue = 44f;
            environmentSettings.FindProperty("idealMinimumHumidity").floatValue = 35f;
            environmentSettings.FindProperty("idealMaximumHumidity").floatValue = 60f;
            environmentSettings.FindProperty("outdoorGrowthRate").floatValue = 0.78f;
            environmentSettings.FindProperty("fallbackDayNightCycleRealSeconds").floatValue = 1800f;
            environmentSettings.FindProperty("fallbackDaylightRealSeconds").floatValue = 1200f;
            environmentSettings.ApplyModifiedPropertiesWithoutUndo();

            RemoveRoot(scene, "Outdoor Soil Beds");
            GameObject beds = new("Outdoor Soil Beds");
            Material soilMaterial = CreateSoilMaterial();
            CreateSoilBed(beds.transform, "Starter Soil Bed", new Vector3(5f, 0.12f, -6f), landManager, soilMaterial);
            CreateSoilBed(beds.transform, "North Plot Soil Bed", new Vector3(0f, 0.12f, 21f), landManager, soilMaterial);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = beds;
            Debug.Log("Growveld Phase 14 setup complete: daylight outdoor growth, global humidity, outdoor pot support, and owned-land soil beds created.");
        }

        private static void CreateSoilBed(Transform parent, string name, Vector3 position, LandManager landManager, Material material)
        {
            GameObject bed = new(name);
            bed.transform.SetParent(parent, false);
            bed.transform.position = position;

            GameObject soil = GameObject.CreatePrimitive(PrimitiveType.Cube);
            soil.name = "Designated Soil";
            soil.transform.SetParent(bed.transform, false);
            soil.transform.localPosition = Vector3.zero;
            soil.transform.localScale = new Vector3(5.5f, 0.22f, 3.5f);
            soil.GetComponent<MeshRenderer>().sharedMaterial = material;

            Vector3[] offsets =
            {
                new(-1.7f, 0.2f, -0.85f),
                new(1.7f, 0.2f, -0.85f),
                new(-1.7f, 0.2f, 0.85f),
                new(1.7f, 0.2f, 0.85f)
            };

            for (int index = 0; index < offsets.Length; index++)
            {
                CreateSoilSpot(bed.transform, index + 1, offsets[index], landManager, material);
            }
        }

        private static void CreateSoilSpot(Transform parent, int index, Vector3 localPosition, LandManager landManager, Material material)
        {
            GameObject spot = new($"Soil Planting Spot {index}");
            spot.name = $"Soil Planting Spot {index}";
            spot.transform.SetParent(parent, false);
            spot.transform.localPosition = localPosition;

            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            marker.name = "Planting Marker";
            marker.transform.SetParent(spot.transform, false);
            marker.transform.localPosition = Vector3.zero;
            marker.transform.localScale = new Vector3(0.65f, 0.08f, 0.65f);
            marker.GetComponent<MeshRenderer>().sharedMaterial = material;

            GameObject socketObject = new("Plant Socket");
            socketObject.transform.SetParent(spot.transform, false);
            socketObject.transform.localPosition = new Vector3(0f, 0.25f, 0f);

            PlantingContainer container = spot.AddComponent<PlantingContainer>();
            PlantDefinition plantDefinition = AssetDatabase.LoadAssetAtPath<PlantDefinition>("Assets/_Project/ScriptableObjects/Plants/Plant_GenericCrop.asset");
            ItemDefinition seed = AssetDatabase.LoadAssetAtPath<ItemDefinition>("Assets/_Project/ScriptableObjects/Items/Item_seed.asset");
            GameObject plantPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Plants/Generic Plant.prefab");
            SerializedObject settings = new(container);
            settings.FindProperty("plantDefinition").objectReferenceValue = plantDefinition;
            settings.FindProperty("seedItem").objectReferenceValue = seed;
            settings.FindProperty("plantPrefab").objectReferenceValue = plantPrefab;
            settings.FindProperty("plantSocket").objectReferenceValue = socketObject.transform;
            settings.FindProperty("outdoor").boolValue = true;
            settings.FindProperty("requireOwnedLand").boolValue = true;
            settings.FindProperty("landManager").objectReferenceValue = landManager;
            settings.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Material CreateSoilMaterial()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            Material material = AssetDatabase.LoadAssetAtPath<Material>(SoilMaterialPath);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, SoilMaterialPath);
            }
            material.shader = shader;
            material.SetColor("_BaseColor", new Color(0.3f, 0.17f, 0.08f));
            material.SetFloat("_Smoothness", 0.05f);
            EditorUtility.SetDirty(material);
            return material;
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
