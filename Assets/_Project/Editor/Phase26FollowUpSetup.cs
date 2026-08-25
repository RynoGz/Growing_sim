using System;
using System.Collections.Generic;
using System.Linq;
using Growveld.Building;
using Growveld.Environment;
using Growveld.Interaction;
using Growveld.Inventory;
using Growveld.Player;
using Growveld.Saving;
using Growveld.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Growveld.Editor
{
    public static class Phase26FollowUpSetup
    {
        private const string ScenePath = "Assets/_Project/Scenes/PrototypeFarm.unity";
        private const string FloorLightPrefabPath = "Assets/_Project/Prefabs/Equipment/Grow Light.prefab";
        private const string CeilingLightPrefabPath = "Assets/_Project/Prefabs/Equipment/Ceiling Grow Light.prefab";

        private static readonly string[] TabletPageNames =
        {
            "Dashboard",
            "Shop",
            "Farm Stock",
            "Sell",
            "Finances",
            "Utilities",
            "Inventory",
            "Game Guide"
        };

        [MenuItem("Growveld/Phase 26/Apply Follow-Up UI and Lighting Fixes")]
        public static void ConfigureFollowUpFixes()
        {
            Phase25UxGameplaySetup.ConfigureUxGameplayFixes();

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject player = FindRoot(scene, "Player");
            GameObject tabletCanvas = FindRoot(scene, "Business Tablet UI");
            if (player == null || tabletCanvas == null) throw new MissingReferenceException("Player and Business Tablet UI are required.");

            RemoveRoot(scene, "Inventory UI");
            RemoveRoot(scene, "Placement UI");
            RemovePermanentHudPanels(scene);

            InventoryHotbarInput hotbarInput = player.GetComponent<InventoryHotbarInput>();
            if (hotbarInput != null) UnityEngine.Object.DestroyImmediate(hotbarInput);

            InteractionPromptUI prompt = ConfigureInteractionPrompt(player);
            TabletInventoryUI tabletInventory = ConfigureTablet(tabletCanvas, player);
            ConfigureInputSuspension(player, tabletInventory);
            RefreshPauseControls(scene);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            ValidateFollowUpFixes();
            Debug.Log($"Growveld Phase 26 setup complete: shared grow-light visuals ({GrowLight.SharedVisualIntensity:0} intensity, {GrowLight.SharedVisualRange:0.0} range), top-level tablet popups, Game Guide navigation, no hotbar/permanent control overlay, and one prioritized contextual prompt are configured.");
        }

        [MenuItem("Growveld/Phase 26/Validate Follow-Up UI and Lighting Fixes")]
        public static void ValidateFollowUpFixes()
        {
            List<string> failures = new();
            ValidateLightPrefab(FloorLightPrefabPath, "floor grow light", failures);
            ValidateLightPrefab(CeilingLightPrefabPath, "ceiling grow light", failures);

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject player = FindRoot(scene, "Player");
            GameObject tabletCanvas = FindRoot(scene, "Business Tablet UI");
            if (player == null) failures.Add("Player missing");
            if (tabletCanvas == null) failures.Add("Business Tablet UI missing");
            if (player != null && player.GetComponent<InventoryHotbarInput>() != null) failures.Add("hotbar input is still attached to the player");
            if (FindRoot(scene, "Inventory UI") != null || UnityEngine.Object.FindFirstObjectByType<InventoryHotbarUI>(FindObjectsInactive.Include) != null) failures.Add("hotbar UI remains in the scene");
            if (FindRoot(scene, "Placement UI") != null) failures.Add("legacy placement instructions remain in the scene");

            Transform polishHud = FindRoot(scene, "Polish HUD")?.transform;
            if (polishHud != null && polishHud.Find("Quick Help") != null) failures.Add("permanent general controls remain");

            Transform tablet = tabletCanvas != null ? tabletCanvas.transform.Find("Tablet") : null;
            Transform content = tablet != null ? tablet.Find("Content") : null;
            BusinessTabletUI tabletUI = tablet != null ? tablet.GetComponent<BusinessTabletUI>() : null;
            if (tablet == null || content == null || tabletUI == null)
            {
                failures.Add("tablet hierarchy is incomplete");
            }
            else
            {
                SerializedObject tabletSettings = new(tabletUI);
                SerializedProperty names = tabletSettings.FindProperty("sectionNames");
                string[] configuredNames = new string[names.arraySize];
                for (int index = 0; index < names.arraySize; index++) configuredNames[index] = names.GetArrayElementAtIndex(index).stringValue;
                if (!configuredNames.SequenceEqual(TabletPageNames)) failures.Add("tablet navigation does not match the required eight pages");
                if (content.Find("Land") != null || content.Find("Construction") != null) failures.Add("removed tablet pages still exist");
                if (content.Find("Game Guide") == null) failures.Add("Game Guide page missing");

                Transform popupLayer = tablet.Find("Popup Layer");
                TabletInventoryUI inventoryUI = content.GetComponentInChildren<TabletInventoryUI>(true);
                GameObject contextMenu = inventoryUI != null
                    ? new SerializedObject(inventoryUI).FindProperty("contextMenu").objectReferenceValue as GameObject
                    : null;
                if (popupLayer == null || popupLayer.GetSiblingIndex() != tablet.childCount - 1) failures.Add("Popup Layer is not the tablet's top rendering sibling");
                if (contextMenu == null || contextMenu.transform.parent != popupLayer) failures.Add("inventory context menu is not parented to Popup Layer");
            }

            InteractionPromptUI prompt = UnityEngine.Object.FindFirstObjectByType<InteractionPromptUI>(FindObjectsInactive.Include);
            if (prompt == null || prompt.GetComponent<PlacementHUD>() == null) failures.Add("unified placement prompt bridge missing");
            if (UnityEngine.Object.FindFirstObjectByType<SaveSystem>(FindObjectsInactive.Include) == null) failures.Add("save system missing");

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                int missingScripts = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(root);
                if (missingScripts > 0) failures.Add($"{missingScripts} missing script(s) under {root.name}");
            }

            if (failures.Count > 0) throw new InvalidOperationException("Phase 26 validation failed:\n- " + string.Join("\n- ", failures));
            Debug.Log("Growveld Phase 26 validation passed: exact tablet pages, top popup layer, Game Guide, contextual prompt bridge, removed hotbar/overlays, shared shadowed grow-light settings, and save system are intact.");
        }

        private static void ValidateLightPrefab(string path, string label, List<string> failures)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            GrowLight growLight = prefab != null ? prefab.GetComponent<GrowLight>() : null;
            Light source = growLight != null ? growLight.LightSource : null;
            if (growLight == null || source == null)
            {
                failures.Add($"{label} reference is missing");
                return;
            }

            if (!Mathf.Approximately(growLight.CoverageRadius, GrowLight.SharedCoverageRadius)) failures.Add($"{label} gameplay coverage is not 6");
            if (!Mathf.Approximately(growLight.VisualIntensity, GrowLight.SharedVisualIntensity)) failures.Add($"{label} visual intensity is not {GrowLight.SharedVisualIntensity}");
            if (!Mathf.Approximately(growLight.VisualRange, GrowLight.SharedVisualRange)) failures.Add($"{label} visual range is not {GrowLight.SharedVisualRange}");
            if (Vector4.Distance(growLight.VisualColor, GrowLight.SharedVisualColor) > 0.01f) failures.Add($"{label} does not use the shared cool-white color");
            if (source.shadows != LightShadows.Soft) failures.Add($"{label} does not cast soft shadows");
            if (!source.gameObject.activeSelf) failures.Add($"{label} Light GameObject is inactive");
        }

        private static InteractionPromptUI ConfigureInteractionPrompt(GameObject player)
        {
            InteractionPromptUI prompt = UnityEngine.Object.FindFirstObjectByType<InteractionPromptUI>(FindObjectsInactive.Include);
            if (prompt == null) throw new MissingReferenceException("Interaction Prompt UI is required.");

            RectTransform promptRect = prompt.GetComponent<RectTransform>();
            promptRect.anchorMin = new Vector2(0.5f, 0f);
            promptRect.anchorMax = new Vector2(0.5f, 0f);
            promptRect.pivot = new Vector2(0.5f, 0f);
            promptRect.anchoredPosition = new Vector2(0f, 64f);
            promptRect.sizeDelta = new Vector2(820f, 92f);

            Text promptText = prompt.GetComponentInChildren<Text>(true);
            if (promptText != null)
            {
                promptText.fontSize = 22;
                promptText.alignment = TextAnchor.MiddleCenter;
                promptText.horizontalOverflow = HorizontalWrapMode.Wrap;
                promptText.verticalOverflow = VerticalWrapMode.Truncate;
            }

            PlacementHUD placementPrompt = prompt.GetComponent<PlacementHUD>() ?? prompt.gameObject.AddComponent<PlacementHUD>();
            SerializedObject placementSettings = new(placementPrompt);
            placementSettings.FindProperty("placementController").objectReferenceValue = player.GetComponent<PlacementController>();
            placementSettings.FindProperty("promptUI").objectReferenceValue = prompt;
            placementSettings.ApplyModifiedPropertiesWithoutUndo();

            PlayerInteractor interactor = player.GetComponent<PlayerInteractor>();
            SerializedObject interactorSettings = new(interactor);
            interactorSettings.FindProperty("promptUI").objectReferenceValue = prompt;
            interactorSettings.ApplyModifiedPropertiesWithoutUndo();
            return prompt;
        }

        private static TabletInventoryUI ConfigureTablet(GameObject tabletCanvas, GameObject player)
        {
            Transform tablet = tabletCanvas.transform.Find("Tablet");
            Transform content = tablet != null ? tablet.Find("Content") : null;
            BusinessTabletUI tabletUI = tablet != null ? tablet.GetComponent<BusinessTabletUI>() : null;
            TabletInventoryUI inventoryUI = content != null ? content.GetComponentInChildren<TabletInventoryUI>(true) : null;
            if (tablet == null || content == null || tabletUI == null || inventoryUI == null) throw new MissingReferenceException("Tablet inventory hierarchy is incomplete.");

            GameObject contextMenu = new SerializedObject(inventoryUI).FindProperty("contextMenu").objectReferenceValue as GameObject;
            if (contextMenu != null) contextMenu.transform.SetParent(tablet, false);

            Transform oldPopupLayer = tablet.Find("Popup Layer");
            if (oldPopupLayer != null) UnityEngine.Object.DestroyImmediate(oldPopupLayer.gameObject);
            GameObject popupLayerObject = new("Popup Layer", typeof(RectTransform));
            popupLayerObject.transform.SetParent(tablet, false);
            RectTransform popupLayer = popupLayerObject.GetComponent<RectTransform>();
            Stretch(popupLayer, 0f);
            if (contextMenu != null)
            {
                contextMenu.transform.SetParent(popupLayer, false);
                RectTransform contextMenuRect = contextMenu.GetComponent<RectTransform>();
                contextMenuRect.anchorMin = new Vector2(0.5f, 0.5f);
                contextMenuRect.anchorMax = new Vector2(0.5f, 0.5f);
                contextMenu.SetActive(false);
            }

            RemoveChild(content, "Land");
            RemoveChild(content, "Construction");
            RemoveChild(content, "Game Guide");
            foreach (TabletTabButton tab in tablet.GetComponentsInChildren<TabletTabButton>(true))
            {
                UnityEngine.Object.DestroyImmediate(tab.gameObject);
            }

            GameObject guide = CreateGameGuide(content);
            Dictionary<string, GameObject> sectionsByName = new();
            for (int index = 0; index < content.childCount; index++)
            {
                GameObject section = content.GetChild(index).gameObject;
                sectionsByName[section.name] = section;
            }

            GameObject[] sections = TabletPageNames.Select(name => sectionsByName.TryGetValue(name, out GameObject section) ? section : null).ToArray();
            if (sections.Any(section => section == null)) throw new MissingReferenceException("One or more required tablet sections are missing.");
            SerializedObject tabletSettings = new(tabletUI);
            SetObjectArray(tabletSettings.FindProperty("sections"), sections);
            SerializedProperty names = tabletSettings.FindProperty("sectionNames");
            names.arraySize = TabletPageNames.Length;
            for (int index = 0; index < TabletPageNames.Length; index++) names.GetArrayElementAtIndex(index).stringValue = TabletPageNames[index];
            tabletSettings.ApplyModifiedPropertiesWithoutUndo();

            for (int index = 0; index < TabletPageNames.Length; index++) CreateTabletTab(tablet, tabletUI, index, TabletPageNames[index]);
            ReflowTabletTabs(tablet);
            popupLayer.SetAsLastSibling();

            SerializedObject inventorySettings = new(inventoryUI);
            inventorySettings.FindProperty("popupLayer").objectReferenceValue = popupLayer;
            inventorySettings.FindProperty("contextMenu").objectReferenceValue = contextMenu;
            inventorySettings.ApplyModifiedPropertiesWithoutUndo();
            guide.SetActive(false);
            tabletUI.ShowSection(0);

            BusinessTabletController controller = player.GetComponent<BusinessTabletController>();
            SerializedObject controllerSettings = new(controller);
            controllerSettings.FindProperty("inventoryUI").objectReferenceValue = inventoryUI;
            controllerSettings.ApplyModifiedPropertiesWithoutUndo();
            return inventoryUI;
        }

        private static GameObject CreateGameGuide(Transform content)
        {
            GameObject guide = new("Game Guide", typeof(RectTransform));
            guide.transform.SetParent(content, false);
            Stretch(guide.GetComponent<RectTransform>(), 0f);

            Text intro = CreateText(guide.transform, "Guide Intro", 20, FontStyle.Italic, TextAnchor.UpperLeft);
            SetAnchoredRect(intro.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -4f), new Vector2(0f, 44f));
            intro.text = "Controls shown here match the current keyboard and mouse implementation.";
            intro.color = new Color(0.72f, 0.82f, 0.74f);

            GameObject leftPanel = CreateGuidePanel(guide.transform, "Movement and Gameplay", new Vector2(0f, 0f), new Vector2(0.49f, 0.92f));
            Text left = CreateText(leftPanel.transform, "Guide Copy", 19, FontStyle.Normal, TextAnchor.UpperLeft);
            Stretch(left.rectTransform, 18f);
            left.text =
                "MOVEMENT\n" +
                "W A S D / Arrow Keys  -  Move\n" +
                "Mouse  -  Look\n" +
                "Left Shift  -  Sprint\n\n" +
                "FARMING AND CARRYING\n" +
                "E  -  Perform the action shown near the crosshair\n" +
                "E  -  Pick up or drop a carried object\n" +
                "Plant care automatically uses owned tools/supplies\n\n" +
                "GENERAL\n" +
                "T  -  Open / close the business tablet\n" +
                "H  -  Open the pause-menu controls reference\n" +
                "Esc  -  Pause / back\n" +
                "F5  -  Save locally\n" +
                "F9  -  Load local save\n" +
                "F2 or `  -  Admin console";

            GameObject rightPanel = CreateGuidePanel(guide.transform, "Construction and Placement", new Vector2(0.51f, 0f), new Vector2(1f, 0.92f));
            Text right = CreateText(rightPanel.transform, "Guide Copy", 19, FontStyle.Normal, TextAnchor.UpperLeft);
            Stretch(right.rectTransform, 18f);
            right.text =
                "CONSTRUCTION MODE\n" +
                "B  -  Enter / exit Construction Mode\n" +
                "Aim at equipment + E  -  Move it\n" +
                "Aim at equipment + Delete  -  Sell it\n" +
                "B or Esc  -  Exit Construction Mode\n\n" +
                "PLACE NEW EQUIPMENT\n" +
                "Tablet > Inventory > Right-click item > Place\n" +
                "Left Click  -  Place a valid preview\n" +
                "R  -  Rotate preview\n" +
                "Esc  -  Cancel without consuming the item\n" +
                "Delete  -  Sell while moving an existing item\n\n" +
                "PROMPTS\n" +
                "Only the currently valid action is shown.\n" +
                "Placement takes priority over construction,\n" +
                "then farming, then carry/drop.";
            return guide;
        }

        private static GameObject CreateGuidePanel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax)
        {
            GameObject panel = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panel.transform.SetParent(parent, false);
            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = new Vector2(0f, 0f);
            rect.offsetMax = new Vector2(0f, -2f);
            panel.GetComponent<Image>().color = new Color(0.04f, 0.085f, 0.052f, 0.92f);
            return panel;
        }

        private static void ConfigureInputSuspension(GameObject player, TabletInventoryUI inventoryUI)
        {
            BusinessTabletController tablet = player.GetComponent<BusinessTabletController>();
            ConstructionModeController construction = player.GetComponent<ConstructionModeController>();
            PlacementController placement = player.GetComponent<PlacementController>();
            Behaviour[] tabletSuspended =
            {
                player.GetComponent<FirstPersonController>(),
                player.GetComponent<PlayerInteractor>(),
                placement,
                construction
            };
            SerializedObject tabletSettings = new(tablet);
            SetBehaviourArray(tabletSettings.FindProperty("gameplayBehaviours"), tabletSuspended);
            tabletSettings.FindProperty("placementController").objectReferenceValue = placement;
            tabletSettings.FindProperty("constructionMode").objectReferenceValue = construction;
            tabletSettings.FindProperty("inventoryUI").objectReferenceValue = inventoryUI;
            tabletSettings.ApplyModifiedPropertiesWithoutUndo();

            PauseAndHelpController pause = player.GetComponent<PauseAndHelpController>();
            Behaviour[] pauseSuspended =
            {
                player.GetComponent<FirstPersonController>(),
                player.GetComponent<PlayerInteractor>(),
                placement,
                tablet,
                construction
            };
            SerializedObject pauseSettings = new(pause);
            SetBehaviourArray(pauseSettings.FindProperty("gameplayBehaviours"), pauseSuspended);
            pauseSettings.FindProperty("constructionMode").objectReferenceValue = construction;
            pauseSettings.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void RefreshPauseControls(Scene scene)
        {
            GameObject pauseUi = FindRoot(scene, "Pause UI");
            Transform controlsRoot = pauseUi != null ? pauseUi.transform.Find("Controls Reference") : null;
            if (controlsRoot == null) return;
            Text controlsText = controlsRoot.GetComponentsInChildren<Text>(true)
                .FirstOrDefault(text => text.text != null && text.text.Contains("FIRST FARM"));
            if (controlsText == null) return;
            controlsText.text =
                "W A S D / Arrows  Move                  Mouse       Look\n" +
                "Shift                    Sprint                 E              Context action / carry / drop\n" +
                "B                         Construction       T              Business tablet\n" +
                "R                         Rotate preview   Left Click   Place object\n" +
                "Delete                  Sell selected       F2 / `        Admin console\n" +
                "F5                       Save                    F9            Load\n" +
                "H                         Controls              Esc           Pause / back\n\n" +
                "FIRST FARM: Buy supplies from the tablet, wait for delivery, then use Tablet > Inventory\n" +
                "to place equipment. Plant, care for, harvest, dry, store, and sell your crop.";
        }

        private static void RemovePermanentHudPanels(Scene scene)
        {
            Transform hud = FindRoot(scene, "Polish HUD")?.transform;
            if (hud == null) return;
            RemoveChild(hud, "Quick Help");
            RemoveChild(hud, "Carry Status");
        }

        private static void CreateTabletTab(Transform tablet, BusinessTabletUI tabletUI, int index, string pageName)
        {
            GameObject buttonObject = new($"{pageName} Tab", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(TabletTabButton));
            buttonObject.transform.SetParent(tablet, false);
            buttonObject.GetComponent<Image>().color = pageName == "Game Guide"
                ? new Color(0.17f, 0.34f, 0.2f, 1f)
                : new Color(0.12f, 0.24f, 0.15f, 1f);
            Text label = CreateText(buttonObject.transform, "Label", 20, FontStyle.Normal, TextAnchor.MiddleCenter);
            Stretch(label.rectTransform, 5f);
            label.text = pageName;
            SerializedObject buttonSettings = new(buttonObject.GetComponent<TabletTabButton>());
            buttonSettings.FindProperty("tabletUI").objectReferenceValue = tabletUI;
            buttonSettings.FindProperty("sectionIndex").intValue = index;
            buttonSettings.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ReflowTabletTabs(Transform tablet)
        {
            TabletTabButton[] tabs = tablet.GetComponentsInChildren<TabletTabButton>(true);
            Array.Sort(tabs, (left, right) =>
                new SerializedObject(left).FindProperty("sectionIndex").intValue
                    .CompareTo(new SerializedObject(right).FindProperty("sectionIndex").intValue));
            for (int index = 0; index < tabs.Length; index++)
            {
                RectTransform rect = tabs[index].GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0f, 1f);
                rect.anchoredPosition = new Vector2(28f, -28f - index * 82f);
                rect.sizeDelta = new Vector2(205f, 60f);
            }
        }

        private static Text CreateText(Transform parent, string name, int fontSize, FontStyle style, TextAnchor alignment)
        {
            GameObject textObject = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            textObject.transform.SetParent(parent, false);
            Text text = textObject.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.alignment = alignment;
            text.color = Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            return text;
        }

        private static void SetAnchoredRect(RectTransform rect, Vector2 min, Vector2 max, Vector2 pivot, Vector2 position, Vector2 size)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void Stretch(RectTransform rect, float padding)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.one * padding;
            rect.offsetMax = Vector2.one * -padding;
        }

        private static void SetObjectArray(SerializedProperty property, GameObject[] values)
        {
            property.arraySize = values.Length;
            for (int index = 0; index < values.Length; index++) property.GetArrayElementAtIndex(index).objectReferenceValue = values[index];
        }

        private static void SetBehaviourArray(SerializedProperty property, Behaviour[] values)
        {
            property.arraySize = values.Length;
            for (int index = 0; index < values.Length; index++) property.GetArrayElementAtIndex(index).objectReferenceValue = values[index];
        }

        private static void RemoveChild(Transform parent, string name)
        {
            Transform child = parent != null ? parent.Find(name) : null;
            if (child != null) UnityEngine.Object.DestroyImmediate(child.gameObject);
        }

        private static GameObject FindRoot(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects()) if (root.name == name) return root;
            return null;
        }

        private static void RemoveRoot(Scene scene, string name)
        {
            GameObject root = FindRoot(scene, name);
            if (root != null) UnityEngine.Object.DestroyImmediate(root);
        }
    }
}
