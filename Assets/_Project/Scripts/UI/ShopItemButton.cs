using Growveld.Economy;
using Growveld.Inventory;
using UnityEngine;
using UnityEngine.UI;

namespace Growveld.UI
{
    [RequireComponent(typeof(Button))]
    public sealed class ShopItemButton : MonoBehaviour
    {
        [SerializeField] private ShopManager shop;
        [SerializeField] private ItemDefinition item;
        [SerializeField] private Text label;

        private void Awake()
        {
            GetComponent<Button>().onClick.AddListener(Purchase);
            RefreshLabel();
        }

        private void Purchase()
        {
            shop?.TryOrder(item, 1);
        }

        private void RefreshLabel()
        {
            if (label != null && item != null)
            {
                label.text = $"[{item.Category}]  {item.DisplayName}\n{item.Description}     R {item.PurchasePrice:N0}";
            }
        }
    }
}
