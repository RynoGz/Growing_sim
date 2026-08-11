using Growveld.Core;
using Growveld.Economy;
using Growveld.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Growveld.Editor
{
    public static class Phase21TimeSetup
    {
        private const string ScenePath = "Assets/_Project/Scenes/PrototypeFarm.unity";
        private const string TimeSettingsPath = "Assets/_Project/ScriptableObjects/TimeSettings.asset";

        [MenuItem("Growveld/Phase 21/Rebuild Time System")]
        public static void ConfigureTime()
        {
            TimeSettings settings = AssetDatabase.LoadAssetAtPath<TimeSettings>(TimeSettingsPath);
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<TimeSettings>();
                AssetDatabase.CreateAsset(settings, TimeSettingsPath);
            }
            SerializedObject timeRates = new(settings);
            timeRates.FindProperty("realSecondsPerFullDay").floatValue = 1800f;
            timeRates.FindProperty("sunriseHour").floatValue = 6f;
            timeRates.FindProperty("sunsetHour").floatValue = 22f;
            timeRates.FindProperty("growLightOnHour").floatValue = 4f;
            timeRates.FindProperty("growLightOffHour").floatValue = 22f;
            timeRates.FindProperty("daytimeSunIntensity").floatValue = 1.15f;
            timeRates.FindProperty("nightSunIntensity").floatValue = 0.03f;
            timeRates.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(settings);

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject systems = FindRoot(scene, "Game Systems");
            GameTimeManager gameTime = systems.GetComponent<GameTimeManager>() ?? systems.AddComponent<GameTimeManager>();
            Light sun = FindSun(scene);
            UtilityManager utilities = systems.GetComponent<UtilityManager>();
            SerializedObject clockSettings = new(gameTime);
            clockSettings.FindProperty("settings").objectReferenceValue = settings;
            clockSettings.FindProperty("sun").objectReferenceValue = sun;
            clockSettings.FindProperty("utilities").objectReferenceValue = utilities;
            clockSettings.FindProperty("day").intValue = 1;
            clockSettings.FindProperty("timeOfDayHours").floatValue = 7f;
            clockSettings.ApplyModifiedPropertiesWithoutUndo();

            DeliveryManager delivery = systems.GetComponent<DeliveryManager>();
            if (delivery != null)
            {
                SerializedObject deliverySettings = new(delivery);
                deliverySettings.FindProperty("gameTime").objectReferenceValue = gameTime;
                deliverySettings.ApplyModifiedPropertiesWithoutUndo();
            }

            ConfigureTimeHUD(scene, gameTime);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = systems;
            Debug.Log("Growveld Phase 21 setup complete: 30-minute day, sun cycle, HUD clock, daylight growth, scheduled lights, delivery time, and day-end bills synchronised.");
        }

        private static Light FindSun(Scene scene)
        {
            GameObject environment = FindRoot(scene, "Environment");
            Transform sunTransform = environment != null ? environment.transform.Find("Sun") : null;
            Light sun = sunTransform != null ? sunTransform.GetComponent<Light>() : null;
            if (sun == null) sun = Object.FindFirstObjectByType<Light>();
            return sun;
        }

        private static void ConfigureTimeHUD(Scene scene, GameTimeManager gameTime)
        {
            GameObject economyUI = FindRoot(scene, "Economy UI");
            if (economyUI == null) return;
            Transform old = economyUI.transform.Find("Time and Day");
            if (old != null) Object.DestroyImmediate(old.gameObject);

            GameObject textObject = new("Time and Day", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text), typeof(TimeHUD));
            textObject.transform.SetParent(economyUI.transform, false);
            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -24f);
            rect.sizeDelta = new Vector2(540f, 52f);
            Text text = textObject.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 25;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;

            TimeHUD hud = textObject.GetComponent<TimeHUD>();
            SerializedObject hudSettings = new(hud);
            hudSettings.FindProperty("gameTime").objectReferenceValue = gameTime;
            hudSettings.FindProperty("timeText").objectReferenceValue = text;
            hudSettings.ApplyModifiedPropertiesWithoutUndo();
        }

        private static GameObject FindRoot(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects()) if (root.name == name) return root;
            return null;
        }
    }
}
