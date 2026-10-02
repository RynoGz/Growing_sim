using System.Collections.Generic;
using Growveld.Farming;
using Growveld.Interaction;
using Growveld.Inventory;
using Growveld.Player;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Growveld.UI
{
    /// <summary>Small modal chooser used by every empty planting container.</summary>
    public sealed class SeedSelectionUI : MonoBehaviour
    {
        private static SeedSelectionUI current;
        private readonly List<Behaviour> suspended = new();
        private GameObject panel;
        private RectTransform rowsRoot;
        private PlantingContainer target;
        private GameObject interactor;
        private PlayerInputStateController inputState;

        public bool IsOpen => panel != null && panel.activeSelf;

        public static void Show(PlantingContainer container, GameObject player, IReadOnlyList<ItemDefinition> seeds)
        {
            EnsureInstance();
            current.Open(container, player, seeds);
        }

        private static void EnsureInstance()
        {
            if (current != null) return;
            current = FindFirstObjectByType<SeedSelectionUI>(FindObjectsInactive.Include);
            if (current == null) current = new GameObject("Seed Selection UI").AddComponent<SeedSelectionUI>();
        }

        private void Awake()
        {
            current = this;
            BuildInterface();
            panel.SetActive(false);
        }

        private void Update()
        {
            if (IsOpen && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) Close();
        }

        private void Open(PlantingContainer container, GameObject player, IReadOnlyList<ItemDefinition> seeds)
        {
            target = container;
            interactor = player;
            for (int index = rowsRoot.childCount - 1; index >= 0; index--) Destroy(rowsRoot.GetChild(index).gameObject);
            for (int index = 0; seeds != null && index < seeds.Count; index++) CreateSeedButton(seeds[index], index);
            panel.SetActive(true);
            inputState = player != null ? player.GetComponent<PlayerInputStateController>() : null;
            inputState?.SetSeedSelectionOpen(true);
            Suspend(player != null ? player.GetComponent<FirstPersonController>() : null);
            Suspend(player != null ? player.GetComponent<PlayerInteractor>() : null);
        }

        private void Choose(ItemDefinition seed)
        {
            target?.TryPlant(interactor, seed);
            Close();
        }

        private void Close()
        {
            panel.SetActive(false);
            foreach (Behaviour behaviour in suspended) if (behaviour != null) behaviour.enabled = true;
            suspended.Clear();
            target = null;
            interactor = null;
            inputState?.SetSeedSelectionOpen(false);
            inputState = null;
        }

        private void Suspend(Behaviour behaviour)
        {
            if (behaviour == null || !behaviour.enabled) return;
            suspended.Add(behaviour);
            behaviour.enabled = false;
        }

        private void BuildInterface()
        {
            GameObject canvasObject = new("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 90;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            panel = new GameObject("Seed Chooser", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panel.transform.SetParent(canvasObject.transform, false);
            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(700f, 560f);
            panel.GetComponent<Image>().color = new Color(0.025f, 0.05f, 0.032f, 0.98f);
            Text title = CreateText(panel.transform, "Title", 30, TextAnchor.MiddleCenter);
            SetRect(title.rectTransform, new Vector2(20f, -18f), new Vector2(660f, 52f));
            title.text = "CHOOSE A STRAIN TO PLANT";
            rowsRoot = new GameObject("Owned Seeds", typeof(RectTransform)).GetComponent<RectTransform>();
            rowsRoot.SetParent(panel.transform, false);
            SetRect(rowsRoot, new Vector2(28f, -88f), new Vector2(644f, 420f));
            Text footer = CreateText(panel.transform, "Footer", 18, TextAnchor.MiddleCenter);
            SetRect(footer.rectTransform, new Vector2(20f, -520f), new Vector2(660f, 28f));
            footer.text = "Select an owned seed, or press Escape to cancel.";
        }

        private void CreateSeedButton(ItemDefinition seed, int index)
        {
            GameObject row = new($"Plant {seed.DisplayName}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            row.transform.SetParent(rowsRoot, false);
            RectTransform rect = row.GetComponent<RectTransform>();
            SetRect(rect, new Vector2(0f, -index * 96f), new Vector2(644f, 82f));
            row.GetComponent<Image>().color = new Color(0.1f, 0.24f, 0.14f, 1f);
            Text label = CreateText(row.transform, "Label", 22, TextAnchor.MiddleLeft);
            label.rectTransform.anchorMin = Vector2.zero;
            label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = new Vector2(18f, 6f);
            label.rectTransform.offsetMax = new Vector2(-18f, -6f);
            int quantity = interactor != null && interactor.TryGetComponent(out PlayerInventory inventory) ? inventory.Count(seed) : 0;
            label.text = $"{seed.DisplayName}  x{quantity}\n{seed.StrainDefinition?.GuidanceText}";
            row.GetComponent<Button>().onClick.AddListener(() => Choose(seed));
        }

        private static Text CreateText(Transform parent, string name, int fontSize, TextAnchor alignment)
        {
            GameObject textObject = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            textObject.transform.SetParent(parent, false);
            Text text = textObject.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = Color.white;
            return text;
        }

        private static void SetRect(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }
    }
}
