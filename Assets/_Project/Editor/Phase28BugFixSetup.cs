using System;
using System.Collections.Generic;
using Growveld.Building;
using Growveld.Core;
using Growveld.Environment;
using Growveld.Player;
using Growveld.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Growveld.Editor
{
    public static class Phase28BugFixSetup
    {
        private const string ScenePath = "Assets/_Project/Scenes/PrototypeFarm.unity";
        private const string InputActionsPath = "Assets/InputSystem_Actions.inputactions";
        private const string FloorLightPrefabPath = "Assets/_Project/Prefabs/Equipment/Grow Light.prefab";
        private const string CeilingLightPrefabPath = "Assets/_Project/Prefabs/Equipment/Ceiling Grow Light.prefab";

        [MenuItem("Growveld/Phase 28/Apply Input, Drying and Grow-Light Fixes")]
        public static void ApplyFixes()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject player = GameObject.FindWithTag("Player");
            if (player == null) throw new MissingReferenceException("Player is required.");

            PlayerInputStateController inputState = player.GetComponent<PlayerInputStateController>()
                ?? player.AddComponent<PlayerInputStateController>();
            AssignObjectReference(player.GetComponent<BusinessTabletController>(), "inputState", inputState);
            AssignObjectReference(player.GetComponent<PauseAndHelpController>(), "inputState", inputState);
            AssignObjectReference(player.GetComponent<ConstructionModeController>(), "inputState", inputState);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            ValidateFixes();
            Debug.Log("Growveld Phase 28 setup complete: centralized input/cursor state, dryall admin command, and direct grow-light game-clock scheduling are configured.");
        }

        [MenuItem("Growveld/Phase 28/Validate Input, Drying and Grow-Light Fixes")]
        public static void ValidateFixes()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            List<string> failures = new();
            GameObject player = GameObject.FindWithTag("Player");
            if (player == null)
            {
                failures.Add("Player is missing");
            }
            else
            {
                if (player.GetComponents<PlayerInputStateController>().Length != 1) failures.Add("Player does not have exactly one input-state coordinator");
                if (player.GetComponents<PlayerInput>().Length != 1) failures.Add("Player does not have exactly one PlayerInput");
                PlayerInput playerInput = player.GetComponent<PlayerInput>();
                InputAction move = playerInput?.actions?.FindAction("Player/Move");
                InputAction look = playerInput?.actions?.FindAction("Player/Look");
                InputAction sprint = playerInput?.actions?.FindAction("Player/Sprint");
                if (move == null || look == null || sprint == null || move.actionMap != look.actionMap || move.actionMap != sprint.actionMap)
                    failures.Add("Move, Look, and Sprint are not independent actions in one Player action map");
            }

            InputActionAsset actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);
            if (actions == null || actions.FindAction("Player/Look") == null || actions.FindAction("Player/Move") == null)
                failures.Add("Player input action asset is missing Move or Look");

            ValidateLightPrefab(FloorLightPrefabPath, "normal grow light", failures);
            ValidateLightPrefab(CeilingLightPrefabPath, "ceiling grow light", failures);
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                int missing = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(root);
                if (missing > 0) failures.Add($"{missing} missing script(s) under {root.name}");
            }

            if (failures.Count > 0) throw new InvalidOperationException("Phase 28 validation failed:\n- " + string.Join("\n- ", failures));
            Debug.Log("Growveld Phase 28 validation passed: one player input owner, independent Move/Look/Sprint actions, both shared scheduled grow-light prefabs at 6 coverage with preserved visuals, and no missing scripts.");
        }

        private static void ValidateLightPrefab(string path, string label, List<string> failures)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            GrowLight growLight = prefab != null ? prefab.GetComponent<GrowLight>() : null;
            if (growLight == null || growLight.LightSource == null)
            {
                failures.Add($"{label} GrowLight or Light reference is missing");
                return;
            }

            SerializedObject serialized = new(growLight);
            if (!serialized.FindProperty("automaticSchedule").boolValue) failures.Add($"{label} automatic schedule is disabled");
            if (!Mathf.Approximately(growLight.CoverageRadius, GrowLight.SharedCoverageRadius)) failures.Add($"{label} coverage is not 6");
            if (!Mathf.Approximately(growLight.VisualIntensity, GrowLight.SharedVisualIntensity)) failures.Add($"{label} visual intensity changed");
            if (!Mathf.Approximately(growLight.VisualRange, GrowLight.SharedVisualRange)) failures.Add($"{label} visual range changed");
            if (Vector4.Distance(growLight.VisualColor, GrowLight.SharedVisualColor) > 0.01f) failures.Add($"{label} visual colour changed");
            if (!growLight.LightSource.gameObject.activeSelf) failures.Add($"{label} Light GameObject is inactive");
        }

        private static void AssignObjectReference(UnityEngine.Object target, string propertyName, UnityEngine.Object value)
        {
            if (target == null) return;
            SerializedObject serialized = new(target);
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null) throw new MissingFieldException(target.GetType().Name, propertyName);
            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }
    }
}
