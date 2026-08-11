using Growveld.Environment;
using Growveld.Farming;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Growveld.Editor
{
    public static class Phase12IndoorEnvironmentSetup
    {
        private const string ScenePath = "Assets/_Project/Scenes/PrototypeFarm.unity";
        private const string GrowRoomPrefabPath = "Assets/_Project/Prefabs/Equipment/Prefab Grow Room.prefab";
        private const string PlantPrefabPath = "Assets/_Project/Prefabs/Plants/Generic Plant.prefab";
        private const string SensorMaterialPath = "Assets/_Project/Materials/M_HumiditySensor.mat";

        [MenuItem("Growveld/Phase 12/Rebuild Indoor Environment")]
        public static void ConfigureIndoorEnvironment()
        {
            ConfigurePlantPrefab();
            GameObject growRoomPrefab = ConfigureGrowRoomPrefab();

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            RemoveRoot(scene, "Indoor Environment Test Room");
            GameObject room = (GameObject)PrefabUtility.InstantiatePrefab(growRoomPrefab, scene);
            room.name = "Indoor Environment Test Room";
            room.transform.position = new Vector3(4f, 0.1f, 5f);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = room;
            Debug.Log("Growveld Phase 12 setup complete: per-room humidity, automatic indoor plant detection, growth modifiers, and humidity sensor created.");
        }

        private static void ConfigurePlantPrefab()
        {
            GameObject plantPrefab = PrefabUtility.LoadPrefabContents(PlantPrefabPath);
            if (plantPrefab.GetComponent<PlantEnvironmentController>() == null)
            {
                plantPrefab.AddComponent<PlantEnvironmentController>();
            }
            PrefabUtility.SaveAsPrefabAsset(plantPrefab, PlantPrefabPath);
            PrefabUtility.UnloadPrefabContents(plantPrefab);
        }

        private static GameObject ConfigureGrowRoomPrefab()
        {
            GameObject roomPrefab = PrefabUtility.LoadPrefabContents(GrowRoomPrefabPath);
            GrowRoomEnvironment environment = roomPrefab.GetComponent<GrowRoomEnvironment>() ?? roomPrefab.AddComponent<GrowRoomEnvironment>();
            BoxCollider volume = roomPrefab.GetComponent<BoxCollider>();
            volume.isTrigger = true;
            volume.center = new Vector3(0f, 1.6f, 0f);
            volume.size = new Vector3(7.6f, 3.1f, 7.6f);

            SerializedObject environmentSettings = new(environment);
            environmentSettings.FindProperty("roomId").stringValue = "small_grow_room";
            environmentSettings.FindProperty("displayName").stringValue = "Small Grow Room";
            environmentSettings.FindProperty("humidity").floatValue = 60f;
            environmentSettings.FindProperty("ambientHumidity").floatValue = 45f;
            environmentSettings.FindProperty("idealMinimumHumidity").floatValue = 55f;
            environmentSettings.FindProperty("idealMaximumHumidity").floatValue = 65f;
            environmentSettings.FindProperty("driftPerRealMinute").floatValue = 0.2f;
            environmentSettings.ApplyModifiedPropertiesWithoutUndo();

            Transform previousSensor = roomPrefab.transform.Find("Humidity Sensor");
            if (previousSensor != null) Object.DestroyImmediate(previousSensor.gameObject);
            Material sensorMaterial = CreateSensorMaterial();
            GameObject sensor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sensor.name = "Humidity Sensor";
            sensor.transform.SetParent(roomPrefab.transform, false);
            sensor.transform.localPosition = new Vector3(0f, 1.45f, 3.7f);
            sensor.transform.localScale = new Vector3(0.7f, 0.45f, 0.16f);
            sensor.GetComponent<MeshRenderer>().sharedMaterial = sensorMaterial;
            HumiditySensor humiditySensor = sensor.AddComponent<HumiditySensor>();
            SerializedObject sensorSettings = new(humiditySensor);
            sensorSettings.FindProperty("room").objectReferenceValue = environment;
            sensorSettings.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(roomPrefab, GrowRoomPrefabPath);
            PrefabUtility.UnloadPrefabContents(roomPrefab);
            return AssetDatabase.LoadAssetAtPath<GameObject>(GrowRoomPrefabPath);
        }

        private static Material CreateSensorMaterial()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            Material material = AssetDatabase.LoadAssetAtPath<Material>(SensorMaterialPath);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, SensorMaterialPath);
            }
            material.shader = shader;
            material.SetColor("_BaseColor", new Color(0.18f, 0.62f, 0.78f));
            material.SetColor("_EmissionColor", new Color(0.02f, 0.12f, 0.16f));
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
