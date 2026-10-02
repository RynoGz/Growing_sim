using System.Collections.Generic;
using System.Text;
using Growveld.Inventory;
using Growveld.Progression;
using UnityEngine;

namespace Growveld.UI
{
    public sealed class ProgressionNotificationUI : MonoBehaviour
    {
        [SerializeField] private BusinessProgression progression;

        private void OnEnable()
        {
            if (progression != null) progression.LevelIncreased += HandleLevelIncreased;
        }

        private void OnDisable()
        {
            if (progression != null) progression.LevelIncreased -= HandleLevelIncreased;
        }

        private static void HandleLevelIncreased(int level, IReadOnlyList<ItemDefinition> unlocked)
        {
            StringBuilder builder = new($"LEVEL UP!  Business Level {level}");
            if (unlocked != null && unlocked.Count > 0)
            {
                List<ItemDefinition> strains = new();
                List<ItemDefinition> equipment = new();
                foreach (ItemDefinition item in unlocked)
                {
                    if (item != null && item.IsSeed) strains.Add(item); else if (item != null) equipment.Add(item);
                }
                if (strains.Count > 0)
                {
                    builder.Append("\nNew Strain Unlocked! ");
                    for (int index = 0; index < strains.Count; index++)
                    {
                        if (index > 0) builder.Append(", ");
                        builder.Append(strains[index].DisplayName);
                    }
                    builder.Append(" now available in the Shop.");
                }
                if (equipment.Count > 0)
                {
                    builder.Append("\nUnlocked: ");
                    for (int index = 0; index < equipment.Count; index++)
                    {
                        if (index > 0) builder.Append(", ");
                        builder.Append(equipment[index].DisplayName);
                    }
                }
            }
            GameplayMessageUI.Show(builder.ToString());
        }
    }
}
