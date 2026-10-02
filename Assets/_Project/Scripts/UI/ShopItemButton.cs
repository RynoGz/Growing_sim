using Growveld.Economy;
using Growveld.Inventory;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Growveld.Progression;

namespace Growveld.UI
{
    [RequireComponent(typeof(Button))]
    public sealed class ShopItemButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler
    {
        [SerializeField] private ShopManager shop;
        [SerializeField] private ItemDefinition item;
        [SerializeField] private Text label;
        [SerializeField] private BusinessProgression progression;
        [SerializeField] private TabletTooltipUI tooltip;

        private Button button;

        private void Awake()
        {
            button = GetComponent<Button>();
            button.onClick.AddListener(Purchase);
            RefreshLabel();
        }

        private void OnEnable()
        {
            if (progression != null) progression.ProgressChanged += RefreshLabel;
            RefreshLabel();
        }

        private void OnDisable()
        {
            if (progression != null) progression.ProgressChanged -= RefreshLabel;
            tooltip?.Hide(this);
        }

        private void Purchase()
        {
            shop?.TryOrder(item, 1);
        }

        public void RefreshLabel()
        {
            if (label != null && item != null)
            {
                bool unlocked = progression == null || progression.IsUnlocked(item);
                label.text = unlocked
                    ? $"[{item.Category}]  {item.DisplayName}\n{item.Description}     R {item.PurchasePrice:N0}"
                    : $"[LOCKED]  {item.DisplayName}\nRequires Business Level {item.RequiredLevel}";
                if (button == null) button = GetComponent<Button>();
                button.interactable = unlocked;
                Image image = GetComponent<Image>();
                if (image != null) image.color = unlocked
                    ? new Color(0.1f, 0.18f, 0.12f, 1f)
                    : new Color(0.09f, 0.1f, 0.09f, 0.9f);
            }
        }

        public void OnPointerEnter(PointerEventData eventData) => tooltip?.Show(this, item, eventData);
        public void OnPointerMove(PointerEventData eventData) => tooltip?.Move(eventData);
        public void OnPointerExit(PointerEventData eventData) => tooltip?.Hide(this);
    }
}
