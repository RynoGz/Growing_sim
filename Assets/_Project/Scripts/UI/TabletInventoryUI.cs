using System.Collections.Generic;
using Growveld.Building;
using Growveld.Inventory;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Growveld.UI
{
    /// <summary>
    /// Readable tablet view over the player's owned items, with contextual place actions.
    /// </summary>
    public sealed class TabletInventoryUI : MonoBehaviour
    {
        [SerializeField] private PlayerInventory inventory;
        [SerializeField] private ConstructionModeController constructionMode;
        [SerializeField] private BusinessTabletController tabletController;
        [SerializeField] private RectTransform rowsRoot;
        [SerializeField] private Text emptyLabel;
        [SerializeField] private GameObject contextMenu;
        [SerializeField] private Text contextTitle;
        [SerializeField] private Button placeButton;

        private ItemDefinition contextItem;

        public bool IsContextMenuOpen => contextMenu != null && contextMenu.activeSelf;

        private void OnEnable()
        {
            if (inventory != null) inventory.InventoryChanged += Refresh;
            placeButton?.onClick.AddListener(PlaceContextItem);
            HideContextMenu();
            Refresh();
        }

        private void OnDisable()
        {
            if (inventory != null) inventory.InventoryChanged -= Refresh;
            placeButton?.onClick.RemoveListener(PlaceContextItem);
            HideContextMenu();
        }

        public bool TryCloseContextMenu()
        {
            if (!IsContextMenuOpen) return false;
            HideContextMenu();
            return true;
        }

        public void Refresh()
        {
            if (rowsRoot == null || inventory == null) return;

            for (int index = rowsRoot.childCount - 1; index >= 0; index--)
            {
                Transform child = rowsRoot.GetChild(index);
                if (child.GetComponent<TabletInventoryItemRow>() != null) Destroy(child.gameObject);
            }

            Dictionary<ItemDefinition, int> ownedItems = new();
            foreach (InventorySlot slot in inventory.Slots)
            {
                if (slot == null || slot.IsEmpty) continue;
                ownedItems.TryGetValue(slot.Item, out int quantity);
                ownedItems[slot.Item] = quantity + slot.Quantity;
            }

            List<ItemDefinition> sortedItems = new(ownedItems.Keys);
            sortedItems.Sort((left, right) =>
            {
                int category = left.Category.CompareTo(right.Category);
                return category != 0 ? category : string.CompareOrdinal(left.DisplayName, right.DisplayName);
            });

            for (int index = 0; index < sortedItems.Count; index++)
            {
                CreateRow(sortedItems[index], ownedItems[sortedItems[index]], index);
            }

            if (emptyLabel != null) emptyLabel.gameObject.SetActive(sortedItems.Count == 0);
            rowsRoot.sizeDelta = new Vector2(rowsRoot.sizeDelta.x, Mathf.Max(590f, sortedItems.Count * 86f + 12f));
        }

        internal void ShowContextMenu(ItemDefinition item, PointerEventData eventData)
        {
            if (item == null || item.PlaceableDefinition == null || inventory == null || inventory.Count(item) <= 0)
            {
                HideContextMenu();
                return;
            }

            contextItem = item;
            if (contextTitle != null) contextTitle.text = item.DisplayName;
            if (contextMenu == null) return;

            contextMenu.SetActive(true);
            RectTransform menuRect = contextMenu.GetComponent<RectTransform>();
            RectTransform parentRect = menuRect.parent as RectTransform;
            if (parentRect != null
                && RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, eventData.position, eventData.pressEventCamera, out Vector2 localPoint))
            {
                menuRect.anchoredPosition = localPoint + new Vector2(18f, -18f);
            }
        }

        private void HideContextMenu()
        {
            contextItem = null;
            if (contextMenu != null) contextMenu.SetActive(false);
        }

        private void PlaceContextItem()
        {
            ItemDefinition item = contextItem;
            HideContextMenu();
            if (item == null || inventory == null || inventory.Count(item) <= 0) return;

            tabletController?.SetOpen(false);
            if (constructionMode == null || !constructionMode.BeginInventoryPlacement(item))
            {
                GameplayMessageUI.Show($"Could not place {item.DisplayName}");
            }
        }

        private void CreateRow(ItemDefinition item, int quantity, int index)
        {
            GameObject row = new($"Owned {item.DisplayName}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(TabletInventoryItemRow));
            row.transform.SetParent(rowsRoot, false);
            RectTransform rect = row.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -index * 86f);
            rect.sizeDelta = new Vector2(0f, 74f);
            row.GetComponent<Image>().color = new Color(0.075f, 0.135f, 0.09f, 0.98f);

            GameObject iconObject = new("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            iconObject.transform.SetParent(row.transform, false);
            RectTransform iconRect = iconObject.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0f, 0.5f);
            iconRect.anchorMax = new Vector2(0f, 0.5f);
            iconRect.pivot = new Vector2(0f, 0.5f);
            iconRect.anchoredPosition = new Vector2(12f, 0f);
            iconRect.sizeDelta = new Vector2(52f, 52f);
            Image icon = iconObject.GetComponent<Image>();
            icon.sprite = item.Icon;
            icon.color = item.Icon != null ? Color.white : item.DisplayColor;

            GameObject labelObject = new("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            labelObject.transform.SetParent(row.transform, false);
            RectTransform labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(82f, 8f);
            labelRect.offsetMax = new Vector2(-14f, -8f);
            Text label = labelObject.GetComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 22;
            label.alignment = TextAnchor.MiddleLeft;
            label.color = Color.white;
            string actionHint = item.PlaceableDefinition != null ? "  •  Right-click to place" : string.Empty;
            label.text = $"{item.DisplayName}    x{quantity}\n{item.Category}{actionHint}";

            row.GetComponent<TabletInventoryItemRow>().Initialise(this, item);
        }
    }

    public sealed class TabletInventoryItemRow : MonoBehaviour, IPointerClickHandler
    {
        private TabletInventoryUI owner;
        private ItemDefinition item;

        public void Initialise(TabletInventoryUI inventoryUI, ItemDefinition definition)
        {
            owner = inventoryUI;
            item = definition;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Right)
            {
                owner?.ShowContextMenu(item, eventData);
            }
        }
    }
}
