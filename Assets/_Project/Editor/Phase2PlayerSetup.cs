using System.IO;
using Growveld.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace Growveld.Editor
{
    /// <summary>
    /// Builds the Phase 2 Player prefab-like scene hierarchy with repeatable settings.
    /// It remains available from the Growveld menu for repeatable scene setup.
    /// </summary>
    public static class Phase2PlayerSetup
    {
        private const string PrototypeScenePath = "Assets/_Project/Scenes/PrototypeFarm.unity";
        private const string InputActionsPath = "Assets/InputSystem_Actions.inputactions";

        [MenuItem("Growveld/Phase 2/Rebuild First-Person Player")]
        public static void RebuildPlayerFromMenu()
        {
            bool shouldRebuild = EditorUtility.DisplayDialog(
                "Rebuild the first-person player?",
                "This replaces the Player and existing scene cameras with the Phase 2 setup.",
                "Rebuild",
                "Cancel");

            if (shouldRebuild)
            {
                ConfigurePlayer();
            }
        }

        public static void ConfigurePlayer()
        {
            ConfigurePlayerInternal();
        }

        private static void ConfigurePlayerInternal()
        {
            InputActionAsset inputActions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);
            if (inputActions == null)
            {
                throw new FileNotFoundException("The Input System actions asset was not found.", InputActionsPath);
            }

            Scene previousActiveScene = SceneManager.GetActiveScene();
            Scene prototypeScene = SceneManager.GetSceneByPath(PrototypeScenePath);
            bool sceneWasAlreadyLoaded = prototypeScene.IsValid() && prototypeScene.isLoaded;

            if (!sceneWasAlreadyLoaded)
            {
                prototypeScene = EditorSceneManager.OpenScene(PrototypeScenePath, OpenSceneMode.Additive);
            }

            SceneManager.SetActiveScene(prototypeScene);
            RemovePreviousPlayerAndCameras(prototypeScene);

            GameObject playerObject = new GameObject("Player");
            playerObject.tag = "Player";
            playerObject.transform.position = new Vector3(0f, 0.05f, -5f);
            playerObject.transform.rotation = Quaternion.identity;

            CharacterController characterController = playerObject.AddComponent<CharacterController>();
            characterController.height = 1.8f;
            characterController.radius = 0.35f;
            characterController.center = new Vector3(0f, 0.9f, 0f);
            characterController.slopeLimit = 45f;
            characterController.stepOffset = 0.3f;
            characterController.skinWidth = 0.08f;
            characterController.minMoveDistance = 0f;

            PlayerInput playerInput = playerObject.AddComponent<PlayerInput>();
            playerInput.actions = inputActions;
            playerInput.defaultActionMap = "Player";
            playerInput.notificationBehavior = PlayerNotifications.InvokeCSharpEvents;

            FirstPersonController firstPersonController = playerObject.AddComponent<FirstPersonController>();

            GameObject cameraObject = new GameObject(
                "Player Camera",
                typeof(Camera),
                typeof(AudioListener),
                typeof(UniversalAdditionalCameraData));
            cameraObject.tag = "MainCamera";
            cameraObject.transform.SetParent(playerObject.transform, false);
            cameraObject.transform.localPosition = new Vector3(0f, 1.65f, 0f);
            cameraObject.transform.localRotation = Quaternion.identity;

            Camera playerCamera = cameraObject.GetComponent<Camera>();
            playerCamera.clearFlags = CameraClearFlags.Skybox;
            playerCamera.fieldOfView = 60f;
            playerCamera.nearClipPlane = 0.1f;
            playerCamera.farClipPlane = 1000f;

            SerializedObject controllerSettings = new SerializedObject(firstPersonController);
            controllerSettings.FindProperty("cameraTransform").objectReferenceValue = cameraObject.transform;
            controllerSettings.FindProperty("walkSpeed").floatValue = 4.5f;
            controllerSettings.FindProperty("sprintSpeed").floatValue = 7f;
            controllerSettings.FindProperty("acceleration").floatValue = 20f;
            controllerSettings.FindProperty("gravity").floatValue = -20f;
            controllerSettings.FindProperty("groundedForce").floatValue = -2f;
            controllerSettings.FindProperty("mouseSensitivity").floatValue = 0.08f;
            controllerSettings.FindProperty("maximumLookAngle").floatValue = 85f;
            controllerSettings.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(prototypeScene);
            EditorSceneManager.SaveScene(prototypeScene, PrototypeScenePath);

            if (sceneWasAlreadyLoaded)
            {
                Selection.activeGameObject = playerObject;
            }
            else
            {
                EditorSceneManager.CloseScene(prototypeScene, true);

                if (previousActiveScene.IsValid() && previousActiveScene.isLoaded)
                {
                    SceneManager.SetActiveScene(previousActiveScene);
                }
            }

            AssetDatabase.SaveAssets();
            Debug.Log("Growveld Phase 2 setup complete: first-person Player created.");
        }

        private static void RemovePreviousPlayerAndCameras(Scene scene)
        {
            foreach (GameObject rootObject in scene.GetRootGameObjects())
            {
                if (rootObject.name == "Player")
                {
                    Object.DestroyImmediate(rootObject);
                    continue;
                }

                Camera[] cameras = rootObject.GetComponentsInChildren<Camera>(true);
                foreach (Camera existingCamera in cameras)
                {
                    Object.DestroyImmediate(existingCamera.gameObject);
                }
            }
        }
    }
}
