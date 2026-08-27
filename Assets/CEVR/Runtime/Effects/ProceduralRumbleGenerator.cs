using UnityEngine;

namespace ChulaEarthquakeVR
{
    [RequireComponent(typeof(AudioSource))]
    public sealed class ProceduralRumbleGenerator : MonoBehaviour
    {
        [SerializeField, Range(20f, 80f)] private float primaryFrequencyHz = 36f;
        [SerializeField, Range(0f, 0.5f)] private float harmonicMix = 0.22f;
        private AudioClip generatedClip;

        private void Awake()
        {
            AudioSource source = GetComponent<AudioSource>();
            const int sampleRate = 48000;
            const int seconds = 2;
            float[] samples = new float[sampleRate * seconds];
            for (int i = 0; i < samples.Length; i++)
            {
                float time = (float)i / sampleRate;
                float fundamental = Mathf.Sin(2f * Mathf.PI * primaryFrequencyHz * time);
                float harmonic = Mathf.Sin(2f * Mathf.PI * primaryFrequencyHz * 1.37f * time);
                samples[i] = (fundamental + harmonic * harmonicMix) * 0.35f;
            }
            generatedClip = AudioClip.Create("CEVR_ProceduralRumble", samples.Length, 1, sampleRate, false);
            generatedClip.SetData(samples, 0);
            source.clip = generatedClip;
            source.loop = true;
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.volume = 0f;
        }

        private void OnDestroy()
        {
            if (generatedClip != null) Destroy(generatedClip);
        }
    }
}
