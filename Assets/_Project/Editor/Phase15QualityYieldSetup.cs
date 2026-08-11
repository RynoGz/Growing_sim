using Growveld.Farming;
using UnityEditor;
using UnityEngine;

namespace Growveld.Editor
{
    public static class Phase15QualityYieldSetup
    {
        private const string QualityPath = "Assets/_Project/ScriptableObjects/Plants/QualitySettings.asset";
        private const string PlantDefinitionPath = "Assets/_Project/ScriptableObjects/Plants/Plant_GenericCrop.asset";
        private const string PlantPrefabPath = "Assets/_Project/Prefabs/Plants/Generic Plant.prefab";

        [MenuItem("Growveld/Phase 15/Configure Quality and Yield")]
        public static void ConfigureQualityAndYield()
        {
            Growveld.Farming.QualitySettings quality = AssetDatabase.LoadAssetAtPath<Growveld.Farming.QualitySettings>(QualityPath);
            if (quality == null)
            {
                quality = ScriptableObject.CreateInstance<Growveld.Farming.QualitySettings>();
                AssetDatabase.CreateAsset(quality, QualityPath);
            }

            SerializedObject qualitySettings = new(quality);
            qualitySettings.FindProperty("standardThreshold").floatValue = 45f;
            qualitySettings.FindProperty("premiumThreshold").floatValue = 70f;
            qualitySettings.FindProperty("topGradeThreshold").floatValue = 88f;
            qualitySettings.FindProperty("lowMultiplier").floatValue = 0.7f;
            qualitySettings.FindProperty("standardMultiplier").floatValue = 1f;
            qualitySettings.FindProperty("premiumMultiplier").floatValue = 1.3f;
            qualitySettings.FindProperty("topGradeMultiplier").floatValue = 1.6f;
            qualitySettings.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(quality);

            PlantDefinition plantDefinition = AssetDatabase.LoadAssetAtPath<PlantDefinition>(PlantDefinitionPath);
            SerializedObject definitionSettings = new(plantDefinition);
            definitionSettings.FindProperty("qualitySettings").objectReferenceValue = quality;
            definitionSettings.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(plantDefinition);

            GameObject plantPrefab = PrefabUtility.LoadPrefabContents(PlantPrefabPath);
            PlantInstance plant = plantPrefab.GetComponent<PlantInstance>();
            SerializedObject plantSettings = new(plant);
            plantSettings.FindProperty("qualityScore").floatValue = 100f;
            plantSettings.FindProperty("yieldPotential").floatValue = 1f;
            plantSettings.FindProperty("accumulatedCareScore").floatValue = 0f;
            plantSettings.FindProperty("careSampleSeconds").floatValue = 0f;
            plantSettings.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(plantPrefab, PlantPrefabPath);
            PrefabUtility.UnloadPrefabContents(plantPrefab);

            AssetDatabase.SaveAssets();
            Debug.Log("Growveld Phase 15 setup complete: continuous care history, four quality grades, price multipliers, and yield potential configured.");
        }
    }
}
