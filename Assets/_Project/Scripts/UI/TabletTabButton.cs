using UnityEngine;
using UnityEngine.UI;

namespace Growveld.UI
{
    [RequireComponent(typeof(Button))]
    public sealed class TabletTabButton : MonoBehaviour
    {
        [SerializeField] private BusinessTabletUI tabletUI;
        [SerializeField] private int sectionIndex;

        private void Awake()
        {
            GetComponent<Button>().onClick.AddListener(SelectSection);
        }

        private void SelectSection()
        {
            tabletUI?.ShowSection(sectionIndex);
        }
    }
}
