using Growveld.Economy;
using Growveld.Farming;
using Growveld.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Growveld.Editor
{
    public static class Phase18StorageSetup
    {
        private const string ScenePath = "Assets/_Project/Scenes/PrototypeFarm.unity";
        private const string StoragePrefabPath = "Assets/_Project/Prefabs/Equipment/Storage Bin.prefab";
        private const string HarvestPrefabPath = "Assets/_Project/Prefabs/Harvest/Harvest Container.prefab";

        [MenuItem("Growveld/Phase 18/Rebuild Storage")]
        public static void ConfigureStorage()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject systems = FindRoot(scene, "Game Systems");
            FarmStockManager stock = systems.GetComponent<FarmStockManager>() ?? systems.AddComponent<FarmStockManager>();
            GameObject storagePrefab = ConfigureStoragePrefab();

            RemoveRoot(scene, "Storage Test Bin");
            RemoveRoot(scene, "Dried Batch Test");
            GameObject storage = (GameObject)PrefabUtility.InstantiatePrefab(storagePrefab, scene);
            storage.name = "Storage Test Bin";
            storage.transform.position = new Vector3(-7.4f, 0.1f, 4.9f);

            GameObject harvestPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(HarvestPrefabPath);
            GameObject driedBatch = (GameObject)PrefabUtility.InstantiatePrefab(harvestPrefab, scene);
            driedBatch.name = "Dried Batch Test";
            driedBatch.transform.position = new Vector3(-7.2f, 0.6f, 2.6f);
            driedBatch.GetComponent<HarvestBatch>().Initialise(0.48f, QualityGrade.TopGrade, HarvestStatus.Dried);

            ConfigureStockTablet(scene, stock);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = storage;
            Debug.Log("Growveld Phase 18 setup complete: physical Dried storage transfer and quality-separated farm stock created.");
        }

        private static GameObject ConfigureStoragePrefab()
        {
            GameObject storage = PrefabUtility.LoadPrefabContents(StoragePrefabPath);
            StorageContainer container = storage.GetComponent<StorageContainer>() ?? storage.AddComponent<StorageContainer>();
            SerializedObject settings = new(container);
            settings.FindProperty("farmStock").objectReferenceValue = null;
            settings.FindProperty("capacityKilograms").floatValue = 25f;
            settings.FindProperty("storedKilograms").floatValue = 0f;
            settings.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(storage, StoragePrefabPath);
            PrefabUtility.UnloadPrefabContents(storage);
            return AssetDatabase.LoadAssetAtPath<GameObject>(StoragePrefabPath);
        }

        private static void ConfigureStockTablet(Scene scene, FarmStockManager stock)
        {
            GameObject tabletCanvas = FindRoot(scene, "Business Tablet UI");
            Transform stockSection = tabletCanvas != null
                ? tabletCanvas.transform.Find("Tablet/Content/Farm Stock")
                : null;
            if (stockSection == null) return;

            for (int index = stockSection.childCount - 1; index >= 0; index--)
            {
                Object.DestroyImmediate(stockSection.GetChild(index).gameObject);
            }

            FarmStockUI stockUI = stockSection.GetComponent<FarmStockUI>() ?? stockSection.gameObject.AddComponent<FarmStockUI>();
            GameObject textObject = new("Stock Summary", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            textObject.transform.SetParent(stockSection, false);
            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(18f, 18f);
            rect.offsetMax = new Vector2(-18f, -18f);
            Text text = textObject.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 27;
            text.alignment = TextAnchor.UpperLeft;
            text.color = Color.white;

            SerializedObject settings = new(stockUI);
            settings.FindProperty("farmStock").objectReferenceValue = stock;
            settings.FindProperty("qualitySettings").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Growveld.Farming.QualitySettings>("Assets/_Project/ScriptableObjects/Plants/QualitySettings.asset");
            settings.FindProperty("stockText").objectReferenceValue = text;
            settings.ApplyModifiedPropertiesWithoutUndo();
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
