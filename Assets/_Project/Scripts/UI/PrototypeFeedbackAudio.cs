using Growveld.Carrying;
using Growveld.Economy;
using UnityEngine;

namespace Growveld.UI
{
    /// <summary>
    /// Small generated audio cues keep the prototype responsive without external audio assets.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public sealed class PrototypeFeedbackAudio : MonoBehaviour
    {
        [SerializeField] private EconomyManager economy;
        [SerializeField] private DeliveryManager deliveries;
        [SerializeField] private PlayerCarryController carryController;
        [SerializeField, Range(0f, 1f)] private float volume = 0.16f;

        private AudioSource audioSource;
        private AudioClip positiveCue;
        private AudioClip negativeCue;
        private AudioClip carryCue;

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
            positiveCue = CreateTone("Positive Cue", 720f, 0.11f);
            negativeCue = CreateTone("Negative Cue", 230f, 0.14f);
            carryCue = CreateTone("Carry Cue", 480f, 0.08f);
        }

        private void OnEnable()
        {
            if (economy != null) economy.TransactionCompleted += HandleTransaction;
            if (deliveries != null) deliveries.DeliveryCompleted += HandleDelivery;
            if (carryController != null) carryController.HeldObjectChanged += HandleCarry;
        }

        private void OnDisable()
        {
            if (economy != null) economy.TransactionCompleted -= HandleTransaction;
            if (deliveries != null) deliveries.DeliveryCompleted -= HandleDelivery;
            if (carryController != null) carryController.HeldObjectChanged -= HandleCarry;
        }

        private void HandleTransaction(EconomyTransaction transaction) => Play(transaction.Amount >= 0f ? positiveCue : negativeCue);
        private void HandleDelivery(PendingDelivery _) => Play(positiveCue);
        private void HandleCarry(CarryableObject _) => Play(carryCue);

        private void Play(AudioClip clip)
        {
            if (audioSource != null && clip != null) audioSource.PlayOneShot(clip, volume);
        }

        private static AudioClip CreateTone(string clipName, float frequency, float duration)
        {
            const int sampleRate = 22050;
            int sampleCount = Mathf.Max(1, Mathf.RoundToInt(sampleRate * duration));
            float[] samples = new float[sampleCount];
            for (int index = 0; index < sampleCount; index++)
            {
                float time = index / (float)sampleRate;
                float envelope = 1f - index / (float)sampleCount;
                samples[index] = Mathf.Sin(2f * Mathf.PI * frequency * time) * envelope * 0.35f;
            }

            AudioClip clip = AudioClip.Create(clipName, sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
