using Growveld.Environment;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Growveld.Editor
{
    public static class Phase13GrowLightSetup
    {
        private const string ScenePath = "Assets/_Project/Scenes/PrototypeFarm.unity";
        private const string GrowLightPrefabPath = "Assets/_Project/Prefabs/Equipment/Grow Light.prefab";

        [MenuItem("Growveld/Phase 13/Rebuild Grow Lights")]
        public static void ConfigureGrowLights()
        {
            GameObject lightPrefab = ConfigureGrowLightPrefab();
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            RemoveRoot(scene, "Indoor Test Grow Light");
            GameObject testLight = (GameObject)PrefabUtility.InstantiatePrefab(lightPrefab, scene);
            testLight.name = "Indoor Test Grow Light";
            testLight.transform.position = new Vector3(6.3f, 0.1f, 5f);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = testLight;
            Debug.Log("Growveld Phase 13 setup complete: grow-light coverage, automatic schedule, indoor growth requirement, and power rating created.");
        }

        private static GameObject ConfigureGrowLightPrefab()
        {
            GameObject prefab = PrefabUtility.LoadPrefabContents(GrowLightPrefabPath);
            GrowLight growLight = prefab.GetComponent<GrowLight>() ?? prefab.AddComponent<GrowLight>();

            Transform oldLight = prefab.transform.Find("Plant Light Source");
            if (oldLight != null) Object.DestroyImmediate(oldLight.gameObject);
            GameObject sourceObject = new("Plant Light Source", typeof(Light));
            sourceObject.transform.SetParent(prefab.transform, false);
            sourceObject.transform.localPosition = new Vector3(0f, 2.35f, 0f);
            sourceObject.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            Light source = sourceObject.GetComponent<Light>();
            source.type = LightType.Point;
            source.color = new Color(1f, 0.88f, 0.7f);
            source.intensity = 850f;
            source.range = 5f;
            source.shadows = LightShadows.None;

            Transform coverage = prefab.transform.Find("Coverage Preview");
            if (coverage != null)
            {
                coverage.localScale = new Vector3(3.5f, 0.025f, 3.5f);
                coverage.gameObject.SetActive(false);
            }

            SerializedObject settings = new(growLight);
            settings.FindProperty("coverageRadius").floatValue = 3.5f;
            settings.FindProperty("powerConsumptionKilowatts").floatValue = 1.2f;
            settings.FindProperty("lightSource").objectReferenceValue = source;
            settings.FindProperty("automaticSchedule").boolValue = true;
            settings.FindProperty("fallbackCycleRealSeconds").floatValue = 1800f;
            settings.FindProperty("fallbackActiveRealSeconds").floatValue = 1200f;
            settings.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(prefab, GrowLightPrefabPath);
            PrefabUtility.UnloadPrefabContents(prefab);
            return AssetDatabase.LoadAssetAtPath<GameObject>(GrowLightPrefabPath);
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
