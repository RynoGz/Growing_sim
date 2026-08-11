using Growveld.Economy;
using Growveld.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Growveld.Editor
{
    public static class Phase19SellingSetup
    {
        private const string ScenePath = "Assets/_Project/Scenes/PrototypeFarm.unity";
        private const string SellingSettingsPath = "Assets/_Project/ScriptableObjects/Economy/SellingSettings.asset";

        [MenuItem("Growveld/Phase 19/Rebuild Selling")]
        public static void ConfigureSelling()
        {
            EnsureEconomyFolder();
            SellingSettings sellingSettings = AssetDatabase.LoadAssetAtPath<SellingSettings>(SellingSettingsPath);
            if (sellingSettings == null)
            {
                sellingSettings = ScriptableObject.CreateInstance<SellingSettings>();
                AssetDatabase.CreateAsset(sellingSettings, SellingSettingsPath);
            }
            SerializedObject priceSettings = new(sellingSettings);
            priceSettings.FindProperty("basePricePerKilogram").floatValue = 1000f;
            priceSettings.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(sellingSettings);

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject systems = FindRoot(scene, "Game Systems");
            EconomyManager economy = systems.GetComponent<EconomyManager>();
            FarmStockManager stock = systems.GetComponent<FarmStockManager>();
            SellingManager selling = systems.GetComponent<SellingManager>() ?? systems.AddComponent<SellingManager>();
            SerializedObject sellingConfiguration = new(selling);
            sellingConfiguration.FindProperty("economy").objectReferenceValue = economy;
            sellingConfiguration.FindProperty("farmStock").objectReferenceValue = stock;
            sellingConfiguration.FindProperty("qualitySettings").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Growveld.Farming.QualitySettings>("Assets/_Project/ScriptableObjects/Plants/QualitySettings.asset");
            sellingConfiguration.FindProperty("sellingSettings").objectReferenceValue = sellingSettings;
            sellingConfiguration.ApplyModifiedPropertiesWithoutUndo();

            ConfigureSellTablet(scene, selling, stock);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = systems;
            Debug.Log("Growveld Phase 19 setup complete: quality-priced Sell All Stock flow, immediate payment, and detailed sale summary created.");
        }

        private static void ConfigureSellTablet(Scene scene, SellingManager selling, FarmStockManager stock)
        {
            GameObject tabletCanvas = FindRoot(scene, "Business Tablet UI");
            Transform sellSection = tabletCanvas != null ? tabletCanvas.transform.Find("Tablet/Content/Sell") : null;
            if (sellSection == null) return;
            for (int index = sellSection.childCount - 1; index >= 0; index--) Object.DestroyImmediate(sellSection.GetChild(index).gameObject);

            SellingUI ui = sellSection.GetComponent<SellingUI>() ?? sellSection.gameObject.AddComponent<SellingUI>();
            GameObject summaryObject = new("Sale Summary", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            summaryObject.transform.SetParent(sellSection, false);
            RectTransform summaryRect = summaryObject.GetComponent<RectTransform>();
            summaryRect.anchorMin = new Vector2(0f, 0f);
            summaryRect.anchorMax = new Vector2(1f, 1f);
            summaryRect.offsetMin = new Vector2(18f, 120f);
            summaryRect.offsetMax = new Vector2(-18f, -18f);
            Text summaryText = summaryObject.GetComponent<Text>();
            summaryText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            summaryText.fontSize = 22;
            summaryText.alignment = TextAnchor.UpperLeft;
            summaryText.color = Color.white;

            GameObject buttonObject = new("Sell All Stock", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(sellSection, false);
            RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0f, 0f);
            buttonRect.anchorMax = new Vector2(0f, 0f);
            buttonRect.pivot = new Vector2(0f, 0f);
            buttonRect.anchoredPosition = new Vector2(18f, 20f);
            buttonRect.sizeDelta = new Vector2(360f, 78f);
            buttonObject.GetComponent<Image>().color = new Color(0.16f, 0.54f, 0.24f, 1f);
            Text buttonLabel = CreateButtonLabel(buttonObject.transform, "SELL ALL STOCK");
            Button sellButton = buttonObject.GetComponent<Button>();

            SerializedObject uiSettings = new(ui);
            uiSettings.FindProperty("sellingManager").objectReferenceValue = selling;
            uiSettings.FindProperty("farmStock").objectReferenceValue = stock;
            uiSettings.FindProperty("summaryText").objectReferenceValue = summaryText;
            uiSettings.FindProperty("sellAllButton").objectReferenceValue = sellButton;
            uiSettings.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Text CreateButtonLabel(Transform parent, string copy)
        {
            GameObject labelObject = new("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            labelObject.transform.SetParent(parent, false);
            RectTransform rect = labelObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            Text text = labelObject.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 24;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.text = copy;
            return text;
        }

        private static void EnsureEconomyFolder()
        {
            if (!AssetDatabase.IsValidFolder("Assets/_Project/ScriptableObjects/Economy"))
            {
                AssetDatabase.CreateFolder("Assets/_Project/ScriptableObjects", "Economy");
            }
        }

        private static GameObject FindRoot(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects()) if (root.name == name) return root;
            return null;
        }
    }
}
