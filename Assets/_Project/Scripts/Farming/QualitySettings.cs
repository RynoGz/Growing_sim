using UnityEngine;

namespace Growveld.Farming
{
    /// <summary>
    /// Shared grade thresholds and selling multipliers for all prototype crops.
    /// </summary>
    [CreateAssetMenu(menuName = "Growveld/Farming/Quality Settings", fileName = "QualitySettings")]
    public sealed class QualitySettings : ScriptableObject
    {
        [SerializeField, Range(0f, 100f)] private float standardThreshold = 45f;
        [SerializeField, Range(0f, 100f)] private float premiumThreshold = 70f;
        [SerializeField, Range(0f, 100f)] private float topGradeThreshold = 88f;
        [SerializeField, Min(0f)] private float lowMultiplier = 0.7f;
        [SerializeField, Min(0f)] private float standardMultiplier = 1f;
        [SerializeField, Min(0f)] private float premiumMultiplier = 1.3f;
        [SerializeField, Min(0f)] private float topGradeMultiplier = 1.6f;

        public QualityGrade GetGrade(float qualityScore)
        {
            if (qualityScore >= topGradeThreshold) return QualityGrade.TopGrade;
            if (qualityScore >= premiumThreshold) return QualityGrade.Premium;
            if (qualityScore >= standardThreshold) return QualityGrade.Standard;
            return QualityGrade.Low;
        }

        public float GetPriceMultiplier(QualityGrade grade)
        {
            return grade switch
            {
                QualityGrade.Low => lowMultiplier,
                QualityGrade.Standard => standardMultiplier,
                QualityGrade.Premium => premiumMultiplier,
                QualityGrade.TopGrade => topGradeMultiplier,
                _ => 1f
            };
        }

        public string GetDisplayName(QualityGrade grade)
        {
            return grade == QualityGrade.TopGrade ? "Top Grade" : grade.ToString();
        }
    }
}
