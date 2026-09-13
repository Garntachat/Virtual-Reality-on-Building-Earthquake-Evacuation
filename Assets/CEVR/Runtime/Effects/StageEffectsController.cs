using UnityEngine;

namespace ChulaEarthquakeVR
{
    public sealed class StageEffectsController : MonoBehaviour
    {
        [SerializeField] private GroundMotionPlayer motion;
        [SerializeField] private AudioSource rumble;
        [SerializeField] private Light[] labLights;
        [SerializeField, Range(0f, 0.6f)] private float maximumFlicker = 0.25f;
        [SerializeField, Range(0f, 0.5f)] private float maximumRumbleVolume = 0.18f;
        private float[] baseIntensity;
        private float smoothedIntensity;

        private void CaptureLightIntensities()
        {
            baseIntensity = new float[labLights?.Length ?? 0];
            for (int i = 0; i < baseIntensity.Length; i++)
                baseIntensity[i] = labLights[i] == null ? 0f : labLights[i].intensity;
        }

        private void Awake() => CaptureLightIntensities();

        private void Update()
        {
            float target = motion != null ? motion.PresentationIntensity : 0f;
            smoothedIntensity = Mathf.Lerp(smoothedIntensity, target, 1f - Mathf.Exp(-8f * Time.deltaTime));
            float intensity = smoothedIntensity;
            if (rumble != null)
            {
                if (GameplayAudioDirector.Active != null)
                {
                    if (rumble.isPlaying) rumble.Stop();
                    rumble.volume = 0f;
                }
                else
                {
                    rumble.volume = intensity * maximumRumbleVolume;
                    if (intensity > 0.01f && !rumble.isPlaying) rumble.Play();
                    else if (intensity <= 0.01f && rumble.isPlaying) rumble.Stop();
                }
            }
            for (int i = 0; i < baseIntensity.Length; i++)
            {
                Light light = labLights[i];
                if (light == null) continue;
                float noise = Mathf.PerlinNoise(Time.unscaledTime * 12f, i * 3.17f);
                light.intensity = baseIntensity[i] * (1f - noise * maximumFlicker * intensity);
            }
        }

        private void OnDisable()
        {
            if (rumble != null)
            {
                rumble.Stop();
                rumble.volume = 0f;
            }
            if (baseIntensity == null) return;
            for (int i = 0; i < baseIntensity.Length; i++)
                if (labLights != null && i < labLights.Length && labLights[i] != null)
                    labLights[i].intensity = baseIntensity[i];
        }

        public void Configure(GroundMotionPlayer source, Light[] lights, AudioSource audioSource = null)
        {
            motion = source;
            labLights = lights;
            rumble = audioSource;
            CaptureLightIntensities();
        }
    }
}
