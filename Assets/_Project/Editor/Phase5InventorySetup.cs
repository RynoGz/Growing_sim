using System.Collections.Generic;
using System.IO;
using Growveld.Inventory;
using Growveld.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Growveld.Editor
{
    public static class Phase5InventorySetup
    {
        private const string ScenePath = "Assets/_Project/Scenes/PrototypeFarm.unity";
        private const string ItemFolder = "Assets/_Project/ScriptableObjects/Items";
        private const string PickupMaterialPath = "Assets/_Project/Materials/M_InventoryPickup.mat";

        [MenuItem("Growveld/Phase 5/Rebuild Inventory")]
        public static void ConfigureInventory()
        {
            EnsureFolder("Assets/_Project/ScriptableObjects");
            EnsureFolder(ItemFolder);

            Dictionary<string, ItemDefinition> items = new()
            {
                ["seed"] = CreateItem("seed", "Generic Seeds", "One generic crop seed.", ItemCategory.Seeds, true, 20, 120f, new Color(0.46f, 0.72f, 0.28f)),
                ["nutrients"] = CreateItem("nutrients", "Generic Nutrients", "A single balanced nutrient dose.", ItemCategory.Nutrients, true, 20, 90f, new Color(0.55f, 0.35f, 0.76f)),
                ["grow_pot"] = CreateItem("grow_pot", "Grow Pot", "A placeable pot for one plant.", ItemCategory.Equipment, false, 1, 650f, new Color(0.35f, 0.24f, 0.16f)),
                ["grow_light"] = CreateItem("grow_light", "Grow Light", "Indoor light with a visible coverage radius.", ItemCategory.Equipment, false, 1, 3200f, new Color(0.95f, 0.82f, 0.35f)),
                ["watering_can"] = CreateItem("watering_can", "Watering Can", "Reusable tool for watering plants.", ItemCategory.Watering, false, 1, 450f, new Color(0.2f, 0.58f, 0.8f)),
                ["drying_rack"] = CreateItem("drying_rack", "Drying Rack", "Dries several fresh harvest batches.", ItemCategory.Drying, false, 1, 4800f, new Color(0.55f, 0.38f, 0.22f)),
                ["storage_bin"] = CreateItem("storage_bin", "Storage Bin", "Stores dried product as farm stock.", ItemCategory.Storage, false, 1, 2200f, new Color(0.25f, 0.52f, 0.42f)),
                ["grow_room"] = CreateItem("grow_room", "Prefab Grow Room", "A complete small indoor growing building.", ItemCategory.Building, false, 1, 18000f, new Color(0.64f, 0.68f, 0.7f))
            };

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject player = FindRoot(scene, "Player");
            if (player == null)
            {
                throw new MissingReferenceException("Player was not found in PrototypeFarm.");
            }

            PlayerInventory inventory = player.GetComponent<PlayerInventory>() ?? player.AddComponent<PlayerInventory>();
            InventoryHotbarInput hotbarInput = player.GetComponent<InventoryHotbarInput>() ?? player.AddComponent<InventoryHotbarInput>();

            SerializedObject inventorySettings = new(inventory);
            inventorySettings.FindProperty("capacity").intValue = 8;
            inventorySettings.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject inputSettings = new(hotbarInput);
            inputSettings.FindProperty("visibleSlotCount").intValue = 5;
            inputSettings.ApplyModifiedPropertiesWithoutUndo();

            RemoveRoot(scene, "Inventory UI");
            RemoveRoot(scene, "Seed Pickup");
            RemoveRoot(scene, "Nutrient Pickup");
            CreateInventoryUI(inventory);

            Material pickupMaterial = CreateMaterial();
            CreatePickup("Seed Pickup", new Vector3(-1.1f, 0.35f, -2.5f), items["seed"], 5, pickupMaterial);
            CreatePickup("Nutrient Pickup", new Vector3(1.1f, 0.35f, -2.5f), items["nutrients"], 3, pickupMaterial);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = player;
            Debug.Log("Growveld Phase 5 setup complete: item definitions, eight-slot inventory, hotbar, and test pickups created.");
        }

        private static ItemDefinition CreateItem(
            string id,
            string displayName,
            string description,
            ItemCategory category,
            bool stackable,
            int maximumStack,
            float price,
            Color color)
        {
            string path = $"{ItemFolder}/Item_{id}.asset";
            ItemDefinition item = AssetDatabase.LoadAssetAtPath<ItemDefinition>(path);
            if (item == null)
            {
                item = ScriptableObject.CreateInstance<ItemDefinition>();
                AssetDatabase.CreateAsset(item, path);
            }

            SerializedObject settings = new(item);
            settings.FindProperty("itemId").stringValue = id;
            settings.FindProperty("displayName").stringValue = displayName;
            settings.FindProperty("description").stringValue = description;
            settings.FindProperty("category").enumValueIndex = (int)category;
            settings.FindProperty("stackable").boolValue = stackable;
            settings.FindProperty("maximumStack").intValue = maximumStack;
            settings.FindProperty("purchasePrice").floatValue = price;
            settings.FindProperty("displayColor").colorValue = color;
            settings.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(item);
            return item;
        }

        private static void CreateInventoryUI(PlayerInventory inventory)
        {
            GameObject canvasObject = new("Inventory UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 6;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            GameObject panelObject = new("Hotbar", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(InventoryHotbarUI));
            panelObject.transform.SetParent(canvasObject.transform, false);
            RectTransform panelRect = panelObject.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0f);
            panelRect.anchorMax = new Vector2(0.5f, 0f);
            panelRect.pivot = new Vector2(0.5f, 0f);
            panelRect.anchoredPosition = new Vector2(0f, 12f);
            panelRect.sizeDelta = new Vector2(710f, 104f);
            panelObject.GetComponent<Image>().color = new Color(0.025f, 0.04f, 0.03f, 0.88f);

            Text[] labels = new Text[5];
            for (int index = 0; index < labels.Length; index++)
            {
                GameObject slot = new($"Slot {index + 1}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                slot.transform.SetParent(panelObject.transform, false);
                RectTransform slotRect = slot.GetComponent<RectTransform>();
                slotRect.anchorMin = new Vector2(0f, 0f);
                slotRect.anchorMax = new Vector2(0f, 0f);
                slotRect.pivot = new Vector2(0f, 0f);
                slotRect.anchoredPosition = new Vector2(8f + index * 112f, 8f);
                slotRect.sizeDelta = new Vector2(104f, 88f);
                slot.GetComponent<Image>().color = new Color(0.12f, 0.16f, 0.12f, 0.95f);

                labels[index] = CreateText(slot.transform, "Label", 16, TextAnchor.MiddleCenter);
            }

            Text heldLabel = CreateText(panelObject.transform, "Selected Item", 18, TextAnchor.MiddleLeft);
            RectTransform heldRect = heldLabel.rectTransform;
            heldRect.anchorMin = new Vector2(0f, 0f);
            heldRect.anchorMax = new Vector2(0f, 0f);
            heldRect.pivot = new Vector2(0f, 0f);
            heldRect.anchoredPosition = new Vector2(578f, 8f);
            heldRect.sizeDelta = new Vector2(125f, 88f);

            InventoryHotbarUI hotbarUI = panelObject.GetComponent<InventoryHotbarUI>();
            SerializedObject uiSettings = new(hotbarUI);
            uiSettings.FindProperty("inventory").objectReferenceValue = inventory;
            SerializedProperty labelArray = uiSettings.FindProperty("slotLabels");
            labelArray.arraySize = labels.Length;
            for (int index = 0; index < labels.Length; index++)
            {
                labelArray.GetArrayElementAtIndex(index).objectReferenceValue = labels[index];
            }
            uiSettings.FindProperty("heldItemLabel").objectReferenceValue = heldLabel;
            uiSettings.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Text CreateText(Transform parent, string name, int fontSize, TextAnchor alignment)
        {
            GameObject textObject = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            textObject.transform.SetParent(parent, false);
            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(5f, 5f);
            rect.offsetMax = new Vector2(-5f, -5f);
            Text text = textObject.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = Color.white;
            return text;
        }

        private static void CreatePickup(string name, Vector3 position, ItemDefinition item, int quantity, Material material)
        {
            GameObject pickup = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pickup.name = name;
            pickup.transform.position = position;
            pickup.transform.localScale = Vector3.one * 0.55f;
            pickup.GetComponent<MeshRenderer>().sharedMaterial = material;

            InventoryPickup component = pickup.AddComponent<InventoryPickup>();
            SerializedObject settings = new(component);
            settings.FindProperty("item").objectReferenceValue = item;
            settings.FindProperty("quantity").intValue = quantity;
            settings.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Material CreateMaterial()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            Material material = AssetDatabase.LoadAssetAtPath<Material>(PickupMaterialPath);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, PickupMaterialPath);
            }

            material.shader = shader;
            material.SetColor("_BaseColor", new Color(0.42f, 0.75f, 0.34f));
            material.SetColor("_EmissionColor", new Color(0.04f, 0.14f, 0.03f));
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            string name = Path.GetFileName(path);
            AssetDatabase.CreateFolder(parent, name);
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
