using Growveld.Inventory;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Growveld.UI
{
    /// <summary>Top-layer tablet tooltip shared by locked and unlocked shop items.</summary>
    public sealed class TabletTooltipUI : MonoBehaviour
    {
        [SerializeField] private RectTransform popupLayer;
        [SerializeField] private GameObject tooltipPanel;
        [SerializeField] private Text tooltipText;

        private object owner;

        public bool IsVisible => tooltipPanel != null && tooltipPanel.activeSelf;

        public void Show(object requestOwner, ItemDefinition item, PointerEventData eventData)
        {
            if (item == null || tooltipPanel == null) return;
            owner = requestOwner;
            if (tooltipText != null) tooltipText.text = item.BuildShopTooltip();
            tooltipPanel.SetActive(true);
            tooltipPanel.transform.SetAsLastSibling();
            Move(eventData);
        }

        public void Move(PointerEventData eventData)
        {
            if (!IsVisible || popupLayer == null || eventData == null) return;
            RectTransform rect = tooltipPanel.GetComponent<RectTransform>();
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(popupLayer, eventData.position, eventData.pressEventCamera, out Vector2 point)) return;
            Vector2 desired = point + new Vector2(24f, -24f);
            Rect bounds = popupLayer.rect;
            float padding = 10f;
            desired.x = Mathf.Clamp(desired.x, bounds.xMin + padding + rect.rect.width * rect.pivot.x, bounds.xMax - padding - rect.rect.width * (1f - rect.pivot.x));
            desired.y = Mathf.Clamp(desired.y, bounds.yMin + padding + rect.rect.height * rect.pivot.y, bounds.yMax - padding - rect.rect.height * (1f - rect.pivot.y));
            rect.anchoredPosition = desired;
        }

        public void Hide(object requestOwner)
        {
            if (owner != requestOwner) return;
            owner = null;
            if (tooltipPanel != null) tooltipPanel.SetActive(false);
        }
    }
}
