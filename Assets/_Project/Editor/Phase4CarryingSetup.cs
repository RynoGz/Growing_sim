using System.IO;
using Growveld.Carrying;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Growveld.Editor
{
    /// <summary>
    /// Creates the Player carry anchor and two physical Phase 4 test crates.
    /// </summary>
    public static class Phase4CarryingSetup
    {
        private const string PrototypeScenePath = "Assets/_Project/Scenes/PrototypeFarm.unity";
        private const string CarryMaterialPath = "Assets/_Project/Materials/M_CarryableTest.mat";

        [MenuItem("Growveld/Phase 4/Rebuild Carrying Test")]
        public static void RebuildFromMenu()
        {
            bool shouldRebuild = EditorUtility.DisplayDialog(
                "Rebuild the Phase 4 carrying setup?",
                "This replaces the carry anchor and both physical carrying test crates.",
                "Rebuild",
                "Cancel");

            if (shouldRebuild)
            {
                ConfigureCarrying();
            }
        }

        public static void ConfigureCarrying()
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

            Transform carryAnchor = CreateCarryAnchor(playerCamera.transform);
            ConfigureCarryController(playerObject, carryAnchor);

            RemoveRootObject(prototypeScene, "Carryable Test Crate A");
            RemoveRootObject(prototypeScene, "Carryable Test Crate B");

            Material carryMaterial = CreateOrUpdateCarryMaterial();
            CreateCarryableCrate("Carryable Test Crate A", "blue supply crate", new Vector3(-2f, 0.6f, -1f), carryMaterial);
            CreateCarryableCrate("Carryable Test Crate B", "blue supply crate", new Vector3(2f, 0.6f, -1f), carryMaterial);

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

            Debug.Log("Growveld Phase 4 setup complete: one-slot large-object carrying created.");
        }

        private static Transform CreateCarryAnchor(Transform cameraTransform)
        {
            Transform existingAnchor = cameraTransform.Find("Carry Anchor");
            if (existingAnchor != null)
            {
                Object.DestroyImmediate(existingAnchor.gameObject);
            }

            GameObject anchorObject = new GameObject("Carry Anchor");
            anchorObject.transform.SetParent(cameraTransform, false);
            anchorObject.transform.localPosition = new Vector3(0f, -0.25f, 2.2f);
            anchorObject.transform.localRotation = Quaternion.identity;
            anchorObject.transform.localScale = Vector3.one;
            return anchorObject.transform;
        }

        private static void ConfigureCarryController(GameObject playerObject, Transform carryAnchor)
        {
            PlayerCarryController carryController = playerObject.GetComponent<PlayerCarryController>();
            if (carryController == null)
            {
                carryController = playerObject.AddComponent<PlayerCarryController>();
            }

            SerializedObject carrySettings = new SerializedObject(carryController);
            carrySettings.FindProperty("carryAnchor").objectReferenceValue = carryAnchor;
            carrySettings.FindProperty("inheritedDropVelocity").floatValue = 0.35f;
            carrySettings.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CreateCarryableCrate(
            string objectName,
            string displayName,
            Vector3 position,
            Material material)
        {
            GameObject crate = GameObject.CreatePrimitive(PrimitiveType.Cube);
            crate.name = objectName;
            crate.transform.position = position;
            crate.transform.rotation = Quaternion.identity;
            crate.transform.localScale = Vector3.one * 1.2f;

            MeshRenderer renderer = crate.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;

            Rigidbody rigidbodyComponent = crate.AddComponent<Rigidbody>();
            rigidbodyComponent.mass = 5f;
            rigidbodyComponent.useGravity = true;
            rigidbodyComponent.isKinematic = false;
            rigidbodyComponent.interpolation = RigidbodyInterpolation.Interpolate;
            rigidbodyComponent.collisionDetectionMode = CollisionDetectionMode.Continuous;
            rigidbodyComponent.linearDamping = 0.6f;
            rigidbodyComponent.angularDamping = 1.5f;

            CarryableObject carryable = crate.AddComponent<CarryableObject>();
            SerializedObject carryableSettings = new SerializedObject(carryable);
            carryableSettings.FindProperty("displayName").stringValue = displayName;
            carryableSettings.FindProperty("rigidbodyComponent").objectReferenceValue = rigidbodyComponent;
            carryableSettings.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Material CreateOrUpdateCarryMaterial()
        {
            Shader urpLitShader = Shader.Find("Universal Render Pipeline/Lit");
            if (urpLitShader == null)
            {
                throw new System.InvalidOperationException("URP Lit shader was not found.");
            }

            Material material = AssetDatabase.LoadAssetAtPath<Material>(CarryMaterialPath);
            if (material == null)
            {
                material = new Material(urpLitShader);
                AssetDatabase.CreateAsset(material, CarryMaterialPath);
            }
            else
            {
                material.shader = urpLitShader;
            }

            Color crateColor = new Color(0.16f, 0.42f, 0.67f, 1f);
            material.SetColor("_BaseColor", crateColor);
            material.SetColor("_Color", crateColor);
            material.SetFloat("_Metallic", 0.05f);
            material.SetFloat("_Smoothness", 0.3f);
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
