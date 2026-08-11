using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Growveld.Editor
{
    /// <summary>
    /// Creates the Phase 1 prototype scene with Unity's editor APIs.
    /// This script is editor-only and is not included in a game build.
    /// </summary>
    [InitializeOnLoad]
    public static class Phase1SceneSetup
    {
        private const string ProjectRoot = "Assets/_Project";
        private const string MaterialsFolder = ProjectRoot + "/Materials";
        private const string ScenesFolder = ProjectRoot + "/Scenes";
        private const string GroundMaterialPath = MaterialsFolder + "/M_Ground.mat";
        private const string PrototypeScenePath = ScenesFolder + "/PrototypeFarm.unity";

        static Phase1SceneSetup()
        {
            if (!File.Exists(PrototypeScenePath))
            {
                EditorApplication.delayCall += CreatePrototypeScene;
            }
        }

        [MenuItem("Growveld/Phase 1/Rebuild Prototype Scene")]
        public static void RebuildPrototypeScene()
        {
            bool shouldRebuild = EditorUtility.DisplayDialog(
                "Rebuild PrototypeFarm?",
                "This replaces Assets/_Project/Scenes/PrototypeFarm.unity with the Phase 1 starter scene.",
                "Rebuild",
                "Cancel");

            if (shouldRebuild)
            {
                CreatePrototypeScene();
            }
        }

        public static void CreatePrototypeScene()
        {
            EnsureFolder(ProjectRoot);
            EnsureFolder(MaterialsFolder);
            EnsureFolder(ScenesFolder);

            Material groundMaterial = CreateOrUpdateGroundMaterial();

            Scene previousScene = SceneManager.GetActiveScene();
            bool shouldReplaceCurrentScene = Application.isBatchMode
                || (!previousScene.isDirty && string.IsNullOrEmpty(previousScene.path));
            NewSceneMode creationMode = shouldReplaceCurrentScene
                ? NewSceneMode.Single
                : NewSceneMode.Additive;
            Scene prototypeScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, creationMode);
            prototypeScene.name = "PrototypeFarm";
            SceneManager.SetActiveScene(prototypeScene);

            GameObject environment = new GameObject("Environment");
            ResetTransform(environment.transform);

            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.SetParent(environment.transform);
            ground.transform.localPosition = Vector3.zero;
            ground.transform.localRotation = Quaternion.identity;
            ground.transform.localScale = new Vector3(10f, 1f, 10f);
            ground.GetComponent<MeshRenderer>().sharedMaterial = groundMaterial;
            GameObjectUtility.SetStaticEditorFlags(ground, StaticEditorFlags.ContributeGI);

            GameObject sun = new GameObject("Sun", typeof(Light));
            sun.transform.SetParent(environment.transform);
            sun.transform.localPosition = Vector3.zero;
            sun.transform.localRotation = Quaternion.Euler(50f, -30f, 0f);
            sun.transform.localScale = Vector3.one;

            Light sunLight = sun.GetComponent<Light>();
            sunLight.type = LightType.Directional;
            sunLight.color = ParseHtmlColor("#FFF4DF");
            sunLight.intensity = 1.2f;
            sunLight.shadows = LightShadows.Soft;

            GameObject cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 5f, -12f);
            cameraObject.transform.rotation = Quaternion.Euler(18f, 0f, 0f);

            Camera mainCamera = cameraObject.GetComponent<Camera>();
            mainCamera.clearFlags = CameraClearFlags.Skybox;
            mainCamera.fieldOfView = 60f;
            mainCamera.nearClipPlane = 0.3f;
            mainCamera.farClipPlane = 1000f;

            RenderSettings.ambientMode = AmbientMode.Skybox;
            RenderSettings.ambientIntensity = 1f;
            RenderSettings.sun = sunLight;

            EditorSceneManager.MarkSceneDirty(prototypeScene);
            EditorSceneManager.SaveScene(prototypeScene, PrototypeScenePath);

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(PrototypeScenePath, true)
            };

            if (!shouldReplaceCurrentScene
                && previousScene.IsValid()
                && previousScene.isLoaded
                && !previousScene.isDirty)
            {
                EditorSceneManager.CloseScene(previousScene, true);
            }

            SceneManager.SetActiveScene(prototypeScene);
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<SceneAsset>(PrototypeScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("Growveld Phase 1 setup complete: PrototypeFarm scene and M_Ground material created.");
        }

        private static Material CreateOrUpdateGroundMaterial()
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(GroundMaterialPath);
            Shader urpLitShader = Shader.Find("Universal Render Pipeline/Lit");

            if (urpLitShader == null)
            {
                throw new System.InvalidOperationException(
                    "Universal Render Pipeline/Lit shader was not found. Confirm that URP is installed and configured.");
            }

            if (material == null)
            {
                material = new Material(urpLitShader);
                AssetDatabase.CreateAsset(material, GroundMaterialPath);
            }
            else
            {
                material.shader = urpLitShader;
            }

            material.name = "M_Ground";
            material.SetColor("_BaseColor", ParseHtmlColor("#6E7B42"));
            material.SetFloat("_Metallic", 0f);
            material.SetFloat("_Smoothness", 0.15f);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void EnsureFolder(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath))
            {
                return;
            }

            string parentPath = Path.GetDirectoryName(folderPath)?.Replace('\\', '/');
            string folderName = Path.GetFileName(folderPath);

            if (!string.IsNullOrEmpty(parentPath))
            {
                EnsureFolder(parentPath);
                AssetDatabase.CreateFolder(parentPath, folderName);
            }
        }

        private static void ResetTransform(Transform target)
        {
            target.position = Vector3.zero;
            target.rotation = Quaternion.identity;
            target.localScale = Vector3.one;
        }

        private static Color ParseHtmlColor(string htmlColor)
        {
            if (ColorUtility.TryParseHtmlString(htmlColor, out Color color))
            {
                return color;
            }

            return Color.white;
        }
    }
}
