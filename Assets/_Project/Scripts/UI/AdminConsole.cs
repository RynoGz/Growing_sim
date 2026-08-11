using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Growveld.Building;
using Growveld.Core;
using Growveld.Economy;
using Growveld.Farming;
using Growveld.Interaction;
using Growveld.Inventory;
using Growveld.Player;
using Growveld.Saving;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Growveld.UI
{
    /// <summary>
    /// Lightweight in-game administration console for prototype testing.
    /// </summary>
    public sealed class AdminConsole : MonoBehaviour
    {
        private const int MaximumOutputLines = 12;
        private const float MaximumMoneyAmount = 1000000000f;

        private readonly Queue<string> outputLines = new();
        private readonly List<BehaviourState> suspendedBehaviours = new();

        private GameObject consoleRoot;
        private InputField commandInput;
        private Text outputText;
        private bool isOpen;
        private float previousTimeScale = 1f;

        public bool IsOpen => isOpen;

        private readonly struct BehaviourState
        {
            public BehaviourState(Behaviour behaviour)
            {
                Behaviour = behaviour;
                WasEnabled = behaviour != null && behaviour.enabled;
            }

            public Behaviour Behaviour { get; }
            public bool WasEnabled { get; }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (FindFirstObjectByType<AdminConsole>(FindObjectsInactive.Include) != null) return;
            new GameObject("Admin Console").AddComponent<AdminConsole>();
        }

        private void Awake()
        {
            BuildInterface();
            consoleRoot.SetActive(false);
        }

        private IEnumerator Start()
        {
            if (!System.Environment.GetCommandLineArgs().Contains("--admin-smoke-test")) yield break;

            yield return null;
            yield return null;
            yield return RunCommandSmokeTest();
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return;

            if (keyboard.f2Key.wasPressedThisFrame || keyboard.backquoteKey.wasPressedThisFrame)
            {
                SetOpen(!isOpen);
                return;
            }

            if (isOpen && keyboard.escapeKey.wasPressedThisFrame)
            {
                SetOpen(false);
            }
        }

        private void OnDestroy()
        {
            if (isOpen) RestoreGameplay();
        }

        public void SetOpen(bool open)
        {
            if (open == isOpen) return;
            isOpen = open;

            if (open)
            {
                PauseAndHelpController pause = FindFirstObjectByType<PauseAndHelpController>();
                if (pause != null && pause.IsPaused) pause.Resume();
                FindFirstObjectByType<BusinessTabletController>()?.SetOpen(false);
                FindFirstObjectByType<PlacementController>()?.CancelPlacement();

                previousTimeScale = Time.timeScale;
                Time.timeScale = 0f;
                SuspendGameplay();
                consoleRoot.SetActive(true);
                commandInput.text = string.Empty;
                commandInput.ActivateInputField();
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                consoleRoot.SetActive(false);
                RestoreGameplay();
            }
        }

        private void SuspendGameplay()
        {
            suspendedBehaviours.Clear();
            Suspend(FindFirstObjectByType<FirstPersonController>());
            Suspend(FindFirstObjectByType<PlayerInteractor>());
            Suspend(FindFirstObjectByType<InventoryHotbarInput>());
            Suspend(FindFirstObjectByType<PlacementController>());
            Suspend(FindFirstObjectByType<ConstructionModeController>());
            Suspend(FindFirstObjectByType<BusinessTabletController>());
            Suspend(FindFirstObjectByType<PauseAndHelpController>());
        }

        private void Suspend(Behaviour behaviour)
        {
            if (behaviour == null) return;
            suspendedBehaviours.Add(new BehaviourState(behaviour));
            behaviour.enabled = false;
        }

        private void RestoreGameplay()
        {
            Time.timeScale = previousTimeScale;
            foreach (BehaviourState state in suspendedBehaviours)
            {
                if (state.Behaviour != null) state.Behaviour.enabled = state.WasEnabled;
            }

            suspendedBehaviours.Clear();
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void SubmitCommand(string rawCommand)
        {
            if (!isOpen || string.IsNullOrWhiteSpace(rawCommand))
            {
                ReactivateInput();
                return;
            }

            string trimmedCommand = rawCommand.Trim();
            AppendOutput($"> {trimmedCommand}");
            string result = ExecuteCommand(trimmedCommand);
            AppendOutput(result);
            Debug.Log($"Growveld admin command '{trimmedCommand}': {result}");
            ReactivateInput();
        }

        private void ReactivateInput()
        {
            if (commandInput == null) return;
            commandInput.text = string.Empty;
            commandInput.ActivateInputField();
            commandInput.Select();
        }

        private string ExecuteCommand(string rawCommand)
        {
            string[] arguments = rawCommand.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (arguments.Length == 0) return "Enter a command, or type help.";

            switch (arguments[0].ToLowerInvariant())
            {
                case "help":
                    return "grow | time <hour or HH:MM> | money <amount> | setmoney <amount> | save | load | clear";
                case "grow":
                case "growall":
                    return GrowAllPlants();
                case "time":
                    return SetTime(arguments);
                case "money":
                    return AddMoney(arguments);
                case "setmoney":
                    return SetMoney(arguments);
                case "save":
                    return SaveGame();
                case "load":
                    return LoadGame();
                case "clear":
                    outputLines.Clear();
                    outputText.text = string.Empty;
                    return "Console cleared.";
                default:
                    return $"Unknown command '{arguments[0]}'. Type help for commands.";
            }
        }

        public string ExecuteAdminCommand(string rawCommand)
        {
            return string.IsNullOrWhiteSpace(rawCommand)
                ? "Enter a command, or type help."
                : ExecuteCommand(rawCommand.Trim());
        }

        private IEnumerator RunCommandSmokeTest()
        {
            EconomyManager economy = FindFirstObjectByType<EconomyManager>();
            GameTimeManager gameTime = FindFirstObjectByType<GameTimeManager>();
            if (economy == null || gameTime == null)
            {
                Debug.LogError("Growveld admin console runtime smoke test failed: required managers were not found.");
                Application.Quit(1);
                yield break;
            }

            float originalBalance = economy.Balance;
            int originalDay = gameTime.Day;
            float originalHour = gameTime.TimeOfDayHours;
            PlantingContainer temporaryContainer = null;
            PlantInstance temporaryPlant = null;
            PlantInstance[] plants = FindObjectsByType<PlantInstance>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            if (plants.Length == 0)
            {
                temporaryContainer = FindObjectsByType<PlantingContainer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                    .FirstOrDefault(container => container.CurrentPlant == null);
                temporaryPlant = temporaryContainer?.SpawnRestoredPlant();
                plants = FindObjectsByType<PlantInstance>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            }

            Dictionary<PlantInstance, float> originalGrowth = plants.ToDictionary(plant => plant, plant => plant.ElapsedGrowthSeconds);
            string failure = null;
            try
            {
                SetOpen(true);
                if (!IsOpen || Time.timeScale != 0f) throw new InvalidOperationException("console toggle failed");
                SetOpen(false);

                ExecuteAdminCommand("money 1234");
                if (!Mathf.Approximately(economy.Balance, originalBalance + 1234f)) throw new InvalidOperationException("money command failed");

                ExecuteAdminCommand("time 18:30");
                if (!Mathf.Approximately(gameTime.TimeOfDayHours, 18.5f)) throw new InvalidOperationException("time command failed");

                ExecuteAdminCommand("grow");
                if (plants.Length == 0 || plants.Any(plant => plant == null || !plant.IsHarvestReady))
                {
                    throw new InvalidOperationException("instant-grow command failed");
                }
            }
            catch (Exception exception)
            {
                failure = exception.Message;
            }

            economy.RestoreBalance(originalBalance);
            gameTime.RestoreTime(originalDay, originalHour);
            foreach (KeyValuePair<PlantInstance, float> entry in originalGrowth)
            {
                if (entry.Key != null) entry.Key.RestoreGrowth(entry.Value);
            }

            if (temporaryPlant != null)
            {
                temporaryContainer?.ClearPlant(temporaryPlant);
                Destroy(temporaryPlant.gameObject);
            }

            yield return null;
            if (failure == null)
            {
                Debug.Log($"Growveld admin console runtime smoke test passed: toggle, money, time, and instant-grow commands succeeded; {plants.Length} plant state(s) restored.");
                Application.Quit(0);
            }
            else
            {
                Debug.LogError($"Growveld admin console runtime smoke test failed: {failure}.");
                Application.Quit(1);
            }
        }

        private static string GrowAllPlants()
        {
            PlantInstance[] plants = FindObjectsByType<PlantInstance>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (PlantInstance plant in plants)
            {
                if (plant != null) plant.RestoreGrowth(float.MaxValue);
            }

            return plants.Length == 0
                ? "No planted crops were found."
                : $"Instantly grew {plants.Length} planted crop{(plants.Length == 1 ? string.Empty : "s")}.";
        }

        private static string SetTime(string[] arguments)
        {
            if (arguments.Length < 2 || !TryParseHour(arguments[1], out float hour))
            {
                return "Usage: time <0-23.99 or HH:MM>. Example: time 18:30";
            }

            GameTimeManager gameTime = FindFirstObjectByType<GameTimeManager>();
            if (gameTime == null) return "Game time manager was not found.";
            gameTime.RestoreTime(gameTime.Day, hour);
            return $"Time changed to {gameTime.FormattedTime} on Day {gameTime.Day}.";
        }

        private static string AddMoney(string[] arguments)
        {
            if (!TryParseMoney(arguments, out float amount) || amount <= 0f)
            {
                return "Usage: money <positive amount>. Example: money 50000";
            }

            EconomyManager economy = FindFirstObjectByType<EconomyManager>();
            if (economy == null) return "Economy manager was not found.";
            economy.Credit(amount, "Admin command");
            return $"Added R{amount:N0}. Balance: R{economy.Balance:N0}.";
        }

        private static string SetMoney(string[] arguments)
        {
            if (!TryParseMoney(arguments, out float amount) || amount < 0f)
            {
                return "Usage: setmoney <non-negative amount>. Example: setmoney 100000";
            }

            EconomyManager economy = FindFirstObjectByType<EconomyManager>();
            if (economy == null) return "Economy manager was not found.";
            economy.RestoreBalance(amount);
            return $"Balance set to R{economy.Balance:N0}.";
        }

        private static string SaveGame()
        {
            SaveSystem saveSystem = FindFirstObjectByType<SaveSystem>();
            return saveSystem != null && saveSystem.SaveGame(false)
                ? $"Game saved locally: {saveSystem.SavePath}"
                : "Save failed.";
        }

        private static string LoadGame()
        {
            SaveSystem saveSystem = FindFirstObjectByType<SaveSystem>();
            if (saveSystem == null || !saveSystem.HasSave) return "No local save was found.";
            saveSystem.LoadGame();
            return "Loading the local save.";
        }

        private static bool TryParseMoney(string[] arguments, out float amount)
        {
            amount = 0f;
            if (arguments.Length < 2) return false;
            bool parsed = float.TryParse(arguments[1], NumberStyles.Float, CultureInfo.InvariantCulture, out amount)
                || float.TryParse(arguments[1], NumberStyles.Float, CultureInfo.CurrentCulture, out amount);
            return parsed && !float.IsNaN(amount) && !float.IsInfinity(amount) && amount <= MaximumMoneyAmount;
        }

        private static bool TryParseHour(string value, out float hour)
        {
            hour = 0f;
            if (value.Contains(':'))
            {
                string[] pieces = value.Split(':');
                if (pieces.Length != 2
                    || !int.TryParse(pieces[0], out int wholeHour)
                    || !int.TryParse(pieces[1], out int minute)
                    || wholeHour < 0 || wholeHour > 23 || minute < 0 || minute > 59)
                {
                    return false;
                }

                hour = wholeHour + minute / 60f;
                return true;
            }

            bool parsed = float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out hour)
                || float.TryParse(value, NumberStyles.Float, CultureInfo.CurrentCulture, out hour);
            return parsed && hour >= 0f && hour < 24f;
        }

        private void AppendOutput(string message)
        {
            foreach (string line in message.Split('\n'))
            {
                outputLines.Enqueue(line);
                while (outputLines.Count > MaximumOutputLines) outputLines.Dequeue();
            }

            outputText.text = string.Join("\n", outputLines);
        }

        private void BuildInterface()
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            GameObject canvasObject = new("Admin Console UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 120;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            consoleRoot = CreatePanel(canvasObject.transform, "Console", new Vector2(900f, 520f), new Color(0.025f, 0.04f, 0.03f, 0.97f));
            Text title = CreateText(consoleRoot.transform, "Title", font, 28, FontStyle.Bold, TextAnchor.MiddleLeft);
            SetRect(title.rectTransform, new Vector2(24f, -18f), new Vector2(852f, 44f));
            title.text = "GROWVELD ADMIN CONSOLE";
            title.color = new Color(0.48f, 0.92f, 0.55f);

            Text hint = CreateText(consoleRoot.transform, "Hint", font, 18, FontStyle.Normal, TextAnchor.MiddleLeft);
            SetRect(hint.rectTransform, new Vector2(24f, -62f), new Vector2(852f, 34f));
            hint.text = "Enter a command and press Enter. Close with F2, `, or Escape. Type help for the command list.";
            hint.color = new Color(0.72f, 0.8f, 0.73f);

            outputText = CreateText(consoleRoot.transform, "Output", font, 21, FontStyle.Normal, TextAnchor.UpperLeft);
            SetRect(outputText.rectTransform, new Vector2(24f, -108f), new Vector2(852f, 322f));
            outputText.horizontalOverflow = HorizontalWrapMode.Wrap;
            outputText.verticalOverflow = VerticalWrapMode.Truncate;
            outputText.color = new Color(0.87f, 0.94f, 0.88f);
            AppendOutput("Admin console ready. Type help for commands.");

            GameObject inputObject = CreatePanel(consoleRoot.transform, "Command Input", new Vector2(852f, 58f), new Color(0.08f, 0.13f, 0.09f, 1f));
            RectTransform inputRect = inputObject.GetComponent<RectTransform>();
            inputRect.anchorMin = new Vector2(0f, 1f);
            inputRect.anchorMax = new Vector2(0f, 1f);
            inputRect.pivot = new Vector2(0f, 1f);
            inputRect.anchoredPosition = new Vector2(24f, -442f);

            Text inputText = CreateText(inputObject.transform, "Text", font, 23, FontStyle.Normal, TextAnchor.MiddleLeft);
            Stretch(inputText.rectTransform, 16f, 6f);
            inputText.color = Color.white;

            Text placeholder = CreateText(inputObject.transform, "Placeholder", font, 23, FontStyle.Italic, TextAnchor.MiddleLeft);
            Stretch(placeholder.rectTransform, 16f, 6f);
            placeholder.text = "help";
            placeholder.color = new Color(0.45f, 0.55f, 0.47f);

            commandInput = inputObject.AddComponent<InputField>();
            commandInput.textComponent = inputText;
            commandInput.placeholder = placeholder;
            commandInput.lineType = InputField.LineType.SingleLine;
            commandInput.characterLimit = 100;
            commandInput.onEndEdit.AddListener(SubmitCommand);
        }

        private static GameObject CreatePanel(Transform parent, string name, Vector2 size, Color colour)
        {
            GameObject panel = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panel.transform.SetParent(parent, false);
            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            panel.GetComponent<Image>().color = colour;
            return panel;
        }

        private static Text CreateText(Transform parent, string name, Font font, int size, FontStyle style, TextAnchor alignment)
        {
            GameObject textObject = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            textObject.transform.SetParent(parent, false);
            Text text = textObject.GetComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = alignment;
            return text;
        }

        private static void SetRect(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void Stretch(RectTransform rect, float horizontalPadding, float verticalPadding)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(horizontalPadding, verticalPadding);
            rect.offsetMax = new Vector2(-horizontalPadding, -verticalPadding);
        }
    }
}
