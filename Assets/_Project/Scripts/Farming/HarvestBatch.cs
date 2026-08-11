using Growveld.Interaction;
using UnityEngine;

namespace Growveld.Farming
{
    /// <summary>
    /// Persistent product data attached to a physical harvest container.
    /// </summary>
    public sealed class HarvestBatch : MonoBehaviour, IContextualInfoProvider
    {
        [SerializeField] private string batchId;
        [SerializeField, Min(0f)] private float weightKilograms;
        [SerializeField] private QualityGrade qualityGrade = QualityGrade.Standard;
        [SerializeField] private HarvestStatus status = HarvestStatus.Fresh;
        [SerializeField] private QualitySettings qualitySettings;

        public string BatchId => batchId;
        public float WeightKilograms => weightKilograms;
        public QualityGrade QualityGrade => qualityGrade;
        public HarvestStatus Status => status;
        public string ContextualInfo
        {
            get
            {
                string gradeName = qualitySettings != null
                    ? qualitySettings.GetDisplayName(qualityGrade)
                    : qualityGrade.ToString();
                return $"{status} Harvest Batch\nWeight: {weightKilograms:0.00} kg\nQuality: {gradeName}";
            }
        }

        private void Awake()
        {
            if (string.IsNullOrWhiteSpace(batchId)) batchId = System.Guid.NewGuid().ToString("N");
        }

        public void Initialise(float weight, QualityGrade grade, HarvestStatus initialStatus = HarvestStatus.Fresh)
        {
            if (string.IsNullOrWhiteSpace(batchId)) batchId = System.Guid.NewGuid().ToString("N");
            weightKilograms = Mathf.Max(0.01f, weight);
            qualityGrade = grade;
            status = initialStatus;
        }

        public void SetStatus(HarvestStatus newStatus)
        {
            status = newStatus;
        }

        public void RestoreBatch(string restoredId, float weight, QualityGrade grade, HarvestStatus restoredStatus)
        {
            batchId = string.IsNullOrWhiteSpace(restoredId) ? System.Guid.NewGuid().ToString("N") : restoredId;
            weightKilograms = Mathf.Max(0.01f, weight);
            qualityGrade = grade;
            status = restoredStatus;
        }
    }
}
