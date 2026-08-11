using System.Collections.Generic;
using Growveld.Building;
using Growveld.Economy;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Growveld.Editor
{
    public static class Phase7LandSetup
    {
        private const string ScenePath = "Assets/_Project/Scenes/PrototypeFarm.unity";
        private const string BoundaryMaterialPath = "Assets/_Project/Materials/M_LandBoundary.mat";
        private const string ShedMaterialPath = "Assets/_Project/Materials/M_StarterShed.mat";

        [MenuItem("Growveld/Phase 7/Rebuild Land System")]
        public static void ConfigureLand()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject systems = FindRoot(scene, "Game Systems");
            EconomyManager economy = systems != null ? systems.GetComponent<EconomyManager>() : null;
            if (systems == null || economy == null)
            {
                throw new MissingReferenceException("Phase 6 Game Systems and EconomyManager are required.");
            }

            ExpandGround(scene);
            RemoveRoot(scene, "Land Plots");
            RemoveRoot(scene, "Starter Building");

            Material boundaryMaterial = CreateMaterial(BoundaryMaterialPath, new Color(0.3f, 0.75f, 0.35f));
            GameObject plotRoot = new("Land Plots");
            List<LandPlot> plots = new()
            {
                CreatePlot(plotRoot.transform, "starter", "Starter Plot", new Vector3(0f, 0.05f, 0f), new Vector2(20f, 20f), 0f, true, boundaryMaterial),
                CreatePlot(plotRoot.transform, "east", "East Plot", new Vector3(21f, 0.05f, 0f), new Vector2(20f, 20f), 15000f, false, boundaryMaterial),
                CreatePlot(plotRoot.transform, "north", "North Plot", new Vector3(0f, 0.05f, 21f), new Vector2(20f, 20f), 18000f, false, boundaryMaterial)
            };

            LandManager landManager = systems.GetComponent<LandManager>() ?? systems.AddComponent<LandManager>();
            SerializedObject managerSettings = new(landManager);
            managerSettings.FindProperty("economy").objectReferenceValue = economy;
            SerializedProperty plotArray = managerSettings.FindProperty("plots");
            plotArray.arraySize = plots.Count;
            for (int index = 0; index < plots.Count; index++)
            {
                plotArray.GetArrayElementAtIndex(index).objectReferenceValue = plots[index];
            }
            managerSettings.ApplyModifiedPropertiesWithoutUndo();

            foreach (LandPlot plot in plots)
            {
                CreatePurchaseSign(plot, landManager, boundaryMaterial);
            }

            CreateStarterBuilding();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = plotRoot;
            Debug.Log("Growveld Phase 7 setup complete: starter plot, two purchasable plots, boundaries, and starter building created.");
        }

        private static LandPlot CreatePlot(
            Transform parent,
            string id,
            string displayName,
            Vector3 position,
            Vector2 size,
            float price,
            bool owned,
            Material boundaryMaterial)
        {
            GameObject plotObject = new($"Land Plot - {displayName}", typeof(BoxCollider), typeof(LandPlot));
            plotObject.transform.SetParent(parent, false);
            plotObject.transform.position = position;
            BoxCollider area = plotObject.GetComponent<BoxCollider>();
            area.isTrigger = true;
            area.size = new Vector3(size.x, 2f, size.y);
            area.center = new Vector3(0f, 0.9f, 0f);

            List<Renderer> boundaries = new();
            boundaries.Add(CreateBoundary(plotObject.transform, "South Boundary", new Vector3(0f, 0.08f, -size.y * 0.5f), new Vector3(size.x, 0.12f, 0.14f), boundaryMaterial));
            boundaries.Add(CreateBoundary(plotObject.transform, "North Boundary", new Vector3(0f, 0.08f, size.y * 0.5f), new Vector3(size.x, 0.12f, 0.14f), boundaryMaterial));
            boundaries.Add(CreateBoundary(plotObject.transform, "West Boundary", new Vector3(-size.x * 0.5f, 0.08f, 0f), new Vector3(0.14f, 0.12f, size.y), boundaryMaterial));
            boundaries.Add(CreateBoundary(plotObject.transform, "East Boundary", new Vector3(size.x * 0.5f, 0.08f, 0f), new Vector3(0.14f, 0.12f, size.y), boundaryMaterial));

            LandPlot plot = plotObject.GetComponent<LandPlot>();
            SerializedObject settings = new(plot);
            settings.FindProperty("plotId").stringValue = id;
            settings.FindProperty("displayName").stringValue = displayName;
            settings.FindProperty("purchasePrice").floatValue = price;
            settings.FindProperty("startingOwned").boolValue = owned;
            settings.FindProperty("isOwned").boolValue = owned;
            SerializedProperty rendererArray = settings.FindProperty("boundaryRenderers");
            rendererArray.arraySize = boundaries.Count;
            for (int index = 0; index < boundaries.Count; index++)
            {
                rendererArray.GetArrayElementAtIndex(index).objectReferenceValue = boundaries[index];
            }
            settings.ApplyModifiedPropertiesWithoutUndo();
            return plot;
        }

        private static Renderer CreateBoundary(Transform parent, string name, Vector3 localPosition, Vector3 scale, Material material)
        {
            GameObject boundary = GameObject.CreatePrimitive(PrimitiveType.Cube);
            boundary.name = name;
            boundary.transform.SetParent(parent, false);
            boundary.transform.localPosition = localPosition;
            boundary.transform.localScale = scale;
            Object.DestroyImmediate(boundary.GetComponent<Collider>());
            Renderer renderer = boundary.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            return renderer;
        }

        private static void CreatePurchaseSign(LandPlot plot, LandManager manager, Material material)
        {
            Vector3 boundsMin = plot.WorldBounds.min;
            GameObject sign = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sign.name = "Purchase Sign";
            sign.transform.SetParent(plot.transform, false);
            sign.transform.position = new Vector3(plot.transform.position.x, 1f, boundsMin.z + 1.2f);
            sign.transform.localScale = new Vector3(2.2f, 1.7f, 0.25f);
            sign.GetComponent<MeshRenderer>().sharedMaterial = material;

            LandPurchaseSign purchaseSign = sign.AddComponent<LandPurchaseSign>();
            SerializedObject settings = new(purchaseSign);
            settings.FindProperty("plot").objectReferenceValue = plot;
            settings.FindProperty("landManager").objectReferenceValue = manager;
            settings.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CreateStarterBuilding()
        {
            Material shedMaterial = CreateMaterial(ShedMaterialPath, new Color(0.56f, 0.42f, 0.27f));
            GameObject building = new("Starter Building");
            Vector3 centre = new Vector3(-5.5f, 0f, 5f);
            CreateBuildingPart(building.transform, "Floor", centre + new Vector3(0f, 0.1f, 0f), new Vector3(7f, 0.2f, 6f), shedMaterial);
            CreateBuildingPart(building.transform, "Back Wall", centre + new Vector3(0f, 1.6f, 2.9f), new Vector3(7f, 3f, 0.2f), shedMaterial);
            CreateBuildingPart(building.transform, "Left Wall", centre + new Vector3(-3.4f, 1.6f, 0f), new Vector3(0.2f, 3f, 6f), shedMaterial);
            CreateBuildingPart(building.transform, "Right Wall", centre + new Vector3(3.4f, 1.6f, 0f), new Vector3(0.2f, 3f, 6f), shedMaterial);
            CreateBuildingPart(building.transform, "Roof", centre + new Vector3(0f, 3.15f, 0f), new Vector3(7.2f, 0.2f, 6.2f), shedMaterial);
        }

        private static void CreateBuildingPart(Transform parent, string name, Vector3 position, Vector3 scale, Material material)
        {
            GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.position = position;
            part.transform.localScale = scale;
            part.GetComponent<MeshRenderer>().sharedMaterial = material;
        }

        private static void ExpandGround(Scene scene)
        {
            GameObject environment = FindRoot(scene, "Environment");
            Transform ground = environment != null ? environment.transform.Find("Ground") : null;
            if (ground != null)
            {
                ground.localScale = new Vector3(70f, 0.1f, 70f);
            }
        }

        private static Material CreateMaterial(string path, Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            material.shader = shader;
            material.SetColor("_BaseColor", color);
            EditorUtility.SetDirty(material);
            return material;
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
