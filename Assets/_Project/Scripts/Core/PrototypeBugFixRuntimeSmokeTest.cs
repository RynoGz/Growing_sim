using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Growveld.Building;
using Growveld.Economy;
using Growveld.Environment;
using Growveld.Farming;
using Growveld.Inventory;
using Growveld.Player;
using Growveld.Saving;
using Growveld.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Growveld.Core
{
    /// <summary>Standalone regression for input-mode, admin drying, and grow-light schedule fixes.</summary>
    public sealed class PrototypeBugFixRuntimeSmokeTest : MonoBehaviour
    {
        private const string CommandLineFlag = "--bugfix-smoke-test";
        private readonly List<string> failures = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (Application.isEditor || !System.Environment.GetCommandLineArgs().Contains(CommandLineFlag)) return;
            new GameObject("Bug Fix Runtime Smoke Test").AddComponent<PrototypeBugFixRuntimeSmokeTest>();
        }

        private IEnumerator Start()
        {
            for (int index = 0; index < 6; index++) yield return null;

            SaveSystem save = FindFirstObjectByType<SaveSystem>();
            string savePath = save != null ? save.SavePath : string.Empty;
            bool hadOriginalSave = !string.IsNullOrEmpty(savePath) && File.Exists(savePath);
            string originalSave = hadOriginalSave ? File.ReadAllText(savePath) : null;

            GameObject player = GameObject.FindWithTag("Player");
            ShopManager shop = FindFirstObjectByType<ShopManager>();
            GameTimeManager gameTime = FindFirstObjectByType<GameTimeManager>();
            UtilityManager utilities = FindFirstObjectByType<UtilityManager>();
            AdminConsole admin = FindFirstObjectByType<AdminConsole>();
            Require(save != null && player != null && shop != null && gameTime != null && utilities != null && admin != null,
                "required player, save, shop, time, utility, or admin service is missing");

            int originalDay = gameTime != null ? gameTime.Day : 1;
            float originalHour = gameTime != null ? gameTime.TimeOfDayHours : 7f;
            Vector3 originalPlayerPosition = player != null ? player.transform.position : Vector3.zero;
            Quaternion originalPlayerRotation = player != null ? player.transform.rotation : Quaternion.identity;

            if (failures.Count == 0)
            {
                yield return TestInputModes(player, admin);
                yield return TestDryingAndLightPersistence(shop, gameTime, utilities, admin, save);
            }

            if (hadOriginalSave && save != null)
            {
                File.WriteAllText(savePath, originalSave);
                save.LoadGame();
                for (int index = 0; index < 8; index++) yield return null;
                File.WriteAllText(savePath, originalSave);
            }
            else
            {
                gameTime?.RestoreTime(originalDay, originalHour);
                RestorePlayerTransform(player, originalPlayerPosition, originalPlayerRotation);
            }

            if (save != null) Destroy(save);
            yield return null;
            if (!hadOriginalSave && !string.IsNullOrEmpty(savePath) && File.Exists(savePath)) File.Delete(savePath);

            if (failures.Count == 0)
            {
                Debug.Log("Growveld bug-fix runtime smoke test passed: simultaneous movement/look, repeated gameplay/UI/construction transitions, multi-rack dryall and dried save/load persistence, and normal/ceiling grow-light schedule, placement, load, electricity, and coverage behaviour succeeded.");
                Application.Quit(0);
            }
            else
            {
                Debug.LogError("Growveld bug-fix runtime smoke test failed: " + string.Join("; ", failures));
                Application.Quit(1);
            }
        }

        private IEnumerator TestInputModes(GameObject player, AdminConsole admin)
        {
            FirstPersonController controller = player.GetComponent<FirstPersonController>();
            PlayerInput playerInput = player.GetComponent<PlayerInput>();
            PlayerInputStateController inputState = player.GetComponent<PlayerInputStateController>();
            BusinessTabletController tablet = player.GetComponent<BusinessTabletController>();
            PauseAndHelpController pause = player.GetComponent<PauseAndHelpController>();
            ConstructionModeController construction = player.GetComponent<ConstructionModeController>();

            Require(controller != null && playerInput != null && inputState != null && tablet != null && pause != null && construction != null,
                "input-mode components are missing");
            InputAction move = playerInput?.actions.FindAction("Player/Move");
            InputAction look = playerInput?.actions.FindAction("Player/Look");
            InputAction sprint = playerInput?.actions.FindAction("Player/Sprint");
            Require(move != null && look != null && sprint != null && move.actionMap == look.actionMap && move.actionMap == sprint.actionMap,
                "Move, Look, and Sprint are not independent actions in the same Player map");
            Require(move != null && move.bindings.Any(binding => binding.path == "<Keyboard>/w"), "Move action lacks the W binding");
            Require(look != null && look.bindings.Any(binding => binding.path == "<Pointer>/delta"), "Look action lacks pointer delta");

            if (controller != null)
            {
                MethodInfo applyLook = typeof(FirstPersonController).GetMethod("ApplyMouseLook", BindingFlags.Instance | BindingFlags.NonPublic);
                MethodInfo applyMove = typeof(FirstPersonController).GetMethod("ApplyMovement", BindingFlags.Instance | BindingFlags.NonPublic);
                Require(applyLook != null && applyMove != null, "independent movement/look processing methods are missing");
                Vector3 testOrigin = new(1800f, 8f, 1800f);
                RestorePlayerTransform(player, testOrigin, Quaternion.identity);
                Vector2[] movementCases =
                {
                    new(0f, 1f), new(0f, -1f), new(-1f, 0f), new(1f, 0f), new(-1f, 1f), new(1f, 1f)
                };
                for (int index = 0; index < movementCases.Length; index++)
                {
                    float yawBefore = player.transform.eulerAngles.y;
                    Vector3 positionBefore = player.transform.position;
                    applyLook?.Invoke(controller, new object[] { new Vector2(12f, 3f) });
                    applyMove?.Invoke(controller, new object[] { movementCases[index], index == movementCases.Length - 1, 0.1f });
                    Require(Mathf.Abs(Mathf.DeltaAngle(yawBefore, player.transform.eulerAngles.y)) > 0.01f,
                        $"mouse look stopped for movement case {movementCases[index]}");
                    Require(Vector3.Distance(positionBefore, player.transform.position) > 0.001f,
                        $"movement stopped for movement case {movementCases[index]}");
                }
            }

            for (int repetition = 0; repetition < 4; repetition++)
            {
                tablet.SetOpen(true);
                Require(inputState.CurrentMode == PlayerInputMode.Tablet && !inputState.AllowsMovementAndLook && !inputState.WantsLockedCursor,
                    "tablet did not enter modal input mode");
                tablet.SetOpen(false);
                Require(inputState.CurrentMode == PlayerInputMode.Gameplay && inputState.AllowsMovementAndLook && inputState.WantsLockedCursor,
                    "tablet close did not restore gameplay input");

                pause.ShowControls();
                Require(inputState.CurrentMode == PlayerInputMode.Paused && !inputState.AllowsMovementAndLook,
                    "pause did not suspend gameplay input");
                pause.Resume();
                Require(inputState.CurrentMode == PlayerInputMode.Gameplay && inputState.AllowsMovementAndLook,
                    "unpause did not restore gameplay input");

                construction.EnterMode();
                Require(inputState.CurrentMode == PlayerInputMode.Construction && inputState.AllowsMovementAndLook && inputState.WantsLockedCursor,
                    "construction mode blocked movement/look");
                inputState.SetPlacementActive(true);
                Require(inputState.CurrentMode == PlayerInputMode.Placement && inputState.AllowsMovementAndLook,
                    "placement mode blocked movement/look");
                inputState.SetPlacementActive(false);
                construction.ExitMode();
                Require(inputState.CurrentMode == PlayerInputMode.Gameplay && inputState.AllowsMovementAndLook,
                    "construction exit did not restore gameplay input");
            }

            admin.SetOpen(true);
            Require(inputState.CurrentMode == PlayerInputMode.AdminConsole && !inputState.AllowsMovementAndLook,
                "admin console did not enter modal input mode");
            admin.SetOpen(false);
            Require(inputState.CurrentMode == PlayerInputMode.Gameplay && inputState.AllowsMovementAndLook,
                "admin console close did not restore gameplay input");
            yield return null;
        }

        private IEnumerator TestDryingAndLightPersistence(
            ShopManager shop,
            GameTimeManager gameTime,
            UtilityManager utilities,
            AdminConsole admin,
            SaveSystem save)
        {
            ItemDefinition basicRackItem = FindItem(shop, "drying_rack");
            ItemDefinition largeRackItem = FindItem(shop, "large_drying_rack");
            ItemDefinition floorLightItem = FindItem(shop, "grow_light");
            ItemDefinition ceilingLightItem = FindItem(shop, "ceiling_grow_light");
            ItemDefinition roomItem = FindItem(shop, "grow_room");
            PlantDefinition[] strains = shop.AvailableItems
                .Where(item => item?.StrainDefinition != null)
                .Select(item => item.StrainDefinition)
                .Distinct()
                .ToArray();
            Require(basicRackItem?.PlaceableDefinition?.Prefab != null && largeRackItem?.PlaceableDefinition?.Prefab != null,
                "basic or large drying-rack prefab is missing");
            Require(floorLightItem?.PlaceableDefinition?.Prefab != null && ceilingLightItem?.PlaceableDefinition?.Prefab != null,
                "normal or ceiling grow-light prefab is missing");
            Require(roomItem?.PlaceableDefinition?.Prefab != null && strains.Length >= 4, "grow-room or strain test data is missing");
            if (failures.Count > 0) yield break;

            GameObject basicRackObject = Instantiate(basicRackItem.PlaceableDefinition.Prefab, new Vector3(1900f, 0f, 1900f), Quaternion.identity);
            GameObject largeRackObject = Instantiate(largeRackItem.PlaceableDefinition.Prefab, new Vector3(1904f, 0f, 1900f), Quaternion.identity);
            DryingRack basicRack = basicRackObject.GetComponent<DryingRack>();
            DryingRack largeRack = largeRackObject.GetComponent<DryingRack>();
            string basicRackId = basicRackObject.GetComponent<PlacedObject>().PersistentId;
            string largeRackId = largeRackObject.GetComponent<PlacedObject>().PersistentId;

            HarvestBatch first = CreateBatch("dry-test-one", 1.25f, QualityGrade.Low, strains[0]);
            HarvestBatch second = CreateBatch("dry-test-two", 2.5f, QualityGrade.Premium, strains[1]);
            HarvestBatch third = CreateBatch("dry-test-three", 0.8f, QualityGrade.TopGrade, strains[3]);
            Require(basicRack.RestoreBatchAtSlot(0, first, 120f), "could not add partial batch to basic rack");
            Require(basicRack.RestoreBatchAtSlot(1, second, 1f), "could not add almost-dried batch to basic rack");
            Require(largeRack.RestoreBatchAtSlot(0, third, 300f), "could not add batch to large rack");

            string dryResult = admin.ExecuteAdminCommand("dryall");
            Require(dryResult == "Admin: All drying rack batches have been fully dried.", "dryall success message is incorrect");
            Require(new[] { first, second, third }.All(batch => batch.Status == HarvestStatus.Dried),
                "dryall did not complete every batch across basic and large racks");
            Require(first.Strain == strains[0] && Mathf.Approximately(first.WeightKilograms, 1.25f) && first.QualityGrade == QualityGrade.Low,
                "dryall changed first batch production data");
            Require(second.Strain == strains[1] && Mathf.Approximately(second.WeightKilograms, 2.5f) && second.QualityGrade == QualityGrade.Premium,
                "dryall changed second batch production data");
            Require(third.Strain == strains[3] && Mathf.Approximately(third.WeightKilograms, 0.8f) && third.QualityGrade == QualityGrade.TopGrade,
                "dryall changed third batch production data");
            Require(admin.ExecuteAdminCommand("dryallracks") == "Admin: No active drying batches found.",
                "empty dryall command did not report no active batches");

            gameTime.RestoreTime(gameTime.Day, 12f);
            GameObject roomObject = Instantiate(roomItem.PlaceableDefinition.Prefab, new Vector3(1950f, 0f, 1950f), Quaternion.identity);
            GameObject floorLightObject = Instantiate(floorLightItem.PlaceableDefinition.Prefab, new Vector3(1950f, 0f, 1950f), Quaternion.identity);
            GameObject ceilingLightObject = Instantiate(ceilingLightItem.PlaceableDefinition.Prefab, new Vector3(1952f, 3.05f, 1950f), Quaternion.identity);
            GrowLight floorLight = floorLightObject.GetComponent<GrowLight>();
            GrowLight ceilingLight = ceilingLightObject.GetComponent<GrowLight>();
            string floorLightId = floorLightObject.GetComponent<PlacedObject>().PersistentId;
            string ceilingLightId = ceilingLightObject.GetComponent<PlacedObject>().PersistentId;
            Require(floorLight.IsActive && floorLight.LightSource.enabled, "new normal grow light did not immediately follow the ON schedule");
            Require(ceilingLight.IsActive && ceilingLight.LightSource.enabled, "new ceiling grow light did not immediately follow the ON schedule");
            ValidateLightConfiguration(floorLight, "normal grow light");
            ValidateLightConfiguration(ceilingLight, "ceiling grow light");

            float electricityBefore = utilities.CurrentElectricityKilowattHours;
            for (int index = 0; index < 3; index++) yield return null;
            Require(utilities.CurrentElectricityKilowattHours > electricityBefore,
                "scheduled-on grow lights did not contribute electricity usage");
            GrowRoomEnvironment room = roomObject.GetComponent<GrowRoomEnvironment>();
            Require(room != null && GrowLight.FindCoveringLight(new Vector3(1951f, 1f, 1950f), room) != null,
                "scheduled-on grow lights did not provide plant coverage");

            gameTime.RestoreTime(gameTime.Day, 23f);
            Require(!floorLight.IsActive && !floorLight.LightSource.enabled && !ceilingLight.IsActive && !ceilingLight.LightSource.enabled,
                "grow lights did not turn off at the OFF schedule");
            Require(GrowLight.FindCoveringLight(new Vector3(1951f, 1f, 1950f), room) == null,
                "scheduled-off grow lights still provided plant coverage");

            gameTime.RestoreTime(gameTime.Day, 12f);
            Require(save.SaveGame(false), "could not save dried batches and scheduled-on lights");
            save.LoadGame();
            for (int index = 0; index < 10; index++) yield return null;
            RequireRestoredDriedRack(basicRackId, new[] { "dry-test-one", "dry-test-two" });
            RequireRestoredDriedRack(largeRackId, new[] { "dry-test-three" });
            GrowLight restoredFloor = FindPlacedComponent<GrowLight>(floorLightId);
            GrowLight restoredCeiling = FindPlacedComponent<GrowLight>(ceilingLightId);
            Require(restoredFloor != null && restoredFloor.IsActive && restoredFloor.LightSource.enabled,
                "loaded normal grow light did not immediately restore the ON schedule");
            Require(restoredCeiling != null && restoredCeiling.IsActive && restoredCeiling.LightSource.enabled,
                "loaded ceiling grow light did not immediately restore the ON schedule");

            gameTime.RestoreTime(gameTime.Day, 23f);
            Require(save.SaveGame(false), "could not save scheduled-off lights");
            save.LoadGame();
            for (int index = 0; index < 10; index++) yield return null;
            restoredFloor = FindPlacedComponent<GrowLight>(floorLightId);
            restoredCeiling = FindPlacedComponent<GrowLight>(ceilingLightId);
            Require(restoredFloor != null && !restoredFloor.IsActive && !restoredFloor.LightSource.enabled,
                "loaded normal grow light did not immediately restore the OFF schedule");
            Require(restoredCeiling != null && !restoredCeiling.IsActive && !restoredCeiling.LightSource.enabled,
                "loaded ceiling grow light did not immediately restore the OFF schedule");
        }

        private void ValidateLightConfiguration(GrowLight light, string label)
        {
            Require(light != null && Mathf.Approximately(light.CoverageRadius, GrowLight.SharedCoverageRadius), $"{label} coverage changed from 6");
            Require(light != null && Mathf.Approximately(light.VisualIntensity, GrowLight.SharedVisualIntensity), $"{label} visual intensity changed");
            Require(light != null && Mathf.Approximately(light.VisualRange, GrowLight.SharedVisualRange), $"{label} visual range changed");
            Require(light != null && Vector4.Distance(light.VisualColor, GrowLight.SharedVisualColor) < 0.01f, $"{label} visual colour changed");
        }

        private void RequireRestoredDriedRack(string persistentId, IReadOnlyCollection<string> batchIds)
        {
            DryingRack rack = FindPlacedComponent<DryingRack>(persistentId);
            Require(rack != null, $"saved drying rack {persistentId} was not restored");
            if (rack == null) return;
            HarvestBatch[] batches = rack.Slots.Where(slot => !slot.IsEmpty).Select(slot => slot.Batch).ToArray();
            Require(batchIds.All(id => batches.Any(batch => batch.BatchId == id && batch.Status == HarvestStatus.Dried)),
                $"rack {persistentId} did not retain dried collectible batches after load");
            Require(rack.Slots.Where(slot => !slot.IsEmpty).All(slot => Mathf.Approximately(slot.RemainingSeconds, 0f)),
                $"rack {persistentId} restarted a dried timer after load");
        }

        private static T FindPlacedComponent<T>(string persistentId) where T : Component
        {
            return FindObjectsByType<PlacedObject>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .FirstOrDefault(placed => placed.PersistentId == persistentId)
                ?.GetComponent<T>();
        }

        private static HarvestBatch CreateBatch(string id, float weight, QualityGrade grade, PlantDefinition strain)
        {
            GameObject batchObject = new($"Admin Dry Test {id}", typeof(Rigidbody), typeof(HarvestBatch));
            HarvestBatch batch = batchObject.GetComponent<HarvestBatch>();
            batch.RestoreBatch(id, weight, grade, strain, HarvestStatus.Fresh);
            return batch;
        }

        private static ItemDefinition FindItem(ShopManager shop, string itemId)
        {
            return shop?.AvailableItems?.FirstOrDefault(item => item != null && item.ItemId == itemId);
        }

        private static void RestorePlayerTransform(GameObject player, Vector3 position, Quaternion rotation)
        {
            if (player == null) return;
            CharacterController character = player.GetComponent<CharacterController>();
            if (character != null) character.enabled = false;
            player.transform.SetPositionAndRotation(position, rotation);
            if (character != null) character.enabled = true;
        }

        private void Require(bool condition, string message)
        {
            if (!condition) failures.Add(message);
        }
    }
}
