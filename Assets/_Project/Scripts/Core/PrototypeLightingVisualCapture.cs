using System.Collections;
using System.IO;
using System.Linq;
using Growveld.Economy;
using Growveld.Environment;
using Growveld.Inventory;
using Growveld.Saving;
using UnityEngine;

namespace Growveld.Core
{
    /// <summary>
    /// Standalone-only controlled lighting bay used for visual regression captures.
    /// </summary>
    public sealed class PrototypeLightingVisualCapture : MonoBehaviour
    {
        private const string CommandLineFlag = "--followup-lighting-capture";
        private static readonly Vector3 CaptureOrigin = new(2000f, 0f, 2000f);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (Application.isEditor || !System.Environment.GetCommandLineArgs().Contains(CommandLineFlag)) return;
            new GameObject("Follow-Up Lighting Visual Capture").AddComponent<PrototypeLightingVisualCapture>();
        }

        private IEnumerator Start()
        {
            yield return null;
            yield return null;
            yield return null;

            SaveSystem saveSystem = FindFirstObjectByType<SaveSystem>();
            if (saveSystem != null) Destroy(saveSystem);
            foreach (Canvas canvas in FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                canvas.gameObject.SetActive(false);
            }
            foreach (Camera camera in FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                camera.gameObject.SetActive(false);
            }
            foreach (Light light in FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (light.type == LightType.Directional) light.enabled = false;
            }

            Material wallMaterial = CreateMaterial(new Color(0.19f, 0.22f, 0.2f));
            Material floorMaterial = CreateMaterial(new Color(0.28f, 0.31f, 0.29f));
            CreatePart("Floor", CaptureOrigin + new Vector3(0f, -0.1f, 1f), new Vector3(10f, 0.2f, 12f), floorMaterial);
            CreatePart("Back Wall", CaptureOrigin + new Vector3(0f, 2.1f, 7f), new Vector3(10f, 4.2f, 0.2f), wallMaterial);
            CreatePart("Left Wall", CaptureOrigin + new Vector3(-5f, 2.1f, 1f), new Vector3(0.2f, 4.2f, 12f), wallMaterial);
            CreatePart("Right Wall", CaptureOrigin + new Vector3(5f, 2.1f, 1f), new Vector3(0.2f, 4.2f, 12f), wallMaterial);
            CreatePart("Ceiling", CaptureOrigin + new Vector3(0f, 4.2f, 1f), new Vector3(10f, 0.2f, 12f), wallMaterial);
            CreatePart("Centre Divider", CaptureOrigin + new Vector3(0f, 1.1f, 2f), new Vector3(0.18f, 2.2f, 4f), wallMaterial);

            ShopManager shop = FindFirstObjectByType<ShopManager>();
            ItemDefinition floorItem = shop?.AvailableItems?.FirstOrDefault(item => item != null && item.ItemId == "grow_light");
            ItemDefinition ceilingItem = shop?.AvailableItems?.FirstOrDefault(item => item != null && item.ItemId == "ceiling_grow_light");
            GrowLight floorLight = SpawnLight(floorItem, CaptureOrigin + new Vector3(-2.4f, 0f, 1.5f));
            GrowLight ceilingLight = SpawnLight(ceilingItem, CaptureOrigin + new Vector3(2.4f, 3.98f, 1.5f));
            floorLight?.SetExternalSchedule(true);
            ceilingLight?.SetExternalSchedule(true);

            GameObject cameraObject = new("Lighting Capture Camera", typeof(Camera));
            Camera captureCamera = cameraObject.GetComponent<Camera>();
            captureCamera.transform.position = CaptureOrigin + new Vector3(0f, 1.8f, -9.2f);
            captureCamera.transform.LookAt(CaptureOrigin + new Vector3(0f, 1.7f, 1.7f));
            captureCamera.fieldOfView = 54f;
            captureCamera.clearFlags = CameraClearFlags.Skybox;
            captureCamera.allowHDR = true;

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.07f, 0.075f, 0.085f);
            string outputPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "FollowUpLighting.png"));
            if (File.Exists(outputPath)) File.Delete(outputPath);
            RenderTexture target = new(1280, 720, 24, RenderTextureFormat.ARGB32);
            Texture2D image = new(1280, 720, TextureFormat.RGB24, false);
            captureCamera.targetTexture = target;
            captureCamera.Render();
            RenderTexture previousTarget = RenderTexture.active;
            RenderTexture.active = target;
            image.ReadPixels(new Rect(0f, 0f, target.width, target.height), 0, 0);
            image.Apply();
            File.WriteAllBytes(outputPath, image.EncodeToPNG());
            RenderTexture.active = previousTarget;
            captureCamera.targetTexture = null;
            Destroy(target);
            Destroy(image);
            yield return null;

            if (File.Exists(outputPath))
            {
                Debug.Log($"Growveld follow-up lighting visual capture saved: {outputPath}");
                Application.Quit(0);
            }
            else
            {
                Debug.LogError("Growveld follow-up lighting visual capture failed: screenshot file was not created.");
                Application.Quit(1);
            }
        }

        private static GrowLight SpawnLight(ItemDefinition item, Vector3 position)
        {
            if (item?.PlaceableDefinition?.Prefab == null) return null;
            GameObject instance = Instantiate(item.PlaceableDefinition.Prefab, position, Quaternion.identity);
            return instance.GetComponent<GrowLight>();
        }

        private static void CreatePart(string name, Vector3 position, Vector3 scale, Material material)
        {
            GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
            part.name = name;
            part.transform.SetPositionAndRotation(position, Quaternion.identity);
            part.transform.localScale = scale;
            part.GetComponent<Renderer>().sharedMaterial = material;
        }

        private static Material CreateMaterial(Color color)
        {
            Material material = new(Shader.Find("Universal Render Pipeline/Lit"));
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", 0.18f);
            return material;
        }
    }
}
