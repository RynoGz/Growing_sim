using System;
using System.Collections.Generic;
using Growveld.Farming;
using Growveld.Inventory;
using UnityEngine;

namespace Growveld.Progression
{
    public sealed class BusinessProgression : MonoBehaviour
    {
        [SerializeField] private BusinessProgressionSettings settings;
        [SerializeField] private ItemDefinition[] unlockCatalog;
        [SerializeField, Min(1)] private int currentLevel = 1;
        [SerializeField, Min(0)] private int currentExperience;
        [SerializeField, Min(0)] private int lifetimeExperience;
        [SerializeField, Min(0f)] private float lifetimeSales;

        public event Action ProgressChanged;
        public event Action<int, IReadOnlyList<ItemDefinition>> LevelIncreased;

        public BusinessProgressionSettings Settings => settings;
        public int CurrentLevel => currentLevel;
        public int CurrentExperience => currentExperience;
        public int LifetimeExperience => lifetimeExperience;
        public float LifetimeSales => lifetimeSales;
        public int ExperienceRequiredForNextLevel => settings != null ? settings.GetExperienceRequired(currentLevel) : 0;
        public bool IsMaximumLevel => settings == null || currentLevel >= settings.MaximumLevel;
        public float Progress01 => IsMaximumLevel || ExperienceRequiredForNextLevel <= 0
            ? 1f
            : Mathf.Clamp01((float)currentExperience / ExperienceRequiredForNextLevel);
        public IReadOnlyList<ItemDefinition> UnlockCatalog => unlockCatalog;

        public bool IsUnlocked(ItemDefinition item) => item != null && currentLevel >= item.RequiredLevel;

        public int CalculateSaleExperience(float kilograms, QualityGrade grade)
        {
            return settings != null ? settings.CalculateSaleExperience(kilograms, grade) : 0;
        }

        public void AwardSale(float saleValue, int experience)
        {
            lifetimeSales += Mathf.Max(0f, saleValue);
            AddExperience(experience);
        }

        public void AddExperience(int amount)
        {
            if (amount <= 0 || settings == null) return;
            lifetimeExperience += amount;
            currentExperience += amount;
            int previousLevel = currentLevel;
            while (currentLevel < settings.MaximumLevel)
            {
                int needed = settings.GetExperienceRequired(currentLevel);
                if (needed <= 0 || currentExperience < needed) break;
                currentExperience -= needed;
                currentLevel++;
            }

            if (currentLevel >= settings.MaximumLevel) currentExperience = 0;
            ProgressChanged?.Invoke();
            if (currentLevel > previousLevel)
            {
                List<ItemDefinition> unlocked = new();
                if (unlockCatalog != null)
                {
                    foreach (ItemDefinition item in unlockCatalog)
                    {
                        if (item != null && item.RequiredLevel > previousLevel && item.RequiredLevel <= currentLevel) unlocked.Add(item);
                    }
                }
                LevelIncreased?.Invoke(currentLevel, unlocked);
            }
        }

        public void Restore(int level, int experience, int lifetimeXp, float totalSales)
        {
            int maximum = settings != null ? settings.MaximumLevel : 10;
            currentLevel = Mathf.Clamp(level <= 0 ? 1 : level, 1, maximum);
            currentExperience = currentLevel >= maximum ? 0 : Mathf.Max(0, experience);
            lifetimeExperience = Mathf.Max(currentExperience, lifetimeXp);
            lifetimeSales = Mathf.Max(0f, totalSales);
            ProgressChanged?.Invoke();
        }
    }
}
