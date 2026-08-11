using System.IO;
using Growveld.Interaction;
using Growveld.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Growveld.Editor
{
    /// <summary>
    /// Creates and wires the Phase 3 interaction UI, PlayerInteractor, and test crate.
    /// </summary>
    public static class Phase3InteractionSetup
    {
        private const string PrototypeScenePath = "Assets/_Project/Scenes/PrototypeFarm.unity";
        private const string TestMaterialPath = "Assets/_Project/Materials/M_InteractionTest.mat";

        [MenuItem("Growveld/Phase 3/Rebuild Interaction Test")]
        public static void RebuildFromMenu()
        {
            bool shouldRebuild = EditorUtility.DisplayDialog(
                "Rebuild the Phase 3 interaction setup?",
                "This replaces the Interaction UI and test crate, then reconnects the PlayerInteractor.",
                "Rebuild",
                "Cancel");

            if (shouldRebuild)
            {
                ConfigureInteraction();
            }
        }

        public static void ConfigureInteraction()
        {
            if (!File.Exists(PrototypeScenePath))
            {
                throw new FileNotFoundException("PrototypeFarm scene was not found.", PrototypeScenePath);
            }

            Scene previousActiveScene = SceneManager.GetActiveScene();
            Scene prototypeScene = SceneManager.GetSceneByPath(PrototypeScenePath);
            bool sceneWasAlreadyLoaded = prototypeScene.IsValid() && prototypeScene.isLoaded;
            bool openedAsSingleScene = Application.isBatchMode;

            if (openedAsSingleScene)
            {
                prototypeScene = EditorSceneManager.OpenScene(PrototypeScenePath, OpenSceneMode.Single);
            }
            else if (!sceneWasAlreadyLoaded)
            {
                prototypeScene = EditorSceneManager.OpenScene(PrototypeScenePath, OpenSceneMode.Additive);
            }

            SceneManager.SetActiveScene(prototypeScene);

            GameObject playerObject = FindRootObject(prototypeScene, "Player");
            if (playerObject == null)
            {
                throw new MissingReferenceException("Phase 2 Player was not found in PrototypeFarm.");
            }

            Camera playerCamera = playerObject.GetComponentInChildren<Camera>(true);
            if (playerCamera == null)
            {
                throw new MissingReferenceException("The Player Camera was not found.");
            }

            RemoveRootObject(prototypeScene, "Interaction UI");
            RemoveRootObject(prototypeScene, "Interaction Test Crate");

            InteractionPromptUI promptUI = CreateInteractionUI();
            ConfigurePlayerInteractor(playerObject, playerCamera, promptUI);
            CreateInteractionTestCrate();

            EditorSceneManager.MarkSceneDirty(prototypeScene);
            EditorSceneManager.SaveScene(prototypeScene, PrototypeScenePath);
            AssetDatabase.SaveAssets();

            if (!openedAsSingleScene && !sceneWasAlreadyLoaded)
            {
                EditorSceneManager.CloseScene(prototypeScene, true);

                if (previousActiveScene.IsValid() && previousActiveScene.isLoaded)
                {
                    SceneManager.SetActiveScene(previousActiveScene);
                }
            }
            else if (!Application.isBatchMode)
            {
                Selection.activeGameObject = playerObject;
            }

            Debug.Log("Growveld Phase 3 setup complete: reusable interaction system and test crate created.");
        }

        private static InteractionPromptUI CreateInteractionUI()
        {
            GameObject canvasObject = new GameObject(
                "Interaction UI",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;

            CanvasScaler canvasScaler = canvasObject.GetComponent<CanvasScaler>();
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(1920f, 1080f);
            canvasScaler.matchWidthOrHeight = 0.5f;

            GameObject promptObject = new GameObject(
                "Interaction Prompt",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(CanvasGroup),
                typeof(InteractionPromptUI));
            promptObject.transform.SetParent(canvasObject.transform, false);

            RectTransform promptRect = promptObject.GetComponent<RectTransform>();
            promptRect.anchorMin = new Vector2(0.5f, 0f);
            promptRect.anchorMax = new Vector2(0.5f, 0f);
            promptRect.pivot = new Vector2(0.5f, 0f);
            promptRect.anchoredPosition = new Vector2(0f, 85f);
            promptRect.sizeDelta = new Vector2(500f, 58f);

            Image background = promptObject.GetComponent<Image>();
            background.color = new Color(0.035f, 0.045f, 0.035f, 0.84f);

            CanvasGroup promptGroup = promptObject.GetComponent<CanvasGroup>();
            promptGroup.alpha = 0f;
            promptGroup.interactable = false;
            promptGroup.blocksRaycasts = false;

            GameObject textObject = new GameObject(
                "Prompt Text",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Text));
            textObject.transform.SetParent(promptObject.transform, false);

            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(18f, 6f);
            textRect.offsetMax = new Vector2(-18f, -6f);

            Text promptText = textObject.GetComponent<Text>();
            promptText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            promptText.fontSize = 24;
            promptText.alignment = TextAnchor.MiddleCenter;
            promptText.color = Color.white;
            promptText.text = "[E] Interact";

            InteractionPromptUI promptUI = promptObject.GetComponent<InteractionPromptUI>();
            SerializedObject promptSettings = new SerializedObject(promptUI);
            promptSettings.FindProperty("promptGroup").objectReferenceValue = promptGroup;
            promptSettings.FindProperty("promptText").objectReferenceValue = promptText;
            promptSettings.ApplyModifiedPropertiesWithoutUndo();

            return promptUI;
        }

        private static void ConfigurePlayerInteractor(
            GameObject playerObject,
            Camera playerCamera,
            InteractionPromptUI promptUI)
        {
            PlayerInteractor interactor = playerObject.GetComponent<PlayerInteractor>();
            if (interactor == null)
            {
                interactor = playerObject.AddComponent<PlayerInteractor>();
            }

            SerializedObject interactorSettings = new SerializedObject(interactor);
            interactorSettings.FindProperty("viewCamera").objectReferenceValue = playerCamera;
            interactorSettings.FindProperty("promptUI").objectReferenceValue = promptUI;
            interactorSettings.FindProperty("interactionDistance").floatValue = 4.5f;
            interactorSettings.FindProperty("interactionLayers").intValue = Physics.DefaultRaycastLayers;
            interactorSettings.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CreateInteractionTestCrate()
        {
            Material testMaterial = CreateOrUpdateTestMaterial();

            GameObject testCrate = GameObject.CreatePrimitive(PrimitiveType.Cube);
            testCrate.name = "Interaction Test Crate";
            testCrate.transform.position = new Vector3(0f, 1f, -1f);
            testCrate.transform.rotation = Quaternion.identity;
            testCrate.transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);

            MeshRenderer crateRenderer = testCrate.GetComponent<MeshRenderer>();
            crateRenderer.sharedMaterial = testMaterial;

            PrototypeInteractable testInteractable = testCrate.AddComponent<PrototypeInteractable>();
            SerializedObject interactableSettings = new SerializedObject(testInteractable);
            interactableSettings.FindProperty("interactionPrompt").stringValue = "Inspect test crate";
            interactableSettings.FindProperty("targetRenderer").objectReferenceValue = crateRenderer;
            interactableSettings.FindProperty("activatedColor").colorValue = new Color(0.3f, 0.65f, 0.42f, 1f);
            interactableSettings.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Material CreateOrUpdateTestMaterial()
        {
            Shader urpLitShader = Shader.Find("Universal Render Pipeline/Lit");
            if (urpLitShader == null)
            {
                throw new System.InvalidOperationException("URP Lit shader was not found.");
            }

            Material material = AssetDatabase.LoadAssetAtPath<Material>(TestMaterialPath);
            if (material == null)
            {
                material = new Material(urpLitShader);
                AssetDatabase.CreateAsset(material, TestMaterialPath);
            }
            else
            {
                material.shader = urpLitShader;
            }

            Color crateColor = new Color(0.79f, 0.48f, 0.2f, 1f);
            material.SetColor("_BaseColor", crateColor);
            material.SetColor("_Color", crateColor);
            material.SetFloat("_Metallic", 0f);
            material.SetFloat("_Smoothness", 0.25f);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static GameObject FindRootObject(Scene scene, string objectName)
        {
            foreach (GameObject rootObject in scene.GetRootGameObjects())
            {
                if (rootObject.name == objectName)
                {
                    return rootObject;
                }
            }

            return null;
        }

        private static void RemoveRootObject(Scene scene, string objectName)
        {
            GameObject rootObject = FindRootObject(scene, objectName);
            if (rootObject != null)
            {
                Object.DestroyImmediate(rootObject);
            }
        }
    }
}
