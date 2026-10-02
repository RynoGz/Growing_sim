using Growveld.Farming;
using UnityEngine;

namespace Growveld.Progression
{
    [CreateAssetMenu(menuName = "Growveld/Progression/Business Progression Settings", fileName = "BusinessProgressionSettings")]
    public sealed class BusinessProgressionSettings : ScriptableObject
    {
        [SerializeField] private int[] experiencePerLevel = { 100, 175, 275, 400, 550, 750, 1000, 1300, 1650 };
        [SerializeField, Min(0.01f)] private float baseExperiencePerKilogram = 50f;
        [SerializeField, Min(0f)] private float lowQualityMultiplier = 0.75f;
        [SerializeField, Min(0f)] private float standardQualityMultiplier = 1f;
        [SerializeField, Min(0f)] private float premiumQualityMultiplier = 1.25f;
        [SerializeField, Min(0f)] private float topGradeQualityMultiplier = 1.5f;

        public int MaximumLevel => (experiencePerLevel?.Length ?? 0) + 1;
        public float BaseExperiencePerKilogram => baseExperiencePerKilogram;

        public int GetExperienceRequired(int currentLevel)
        {
            if (experiencePerLevel == null || currentLevel < 1 || currentLevel > experiencePerLevel.Length) return 0;
            return Mathf.Max(1, experiencePerLevel[currentLevel - 1]);
        }

        public float GetQualityMultiplier(QualityGrade grade)
        {
            return grade switch
            {
                QualityGrade.Low => lowQualityMultiplier,
                QualityGrade.Standard => standardQualityMultiplier,
                QualityGrade.Premium => premiumQualityMultiplier,
                QualityGrade.TopGrade => topGradeQualityMultiplier,
                _ => 1f
            };
        }

        public int CalculateSaleExperience(float kilograms, QualityGrade grade)
        {
            return Mathf.Max(0, Mathf.RoundToInt(Mathf.Max(0f, kilograms) * baseExperiencePerKilogram * GetQualityMultiplier(grade)));
        }
    }
}
