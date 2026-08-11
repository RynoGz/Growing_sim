using Growveld.Economy;
using Growveld.Inventory;
using Growveld.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Growveld.Editor
{
    public static class Phase6EconomySetup
    {
        private const string ScenePath = "Assets/_Project/Scenes/PrototypeFarm.unity";
        private const string WateringCanPath = "Assets/_Project/ScriptableObjects/Items/Item_watering_can.asset";
        private const string KioskMaterialPath = "Assets/_Project/Materials/M_PurchaseKiosk.mat";

        [MenuItem("Growveld/Phase 6/Rebuild Economy Foundation")]
        public static void ConfigureEconomy()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject systems = FindRoot(scene, "Game Systems");
            if (systems == null)
            {
                systems = new GameObject("Game Systems");
            }

            EconomyManager economy = systems.GetComponent<EconomyManager>() ?? systems.AddComponent<EconomyManager>();
            SerializedObject economySettings = new(economy);
            economySettings.FindProperty("startingBalance").floatValue = 30000f;
            economySettings.FindProperty("balance").floatValue = 0f;
            economySettings.FindProperty("initialiseOnAwake").boolValue = true;
            economySettings.ApplyModifiedPropertiesWithoutUndo();

            RemoveRoot(scene, "Economy UI");
            RemoveRoot(scene, "Quick Purchase Kiosk");
            CreateEconomyUI(economy);
            CreateKiosk(economy);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = systems;
            Debug.Log("Growveld Phase 6 setup complete: Rand balance, transaction API, purchase validation, and HUD created.");
        }

        private static void CreateEconomyUI(EconomyManager economy)
        {
            GameObject canvasObject = new("Economy UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(EconomyHUD));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 7;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            Text moneyText = CreateText(canvasObject.transform, "Money", 30, TextAnchor.MiddleLeft);
            RectTransform moneyRect = moneyText.rectTransform;
            moneyRect.anchorMin = new Vector2(0f, 1f);
            moneyRect.anchorMax = new Vector2(0f, 1f);
            moneyRect.pivot = new Vector2(0f, 1f);
            moneyRect.anchoredPosition = new Vector2(28f, -24f);
            moneyRect.sizeDelta = new Vector2(360f, 54f);

            Text transactionText = CreateText(canvasObject.transform, "Transaction Message", 22, TextAnchor.UpperLeft);
            RectTransform transactionRect = transactionText.rectTransform;
            transactionRect.anchorMin = new Vector2(0f, 1f);
            transactionRect.anchorMax = new Vector2(0f, 1f);
            transactionRect.pivot = new Vector2(0f, 1f);
            transactionRect.anchoredPosition = new Vector2(28f, -78f);
            transactionRect.sizeDelta = new Vector2(520f, 90f);

            EconomyHUD hud = canvasObject.GetComponent<EconomyHUD>();
            SerializedObject hudSettings = new(hud);
            hudSettings.FindProperty("economy").objectReferenceValue = economy;
            hudSettings.FindProperty("moneyText").objectReferenceValue = moneyText;
            hudSettings.FindProperty("transactionText").objectReferenceValue = transactionText;
            hudSettings.FindProperty("messageDuration").floatValue = 3f;
            hudSettings.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Text CreateText(Transform parent, string name, int fontSize, TextAnchor alignment)
        {
            GameObject textObject = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            textObject.transform.SetParent(parent, false);
            Text text = textObject.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = Color.white;
            text.text = name;
            return text;
        }

        private static void CreateKiosk(EconomyManager economy)
        {
            ItemDefinition wateringCan = AssetDatabase.LoadAssetAtPath<ItemDefinition>(WateringCanPath);
            if (wateringCan == null)
            {
                throw new MissingReferenceException("The watering can item definition was not found.");
            }

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            Material material = AssetDatabase.LoadAssetAtPath<Material>(KioskMaterialPath);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, KioskMaterialPath);
            }
            material.shader = shader;
            material.SetColor("_BaseColor", new Color(0.12f, 0.48f, 0.62f));
            EditorUtility.SetDirty(material);

            GameObject kiosk = GameObject.CreatePrimitive(PrimitiveType.Cube);
            kiosk.name = "Quick Purchase Kiosk";
            kiosk.transform.position = new Vector3(-3.5f, 1f, -1.5f);
            kiosk.transform.localScale = new Vector3(1.2f, 2f, 0.7f);
            kiosk.GetComponent<MeshRenderer>().sharedMaterial = material;

            QuickPurchaseInteractable purchase = kiosk.AddComponent<QuickPurchaseInteractable>();
            SerializedObject settings = new(purchase);
            settings.FindProperty("item").objectReferenceValue = wateringCan;
            settings.FindProperty("quantity").intValue = 1;
            settings.FindProperty("economy").objectReferenceValue = economy;
            settings.ApplyModifiedPropertiesWithoutUndo();
        }

        private static GameObject FindRoot(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == name) return root;
            }
            return null;
        }

        private static void RemoveRoot(Scene scene, string name)
        {
            GameObject root = FindRoot(scene, name);
            if (root != null) Object.DestroyImmediate(root);
        }
    }
}
