using System.Linq;
using System.Text;
using Growveld.Economy;
using Growveld.Inventory;
using Growveld.Progression;
using UnityEngine;
using UnityEngine.UI;

namespace Growveld.UI
{
    public sealed class BusinessProgressionDashboard : MonoBehaviour
    {
        [SerializeField] private BusinessProgression progression;
        [SerializeField] private EconomyManager economy;
        [SerializeField] private ShopManager shop;
        [SerializeField] private Text summaryText;
        [SerializeField] private Slider experienceBar;

        private void OnEnable()
        {
            if (progression != null) progression.ProgressChanged += Refresh;
            if (economy != null) economy.BalanceChanged += HandleBalance;
            Refresh();
        }

        private void OnDisable()
        {
            if (progression != null) progression.ProgressChanged -= Refresh;
            if (economy != null) economy.BalanceChanged -= HandleBalance;
        }

        private void HandleBalance(float value) => Refresh();

        public void Refresh()
        {
            if (progression == null || summaryText == null) return;
            int required = progression.ExperienceRequiredForNextLevel;
            StringBuilder builder = new();
            builder.AppendLine($"Business Level: {progression.CurrentLevel}");
            builder.AppendLine(progression.IsMaximumLevel ? "XP: MAX LEVEL" : $"XP: {progression.CurrentExperience:N0} / {required:N0}");
            builder.AppendLine($"Current Money: R{(economy != null ? economy.Balance : 0f):N0}");
            builder.AppendLine($"Lifetime Sales: R{progression.LifetimeSales:N0}\n");

            ItemDefinition[] next = shop?.AvailableItems?
                .Where(item => item != null && item.RequiredLevel > progression.CurrentLevel)
                .OrderBy(item => item.RequiredLevel).ThenBy(item => item.DisplayName).ToArray();
            if (next == null || next.Length == 0)
            {
                builder.AppendLine("All current progression unlocks achieved.");
            }
            else
            {
                int nextLevel = next[0].RequiredLevel;
                builder.AppendLine($"Next Unlock - Level {nextLevel}");
                foreach (ItemDefinition item in next.Where(item => item.RequiredLevel == nextLevel)) builder.AppendLine($"- {item.DisplayName}");
            }
            summaryText.text = builder.ToString();
            if (experienceBar != null) experienceBar.value = progression.Progress01;
        }
    }
}
