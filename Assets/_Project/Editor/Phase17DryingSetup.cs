using Growveld.Farming;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Growveld.Editor
{
    public static class Phase17DryingSetup
    {
        private const string ScenePath = "Assets/_Project/Scenes/PrototypeFarm.unity";
        private const string RackPrefabPath = "Assets/_Project/Prefabs/Equipment/Drying Rack.prefab";
        private const string HarvestPrefabPath = "Assets/_Project/Prefabs/Harvest/Harvest Container.prefab";

        [MenuItem("Growveld/Phase 17/Rebuild Drying")]
        public static void ConfigureDrying()
        {
            GameObject rackPrefab = ConfigureRackPrefab();
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            RemoveRoot(scene, "Drying Test Rack");
            RemoveRoot(scene, "Fresh Batch Test");

            GameObject rack = (GameObject)PrefabUtility.InstantiatePrefab(rackPrefab, scene);
            rack.name = "Drying Test Rack";
            rack.transform.position = new Vector3(-5.5f, 0.1f, 6.7f);

            GameObject harvestPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(HarvestPrefabPath);
            GameObject batch = (GameObject)PrefabUtility.InstantiatePrefab(harvestPrefab, scene);
            batch.name = "Fresh Batch Test";
            batch.transform.position = new Vector3(-3.4f, 0.65f, 3.8f);
            batch.GetComponent<HarvestBatch>().Initialise(0.52f, QualityGrade.Premium, HarvestStatus.Fresh);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = rack;
            Debug.Log("Growveld Phase 17 setup complete: four-slot independent drying rack and physical Fresh-to-Dried transfer created.");
        }

        private static GameObject ConfigureRackPrefab()
        {
            GameObject rack = PrefabUtility.LoadPrefabContents(RackPrefabPath);
            DryingRack dryingRack = rack.GetComponent<DryingRack>() ?? rack.AddComponent<DryingRack>();

            Transform oldSlots = rack.transform.Find("Drying Slots");
            if (oldSlots != null) Object.DestroyImmediate(oldSlots.gameObject);
            GameObject slotsRoot = new("Drying Slots");
            slotsRoot.transform.SetParent(rack.transform, false);
            Transform[] anchors = new Transform[4];
            Vector3[] positions =
            {
                new(-0.72f, 0.58f, -0.42f),
                new(0.72f, 0.58f, -0.42f),
                new(-0.72f, 1.38f, -0.42f),
                new(0.72f, 1.38f, -0.42f)
            };
            for (int index = 0; index < anchors.Length; index++)
            {
                GameObject anchor = new($"Batch Slot {index + 1}");
                anchor.transform.SetParent(slotsRoot.transform, false);
                anchor.transform.localPosition = positions[index];
                anchors[index] = anchor.transform;
            }

            Transform oldOutput = rack.transform.Find("Batch Output");
            if (oldOutput != null) Object.DestroyImmediate(oldOutput.gameObject);
            GameObject output = new("Batch Output");
            output.transform.SetParent(rack.transform, false);
            output.transform.localPosition = new Vector3(0f, 0.55f, -1.45f);

            SerializedObject settings = new(dryingRack);
            settings.FindProperty("dryingDurationSeconds").floatValue = 600f;
            SerializedProperty anchorArray = settings.FindProperty("slotAnchors");
            anchorArray.arraySize = anchors.Length;
            for (int index = 0; index < anchors.Length; index++) anchorArray.GetArrayElementAtIndex(index).objectReferenceValue = anchors[index];
            settings.FindProperty("outputPoint").objectReferenceValue = output.transform;
            settings.FindProperty("slots").ClearArray();
            settings.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(rack, RackPrefabPath);
            PrefabUtility.UnloadPrefabContents(rack);
            return AssetDatabase.LoadAssetAtPath<GameObject>(RackPrefabPath);
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
