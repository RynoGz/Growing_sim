using System.Collections.Generic;
using Growveld.Building;
using Growveld.Economy;
using Growveld.Interaction;
using Growveld.Inventory;
using Growveld.Player;
using Growveld.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Growveld.Editor
{
    public static class Phase9ShopSetup
    {
        private const string ScenePath = "Assets/_Project/Scenes/PrototypeFarm.unity";
        private static readonly string[] SectionNames = { "Dashboard", "Shop", "Farm Stock", "Sell", "Finances", "Land", "Construction", "Utilities" };

        [MenuItem("Growveld/Phase 9/Rebuild Shop and Deliveries")]
        public static void ConfigureShop()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject player = FindRoot(scene, "Player");
            GameObject systems = FindRoot(scene, "Game Systems");
            if (player == null || systems == null) throw new MissingReferenceException("Player and Game Systems are required.");

            EconomyManager economy = systems.GetComponent<EconomyManager>();
            PlayerInventory inventory = player.GetComponent<PlayerInventory>();
            ItemDefinition[] items = LoadAllItems();

            DeliveryManager delivery = systems.GetComponent<DeliveryManager>() ?? systems.AddComponent<DeliveryManager>();
            SerializedObject deliverySettings = new(delivery);
            deliverySettings.FindProperty("destinationInventory").objectReferenceValue = inventory;
            deliverySettings.FindProperty("deliveryDelayGameMinutes").floatValue = 5f;
            deliverySettings.FindProperty("realSecondsPerGameMinute").floatValue = 1.25f;
            deliverySettings.ApplyModifiedPropertiesWithoutUndo();

            ShopManager shop = systems.GetComponent<ShopManager>() ?? systems.AddComponent<ShopManager>();
            SerializedObject shopSettings = new(shop);
            shopSettings.FindProperty("economy").objectReferenceValue = economy;
            shopSettings.FindProperty("deliveryManager").objectReferenceValue = delivery;
            SerializedProperty itemArray = shopSettings.FindProperty("availableItems");
            itemArray.arraySize = items.Length;
            for (int index = 0; index < items.Length; index++) itemArray.GetArrayElementAtIndex(index).objectReferenceValue = items[index];
            shopSettings.ApplyModifiedPropertiesWithoutUndo();

            inventory.ClearAll();
            EditorUtility.SetDirty(inventory);
            RemoveRoot(scene, "Quick Purchase Kiosk");
            RemoveRoot(scene, "Business Tablet UI");
            GameObject tabletPanel = CreateTabletUI(shop, delivery);

            BusinessTabletController tabletController = player.GetComponent<BusinessTabletController>() ?? player.AddComponent<BusinessTabletController>();
            Behaviour[] suspended =
            {
                player.GetComponent<FirstPersonController>(),
                player.GetComponent<PlayerInteractor>(),
                player.GetComponent<InventoryHotbarInput>(),
                player.GetComponent<PlacementController>()
            };
            SerializedObject tabletSettings = new(tabletController);
            tabletSettings.FindProperty("tabletRoot").objectReferenceValue = tabletPanel;
            SerializedProperty behaviours = tabletSettings.FindProperty("gameplayBehaviours");
            behaviours.arraySize = suspended.Length;
            for (int index = 0; index < suspended.Length; index++) behaviours.GetArrayElementAtIndex(index).objectReferenceValue = suspended[index];
            tabletSettings.FindProperty("placementController").objectReferenceValue = player.GetComponent<PlacementController>();
            tabletSettings.ApplyModifiedPropertiesWithoutUndo();

            AddDeliveryHUD(scene, delivery, shop);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = systems;
            Debug.Log("Growveld Phase 9 setup complete: business tablet, menu shop, payment, five-minute delivery queue, and notifications created.");
        }

        private static GameObject CreateTabletUI(ShopManager shop, DeliveryManager delivery)
        {
            GameObject canvasObject = new("Business Tablet UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 30;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            GameObject panel = new("Tablet", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(BusinessTabletUI));
            panel.transform.SetParent(canvasObject.transform, false);
            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(1320f, 820f);
            panel.GetComponent<Image>().color = new Color(0.025f, 0.045f, 0.03f, 0.98f);

            Text title = CreateText(panel.transform, "Section Title", 34, TextAnchor.MiddleLeft, new Vector2(260f, 744f), new Vector2(1020f, 58f));
            title.text = "Dashboard";

            GameObject contentRoot = new("Content", typeof(RectTransform));
            contentRoot.transform.SetParent(panel.transform, false);
            RectTransform contentRect = contentRoot.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 0f);
            contentRect.anchorMax = new Vector2(0f, 0f);
            contentRect.pivot = new Vector2(0f, 0f);
            contentRect.anchoredPosition = new Vector2(260f, 40f);
            contentRect.sizeDelta = new Vector2(1020f, 690f);

            GameObject[] sections = new GameObject[SectionNames.Length];
            for (int index = 0; index < sections.Length; index++)
            {
                sections[index] = new GameObject(SectionNames[index], typeof(RectTransform));
                sections[index].transform.SetParent(contentRoot.transform, false);
                RectTransform sectionRect = sections[index].GetComponent<RectTransform>();
                sectionRect.anchorMin = Vector2.zero;
                sectionRect.anchorMax = Vector2.one;
                sectionRect.offsetMin = Vector2.zero;
                sectionRect.offsetMax = Vector2.zero;
            }

            CreateSectionCopy(sections[0].transform, "Welcome to Growveld\n\nUse the Shop to order supplies. Deliveries arrive in about five in-game minutes and enter your inventory automatically.");
            CreateShopRows(sections[1].transform, shop);
            CreateSectionCopy(sections[2].transform, "Farm stock will show dried product by quality grade.");
            CreateSectionCopy(sections[3].transform, "Sell All Stock becomes available after product is stored.");
            CreateSectionCopy(sections[4].transform, "Transaction history and daily expense summaries appear here.");
            CreateSectionCopy(sections[5].transform, "Walk to an orange plot sign and press E to purchase that plot.");
            CreateSectionCopy(sections[6].transform, "Select placeable equipment in the hotbar, close the tablet, then press B.\nGreen preview = valid. Red preview = invalid.");
            CreateSectionCopy(sections[7].transform, "Electricity and water usage will be itemised here.");

            BusinessTabletUI tabletUI = panel.GetComponent<BusinessTabletUI>();
            SerializedObject uiSettings = new(tabletUI);
            uiSettings.FindProperty("sectionTitle").objectReferenceValue = title;
            SerializedProperty sectionArray = uiSettings.FindProperty("sections");
            sectionArray.arraySize = sections.Length;
            SerializedProperty nameArray = uiSettings.FindProperty("sectionNames");
            nameArray.arraySize = sections.Length;
            for (int index = 0; index < sections.Length; index++)
            {
                sectionArray.GetArrayElementAtIndex(index).objectReferenceValue = sections[index];
                nameArray.GetArrayElementAtIndex(index).stringValue = SectionNames[index];
            }
            uiSettings.ApplyModifiedPropertiesWithoutUndo();

            for (int index = 0; index < SectionNames.Length; index++)
            {
                CreateNavigationButton(panel.transform, tabletUI, index, SectionNames[index]);
            }

            panel.SetActive(false);
            return panel;
        }

        private static void CreateNavigationButton(Transform parent, BusinessTabletUI tabletUI, int index, string labelText)
        {
            GameObject buttonObject = new($"{labelText} Tab", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(TabletTabButton));
            buttonObject.transform.SetParent(parent, false);
            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(28f, -32f - index * 90f);
            rect.sizeDelta = new Vector2(205f, 68f);
            buttonObject.GetComponent<Image>().color = new Color(0.12f, 0.24f, 0.15f, 1f);

            Text label = CreateText(buttonObject.transform, "Label", 22, TextAnchor.MiddleCenter, Vector2.zero, Vector2.zero);
            Stretch(label.rectTransform, 6f);
            label.text = labelText;

            SerializedObject settings = new(buttonObject.GetComponent<TabletTabButton>());
            settings.FindProperty("tabletUI").objectReferenceValue = tabletUI;
            settings.FindProperty("sectionIndex").intValue = index;
            settings.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CreateShopRows(Transform parent, ShopManager shop)
        {
            ItemDefinition[] items = shop.AvailableItems;
            for (int index = 0; index < items.Length; index++)
            {
                GameObject row = new($"Buy {items[index].DisplayName}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(ShopItemButton));
                row.transform.SetParent(parent, false);
                RectTransform rect = row.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0f, 1f);
                int column = index / 4;
                int rowIndex = index % 4;
                rect.anchoredPosition = new Vector2(12f + column * 495f, -12f - rowIndex * 158f);
                rect.sizeDelta = new Vector2(465f, 132f);
                row.GetComponent<Image>().color = new Color(0.1f, 0.18f, 0.12f, 1f);

                Text label = CreateText(row.transform, "Label", 20, TextAnchor.MiddleLeft, Vector2.zero, Vector2.zero);
                Stretch(label.rectTransform, 14f);

                SerializedObject settings = new(row.GetComponent<ShopItemButton>());
                settings.FindProperty("shop").objectReferenceValue = shop;
                settings.FindProperty("item").objectReferenceValue = items[index];
                settings.FindProperty("label").objectReferenceValue = label;
                settings.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void CreateSectionCopy(Transform parent, string copy)
        {
            Text text = CreateText(parent, "Section Copy", 24, TextAnchor.UpperLeft, new Vector2(18f, 18f), new Vector2(950f, 630f));
            text.text = copy;
        }

        private static void AddDeliveryHUD(Scene scene, DeliveryManager delivery, ShopManager shop)
        {
            GameObject economyUI = FindRoot(scene, "Economy UI");
            if (economyUI == null) return;
            DeliveryHUD hud = economyUI.GetComponent<DeliveryHUD>() ?? economyUI.AddComponent<DeliveryHUD>();

            Transform oldStatus = economyUI.transform.Find("Delivery Status");
            if (oldStatus != null) Object.DestroyImmediate(oldStatus.gameObject);
            Transform oldNotification = economyUI.transform.Find("Order Notification");
            if (oldNotification != null) Object.DestroyImmediate(oldNotification.gameObject);

            Text status = CreateText(economyUI.transform, "Delivery Status", 18, TextAnchor.UpperLeft, new Vector2(28f, -160f), new Vector2(430f, 130f));
            RectTransform statusRect = status.rectTransform;
            statusRect.anchorMin = new Vector2(0f, 1f);
            statusRect.anchorMax = new Vector2(0f, 1f);
            statusRect.pivot = new Vector2(0f, 1f);

            Text notification = CreateText(economyUI.transform, "Order Notification", 22, TextAnchor.MiddleCenter, new Vector2(0f, -34f), new Vector2(700f, 76f));
            RectTransform notificationRect = notification.rectTransform;
            notificationRect.anchorMin = new Vector2(0.5f, 1f);
            notificationRect.anchorMax = new Vector2(0.5f, 1f);
            notificationRect.pivot = new Vector2(0.5f, 1f);
            notification.gameObject.SetActive(false);

            SerializedObject settings = new(hud);
            settings.FindProperty("deliveryManager").objectReferenceValue = delivery;
            settings.FindProperty("shopManager").objectReferenceValue = shop;
            settings.FindProperty("deliveryStatusText").objectReferenceValue = status;
            settings.FindProperty("notificationText").objectReferenceValue = notification;
            settings.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Text CreateText(Transform parent, string name, int fontSize, TextAnchor alignment, Vector2 position, Vector2 size)
        {
            GameObject textObject = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            textObject.transform.SetParent(parent, false);
            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(0f, 0f);
            rect.pivot = new Vector2(0f, 0f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            Text text = textObject.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = Color.white;
            return text;
        }

        private static void Stretch(RectTransform rect, float padding)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.one * padding;
            rect.offsetMax = Vector2.one * -padding;
        }

        private static ItemDefinition[] LoadAllItems()
        {
            string[] guids = AssetDatabase.FindAssets("t:ItemDefinition", new[] { "Assets/_Project/ScriptableObjects/Items" });
            List<ItemDefinition> items = new();
            foreach (string guid in guids)
            {
                ItemDefinition item = AssetDatabase.LoadAssetAtPath<ItemDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                if (item != null) items.Add(item);
            }
            items.Sort((left, right) => string.CompareOrdinal(left.DisplayName, right.DisplayName));
            return items.ToArray();
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
