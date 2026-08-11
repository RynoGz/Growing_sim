using Growveld.Economy;
using Growveld.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Growveld.Editor
{
    public static class Phase20UtilitiesSetup
    {
        private const string ScenePath = "Assets/_Project/Scenes/PrototypeFarm.unity";
        private const string UtilitySettingsPath = "Assets/_Project/ScriptableObjects/Economy/UtilitySettings.asset";

        [MenuItem("Growveld/Phase 20/Rebuild Utilities")]
        public static void ConfigureUtilities()
        {
            UtilitySettings settings = AssetDatabase.LoadAssetAtPath<UtilitySettings>(UtilitySettingsPath);
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<UtilitySettings>();
                AssetDatabase.CreateAsset(settings, UtilitySettingsPath);
            }
            SerializedObject utilityRates = new(settings);
            utilityRates.FindProperty("electricityRandPerKilowattHour").floatValue = 3.25f;
            utilityRates.FindProperty("waterLitresPerWatering").floatValue = 15f;
            utilityRates.FindProperty("waterRandPerLitre").floatValue = 0.06f;
            utilityRates.FindProperty("fallbackDayRealSeconds").floatValue = 1800f;
            utilityRates.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(settings);

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject systems = FindRoot(scene, "Game Systems");
            UtilityManager utilities = systems.GetComponent<UtilityManager>() ?? systems.AddComponent<UtilityManager>();
            SerializedObject utilityConfiguration = new(utilities);
            utilityConfiguration.FindProperty("economy").objectReferenceValue = systems.GetComponent<EconomyManager>();
            utilityConfiguration.FindProperty("settings").objectReferenceValue = settings;
            utilityConfiguration.FindProperty("currentElectricityKilowattHours").floatValue = 0f;
            utilityConfiguration.FindProperty("currentWaterLitres").floatValue = 0f;
            utilityConfiguration.FindProperty("fallbackDayElapsedSeconds").floatValue = 0f;
            utilityConfiguration.FindProperty("currentDay").intValue = 1;
            utilityConfiguration.FindProperty("externalDayClockEnabled").boolValue = false;
            utilityConfiguration.ApplyModifiedPropertiesWithoutUndo();

            ConfigureUtilitiesTablet(scene, utilities);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = systems;
            Debug.Log("Growveld Phase 20 setup complete: active-light electricity, watering usage, daily bills, negative balance, and purchasing restrictions created.");
        }

        private static void ConfigureUtilitiesTablet(Scene scene, UtilityManager utilities)
        {
            GameObject tabletCanvas = FindRoot(scene, "Business Tablet UI");
            Transform section = tabletCanvas != null ? tabletCanvas.transform.Find("Tablet/Content/Utilities") : null;
            if (section == null) return;
            for (int index = section.childCount - 1; index >= 0; index--) Object.DestroyImmediate(section.GetChild(index).gameObject);

            UtilitiesUI ui = section.GetComponent<UtilitiesUI>() ?? section.gameObject.AddComponent<UtilitiesUI>();
            GameObject textObject = new("Utility Summary", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            textObject.transform.SetParent(section, false);
            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(18f, 18f);
            rect.offsetMax = new Vector2(-18f, -18f);
            Text text = textObject.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 25;
            text.alignment = TextAnchor.UpperLeft;
            text.color = Color.white;

            SerializedObject uiSettings = new(ui);
            uiSettings.FindProperty("utilities").objectReferenceValue = utilities;
            uiSettings.FindProperty("utilityText").objectReferenceValue = text;
            uiSettings.ApplyModifiedPropertiesWithoutUndo();
        }

        private static GameObject FindRoot(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects()) if (root.name == name) return root;
            return null;
        }
    }
}
