using UnityEngine;
using UnityEngine.UI;

namespace Growveld.UI
{
    public sealed class BusinessTabletUI : MonoBehaviour
    {
        [SerializeField] private Text sectionTitle;
        [SerializeField] private GameObject[] sections;
        [SerializeField] private string[] sectionNames;

        private void OnEnable()
        {
            ShowSection(0);
        }

        public void ShowSection(int index)
        {
            if (sections == null || sections.Length == 0)
            {
                return;
            }

            int selected = Mathf.Clamp(index, 0, sections.Length - 1);
            for (int sectionIndex = 0; sectionIndex < sections.Length; sectionIndex++)
            {
                if (sections[sectionIndex] != null)
                {
                    sections[sectionIndex].SetActive(sectionIndex == selected);
                }
            }

            if (sectionTitle != null)
            {
                sectionTitle.text = sectionNames != null && selected < sectionNames.Length
                    ? sectionNames[selected]
                    : "Growveld";
            }
        }
    }
}
