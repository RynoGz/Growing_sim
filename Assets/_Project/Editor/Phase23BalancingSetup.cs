using System;
using System.Collections.Generic;
using System.Linq;
using Growveld.Building;
using Growveld.Core;
using Growveld.Economy;
using Growveld.Environment;
using Growveld.Farming;
using Growveld.Inventory;
using Growveld.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Growveld.Editor
{
    public static class Phase23BalancingSetup
    {
        private const string ScenePath = "Assets/_Project/Scenes/PrototypeFarm.unity";
        private const string PlantPath = "Assets/_Project/ScriptableObjects/Plants/Plant_GenericCrop.asset";
        private const string QualityPath = "Assets/_Project/ScriptableObjects/Plants/QualitySettings.asset";
        private const string SellingPath = "Assets/_Project/ScriptableObjects/Economy/SellingSettings.asset";
        private const string UtilityPath = "Assets/_Project/ScriptableObjects/Economy/UtilitySettings.asset";
        private const string GrowLightPrefabPath = "Assets/_Project/Prefabs/Equipment/Grow Light.prefab";
        private const string DryingRackPrefabPath = "Assets/_Project/Prefabs/Equipment/Drying Rack.prefab";

        private static readonly Dictionary<string, float> ItemPrices = new()
        {
            ["seed"] = 120f,
            ["nutrients"] = 90f,
            ["watering_can"] = 450f,
            ["grow_pot"] = 650f,
            ["grow_light"] = 3200f,
            ["drying_rack"] = 4800f,
            ["storage_bin"] = 2200f,
            ["grow_room"] = 18000f
        };

        [MenuItem("Growveld/Phase 23/Apply Prototype Balance")]
        public static void ConfigureBalance()
        {
            ItemDefinition[] items = LoadAssets<ItemDefinition>("Assets/_Project/ScriptableObjects/Items");
            foreach (ItemDefinition item in items)
            {
                if (!ItemPrices.TryGetValue(item.ItemId, out float price)) continue;
                SetFloat(item, "purchasePrice", price);
            }

            PlantDefinition plant = AssetDatabase.LoadAssetAtPath<PlantDefinition>(PlantPath);
            SerializedObject plantSettings = new(plant);
            plantSettings.FindProperty("germinationSeconds").floatValue = 120f;
            plantSettings.FindProperty("seedlingSeconds").floatValue = 240f;
            plantSettings.FindProperty("vegetativeSeconds").floatValue = 540f;
            plantSettings.FindProperty("floweringSeconds").floatValue = 900f;
            plantSettings.FindProperty("baseYieldKilograms").floatValue = 0.65f;
            plantSettings.FindProperty("waterConsumptionPerRealMinute").floatValue = 4f;
            plantSettings.FindProperty("nutrientConsumptionPerRealMinute").floatValue = 2f;
            plantSettings.FindProperty("waterPerUse").floatValue = 45f;
            plantSettings.FindProperty("nutrientsPerDose").floatValue = 35f;
            plantSettings.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(plant);

            Growveld.Farming.QualitySettings quality = AssetDatabase.LoadAssetAtPath<Growveld.Farming.QualitySettings>(QualityPath);
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

            SellingSettings selling = AssetDatabase.LoadAssetAtPath<SellingSettings>(SellingPath);
            SetFloat(selling, "basePricePerKilogram", 1000f);
            UtilitySettings utilities = AssetDatabase.LoadAssetAtPath<UtilitySettings>(UtilityPath);
            SerializedObject utilitySettings = new(utilities);
            utilitySettings.FindProperty("electricityRandPerKilowattHour").floatValue = 3.25f;
            utilitySettings.FindProperty("waterLitresPerWatering").floatValue = 15f;
            utilitySettings.FindProperty("waterRandPerLitre").floatValue = 0.06f;
            utilitySettings.FindProperty("fallbackDayRealSeconds").floatValue = 1800f;
            utilitySettings.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(utilities);

            GrowLight growLight = AssetDatabase.LoadAssetAtPath<GameObject>(GrowLightPrefabPath)?.GetComponent<GrowLight>();
            if (growLight != null)
            {
                SerializedObject lightSettings = new(growLight);
                lightSettings.FindProperty("coverageRadius").floatValue = 3.5f;
                lightSettings.FindProperty("powerConsumptionKilowatts").floatValue = 1.2f;
                lightSettings.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(growLight);
            }
            DryingRack dryingRack = AssetDatabase.LoadAssetAtPath<GameObject>(DryingRackPrefabPath)?.GetComponent<DryingRack>();
            if (dryingRack != null) SetFloat(dryingRack, "dryingDurationSeconds", 600f);

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject systems = FindRoot(scene, "Game Systems");
            EconomyManager economy = systems.GetComponent<EconomyManager>();
            SerializedObject economySettings = new(economy);
            economySettings.FindProperty("startingBalance").floatValue = 30000f;
            economySettings.FindProperty("balance").floatValue = 0f;
            economySettings.FindProperty("initialiseOnAwake").boolValue = true;
            economySettings.ApplyModifiedPropertiesWithoutUndo();

            UtilityManager utilityManager = systems.GetComponent<UtilityManager>();
            SerializedObject utilityManagerSettings = new(utilityManager);
            utilityManagerSettings.FindProperty("gameTime").objectReferenceValue = systems.GetComponent<GameTimeManager>();
            utilityManagerSettings.ApplyModifiedPropertiesWithoutUndo();

            foreach (LandPlot plot in UnityEngine.Object.FindObjectsByType<LandPlot>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                SerializedObject plotSettings = new(plot);
                if (plot.PlotId == "east") plotSettings.FindProperty("purchasePrice").floatValue = 15000f;
                if (plot.PlotId == "north") plotSettings.FindProperty("purchasePrice").floatValue = 18000f;
                plotSettings.ApplyModifiedPropertiesWithoutUndo();
            }

            ConfigureFinances(scene, economy, plant, quality, selling, utilities, items);
            ValidateBalance(items, plant, quality, selling, utilities);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = systems;
            Debug.Log("Growveld Phase 23 setup complete: startup capital, prices, growth, yield, utilities, quality premiums, land, and indoor/outdoor profitability balanced and validated.");
        }

        private static void ConfigureFinances(Scene scene, EconomyManager economy, PlantDefinition plant, Growveld.Farming.QualitySettings quality, SellingSettings selling, UtilitySettings utilities, ItemDefinition[] items)
        {
            GameObject tabletCanvas = FindRoot(scene, "Business Tablet UI");
            Transform finances = tabletCanvas != null ? tabletCanvas.transform.Find("Tablet/Content/Finances") : null;
            if (finances == null) throw new MissingReferenceException("Business tablet Finances section is required.");
            for (int index = finances.childCount - 1; index >= 0; index--) UnityEngine.Object.DestroyImmediate(finances.GetChild(index).gameObject);

            BalanceOverviewUI ui = finances.GetComponent<BalanceOverviewUI>() ?? finances.gameObject.AddComponent<BalanceOverviewUI>();
            GameObject textObject = new("Balance Summary", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            textObject.transform.SetParent(finances, false);
            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(22f, 18f);
            rect.offsetMax = new Vector2(-22f, -18f);
            Text text = textObject.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 22;
            text.alignment = TextAnchor.UpperLeft;
            text.color = Color.white;

            SerializedObject uiSettings = new(ui);
            uiSettings.FindProperty("economy").objectReferenceValue = economy;
            uiSettings.FindProperty("plant").objectReferenceValue = plant;
            uiSettings.FindProperty("quality").objectReferenceValue = quality;
            uiSettings.FindProperty("selling").objectReferenceValue = selling;
            uiSettings.FindProperty("utilities").objectReferenceValue = utilities;
            SerializedProperty itemArray = uiSettings.FindProperty("items");
            itemArray.arraySize = items.Length;
            for (int index = 0; index < items.Length; index++) itemArray.GetArrayElementAtIndex(index).objectReferenceValue = items[index];
            uiSettings.FindProperty("summaryText").objectReferenceValue = text;
            uiSettings.FindProperty("growLightKilowatts").floatValue = 1.2f;
            uiSettings.FindProperty("scheduledLightHours").floatValue = 18f;
            uiSettings.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ValidateBalance(ItemDefinition[] items, PlantDefinition plant, Growveld.Farming.QualitySettings quality, SellingSettings selling, UtilitySettings utilities)
        {
            float Price(string id) => items.First(item => item.ItemId == id).PurchasePrice;
            float outdoorKit = Price("seed") + Price("nutrients") + Price("watering_can") + Price("grow_pot") + Price("drying_rack") + Price("storage_bin");
            float indoorKit = outdoorKit + Price("grow_light") + Price("grow_room");
            float topSale = plant.BaseYieldKilograms * 1.12f * selling.BasePricePerKilogram * quality.GetPriceMultiplier(QualityGrade.TopGrade);
            float recurringCost = Price("seed") + Price("nutrients") + utilities.WaterLitresPerWatering * utilities.WaterRandPerLitre + 1.2f * 18f * utilities.ElectricityRandPerKilowattHour;
            if (indoorKit > 30000f || indoorKit < 28000f) throw new InvalidOperationException($"Indoor starter kit is outside the intended range: R{indoorKit:N0}.");
            if (outdoorKit >= 15000f) throw new InvalidOperationException($"Outdoor starter kit is too expensive: R{outdoorKit:N0}.");
            if (topSale <= recurringCost * 2f) throw new InvalidOperationException("A well-managed crop is not sufficiently profitable.");
            if (!Mathf.Approximately(plant.TotalGrowthSeconds, 1800f)) throw new InvalidOperationException("Plant growth is not 30 real minutes.");
            Debug.Log($"Phase 23 balance check passed: outdoor kit R{outdoorKit:N0}, indoor kit R{indoorKit:N0}, ideal top-grade sale R{topSale:N0}, estimated recurring cost R{recurringCost:N0}, one-light daily power R{1.2f * 18f * utilities.ElectricityRandPerKilowattHour:N2}.");
        }

        private static T[] LoadAssets<T>(string folder) where T : UnityEngine.Object
        {
            return AssetDatabase.FindAssets($"t:{typeof(T).Name}", new[] { folder })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<T>)
                .Where(asset => asset != null)
                .OrderBy(asset => asset.name, StringComparer.Ordinal)
                .ToArray();
        }

        private static void SetFloat(UnityEngine.Object target, string propertyName, float value)
        {
            SerializedObject settings = new(target);
            settings.FindProperty(propertyName).floatValue = value;
            settings.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private static GameObject FindRoot(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects()) if (root.name == name) return root;
            return null;
        }
    }
}
