using Growveld.Farming;
using UnityEditor;
using UnityEngine;

namespace Growveld.Editor
{
    public static class Phase11PlantCareSetup
    {
        private const string PlantDefinitionPath = "Assets/_Project/ScriptableObjects/Plants/Plant_GenericCrop.asset";
        private const string PlantPrefabPath = "Assets/_Project/Prefabs/Plants/Generic Plant.prefab";

        [MenuItem("Growveld/Phase 11/Configure Water and Nutrients")]
        public static void ConfigurePlantCare()
        {
            PlantDefinition definition = AssetDatabase.LoadAssetAtPath<PlantDefinition>(PlantDefinitionPath);
            if (definition == null) throw new MissingReferenceException("Generic plant definition was not found.");

            SerializedObject definitionSettings = new(definition);
            definitionSettings.FindProperty("maximumWater").floatValue = 100f;
            definitionSettings.FindProperty("maximumNutrients").floatValue = 100f;
            definitionSettings.FindProperty("waterConsumptionPerRealMinute").floatValue = 4f;
            definitionSettings.FindProperty("nutrientConsumptionPerRealMinute").floatValue = 2f;
            definitionSettings.FindProperty("waterPerUse").floatValue = 45f;
            definitionSettings.FindProperty("nutrientsPerDose").floatValue = 35f;
            definitionSettings.FindProperty("healthLossPerCriticalMinute").floatValue = 8f;
            definitionSettings.FindProperty("healthRecoveryPerGoodMinute").floatValue = 2f;
            definitionSettings.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(definition);

            GameObject plantPrefab = PrefabUtility.LoadPrefabContents(PlantPrefabPath);
            PlantInstance plant = plantPrefab.GetComponent<PlantInstance>();
            SerializedObject plantSettings = new(plant);
            plantSettings.FindProperty("waterLevel").floatValue = 100f;
            plantSettings.FindProperty("nutrientLevel").floatValue = 100f;
            plantSettings.FindProperty("health").floatValue = 100f;
            plantSettings.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(plantPrefab, PlantPrefabPath);
            PrefabUtility.UnloadPrefabContents(plantPrefab);

            AssetDatabase.SaveAssets();
            Debug.Log("Growveld Phase 11 setup complete: water and nutrient consumption, manual care interactions, health, and growth penalties configured.");
        }
    }
}
