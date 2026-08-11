using System;
using System.Linq;
using Growveld.Building;
using Growveld.Core;
using Growveld.Economy;
using Growveld.Farming;
using Growveld.Inventory;
using Growveld.Saving;
using Growveld.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Growveld.Editor
{
    public static class Phase22SaveSetup
    {
        private const string ScenePath = "Assets/_Project/Scenes/PrototypeFarm.unity";
        private const string HarvestBatchPrefabPath = "Assets/_Project/Prefabs/Harvest/Harvest Container.prefab";

        [MenuItem("Growveld/Phase 22/Rebuild Saving")]
        public static void ConfigureSaving()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject player = FindRoot(scene, "Player");
            GameObject systems = FindRoot(scene, "Game Systems");
            if (player == null || systems == null) throw new MissingReferenceException("Player and Game Systems are required.");

            SaveSystem saveSystem = systems.GetComponent<SaveSystem>() ?? systems.AddComponent<SaveSystem>();
            ItemDefinition[] items = LoadAssets<ItemDefinition>("Assets/_Project/ScriptableObjects/Items");
            PlaceableDefinition[] placeables = LoadAssets<PlaceableDefinition>("Assets/_Project/ScriptableObjects/Placeables");

            SerializedObject settings = new(saveSystem);
            settings.FindProperty("player").objectReferenceValue = player.transform;
            settings.FindProperty("economy").objectReferenceValue = systems.GetComponent<EconomyManager>();
            settings.FindProperty("gameTime").objectReferenceValue = systems.GetComponent<GameTimeManager>();
            settings.FindProperty("landManager").objectReferenceValue = systems.GetComponent<LandManager>();
            settings.FindProperty("inventory").objectReferenceValue = player.GetComponent<PlayerInventory>();
            settings.FindProperty("deliveries").objectReferenceValue = systems.GetComponent<DeliveryManager>();
            settings.FindProperty("farmStock").objectReferenceValue = systems.GetComponent<FarmStockManager>();
            settings.FindProperty("utilities").objectReferenceValue = systems.GetComponent<UtilityManager>();
            SetObjectArray(settings.FindProperty("itemCatalog"), items);
            SetObjectArray(settings.FindProperty("placeableCatalog"), placeables);
            settings.FindProperty("harvestBatchPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(HarvestBatchPrefabPath);
            settings.FindProperty("autosaveIntervalSeconds").floatValue = 120f;
            settings.FindProperty("loadExistingSaveOnStart").boolValue = true;
            settings.ApplyModifiedPropertiesWithoutUndo();

            ConfigureDashboard(scene, saveSystem);

            EditorUtility.SetDirty(saveSystem);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = systems;
            Debug.Log($"Growveld Phase 22 setup complete: versioned JSON saving covers the full prototype state, F5/F9 manual controls, dashboard buttons, safe replacement writes, startup restore, and 120-second autosaves. Catalogues: {items.Length} items, {placeables.Length} placeables.");
        }

        [MenuItem("Growveld/Phase 22/Run Save-Load Smoke Test %#F8")]
        public static void RunSaveLoadSmokeTest()
        {
            if (!EditorApplication.isPlaying)
            {
                Debug.LogWarning("Enter Play Mode before running the Phase 22 save-load smoke test.");
                return;
            }

            SaveSystem saveSystem = UnityEngine.Object.FindFirstObjectByType<SaveSystem>();
            if (saveSystem == null) throw new MissingReferenceException("The active scene has no SaveSystem.");
            if (!saveSystem.SaveGame(false)) throw new InvalidOperationException("The smoke-test save failed.");
            saveSystem.LoadGame();
            Debug.Log($"Growveld Phase 22 smoke test passed: JSON written to {saveSystem.SavePath} and a full-state load was started successfully.");
        }

        private static void ConfigureDashboard(Scene scene, SaveSystem saveSystem)
        {
            GameObject tabletCanvas = FindRoot(scene, "Business Tablet UI");
            Transform dashboard = tabletCanvas != null ? tabletCanvas.transform.Find("Tablet/Content/Dashboard") : null;
            if (dashboard == null) throw new MissingReferenceException("Business tablet Dashboard section is required.");
            for (int index = dashboard.childCount - 1; index >= 0; index--) UnityEngine.Object.DestroyImmediate(dashboard.GetChild(index).gameObject);

            SaveLoadUI ui = dashboard.GetComponent<SaveLoadUI>() ?? dashboard.gameObject.AddComponent<SaveLoadUI>();
            Text heading = CreateText(dashboard, "Heading", "Farm Dashboard", 36, TextAnchor.UpperLeft, new Vector2(22f, -18f), new Vector2(940f, 56f));
            heading.fontStyle = FontStyle.Bold;
            Text instructions = CreateText(
                dashboard,
                "Instructions",
                "Your farm is autosaved every two minutes and when the game closes.\nManual shortcuts: F5 saves  |  F9 loads",
                23,
                TextAnchor.UpperLeft,
                new Vector2(22f, -96f),
                new Vector2(940f, 110f));
            instructions.color = new Color(0.82f, 0.9f, 0.83f);

            Button saveButton = CreateButton(dashboard, "Save Game", "SAVE GAME", new Vector2(22f, -245f), new Color(0.16f, 0.54f, 0.24f));
            Button loadButton = CreateButton(dashboard, "Load Game", "LOAD GAME", new Vector2(350f, -245f), new Color(0.18f, 0.38f, 0.55f));
            Text status = CreateText(dashboard, "Save Status", "F5 Save  |  F9 Load  |  Autosave every 2 minutes", 22, TextAnchor.UpperLeft, new Vector2(22f, -350f), new Vector2(940f, 90f));
            status.color = new Color(0.55f, 1f, 0.62f);

            SerializedObject uiSettings = new(ui);
            uiSettings.FindProperty("saveSystem").objectReferenceValue = saveSystem;
            uiSettings.FindProperty("statusText").objectReferenceValue = status;
            uiSettings.FindProperty("saveButton").objectReferenceValue = saveButton;
            uiSettings.FindProperty("loadButton").objectReferenceValue = loadButton;
            uiSettings.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Text CreateText(Transform parent, string name, string copy, int fontSize, TextAnchor alignment, Vector2 position, Vector2 size)
        {
            GameObject textObject = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            textObject.transform.SetParent(parent, false);
            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            Text text = textObject.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = Color.white;
            text.text = copy;
            return text;
        }

        private static Button CreateButton(Transform parent, string name, string labelCopy, Vector2 position, Color colour)
        {
            GameObject buttonObject = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(292f, 76f);
            buttonObject.GetComponent<Image>().color = colour;
            Text label = CreateText(buttonObject.transform, "Label", labelCopy, 23, TextAnchor.MiddleCenter, Vector2.zero, Vector2.zero);
            RectTransform labelRect = label.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.pivot = new Vector2(0.5f, 0.5f);
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            label.fontStyle = FontStyle.Bold;
            return buttonObject.GetComponent<Button>();
        }

        private static T[] LoadAssets<T>(string folder) where T : UnityEngine.Object
        {
            return AssetDatabase.FindAssets($"t:{typeof(T).Name}", new[] { folder })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<T>)
                .Where(asset => asset != null)
                .OrderBy(asset => asset.name, StringComparer.Ordinal)
                .ToArray();
        }

        private static void SetObjectArray<T>(SerializedProperty property, T[] values) where T : UnityEngine.Object
        {
            property.arraySize = values.Length;
            for (int index = 0; index < values.Length; index++) property.GetArrayElementAtIndex(index).objectReferenceValue = values[index];
        }

        private static GameObject FindRoot(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects()) if (root.name == name) return root;
            return null;
        }
    }
}
