using System;
using UnityEngine;

namespace ChulaEarthquakeVR
{
    // One scheduled outcome per room light; seed is recorded for replay.
    public sealed class QuakeLightFailures : MonoBehaviour
    {
        private GroundMotionPlayer motion;
        private SessionLogger logger;
        private GameFlowController flow;
        private Light[] lights;
        private float[] baseIntensity, times;
        private int[] outcomes;
        private bool[] triggered;
        private bool wasPlaying;

        private void Start()
        {
            motion = FindFirstObjectByType<GroundMotionPlayer>();
            logger = FindFirstObjectByType<SessionLogger>();
            flow = FindFirstObjectByType<GameFlowController>();
            lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
            Array.Sort(lights, (a,b) => string.CompareOrdinal(a.name, b.name));
            baseIntensity = new float[lights.Length];
            times = new float[lights.Length]; outcomes = new int[lights.Length]; triggered = new bool[lights.Length];
            for (int i = 0; i < lights.Length; i++) baseIntensity[i] = lights[i].intensity;
        }
        private void LateUpdate()
        {
            if (motion == null || lights == null) return;
            // Recorded research sessions keep their original conditions.
            if (!motion.IsUsingPreview || (flow != null && !flow.IsTraining)) return;
            if (motion.IsPlaying && !wasPlaying)
            {
                Restore();
                int seed = Guid.NewGuid().GetHashCode();
                var random = new System.Random(seed);
                logger?.LogEvent("light_failure_seed", "{\"seed\":" + seed + "}");
                for (int i = 0; i < lights.Length; i++)
                {
                    outcomes[i] = random.Next(4); // unaffected / flicker / outage / broken bulb
                    times[i] = motion.DurationSeconds * (0.3f + (float)random.NextDouble() * 0.5f);
                    triggered[i] = false;
                }
            }
            wasPlaying = motion.IsPlaying;
            for (int i = 0; i < lights.Length; i++)
            {
                Light lamp = lights[i];
                if (lamp == null || lamp.type == LightType.Directional || lamp.name.Contains("Exit")) continue;
                if (!triggered[i] && motion.IsPlaying && motion.ElapsedSeconds >= times[i])
                {
                    triggered[i] = true;
                    logger?.LogEvent("light_failure", "{\"index\":" + i + ",\"outcome\":" + outcomes[i] + "}");
                    if (outcomes[i] == 3) Burst(lamp.transform.position);
                }
                if (!triggered[i]) continue;
                if (outcomes[i] >= 2) lamp.intensity = 0f;
                else if (outcomes[i] == 1 && motion.IsPlaying)
                    lamp.intensity = baseIntensity[i] * (0.25f + 0.75f * Mathf.PerlinNoise(Time.time * 2f, i * 3.7f));
                else if (outcomes[i] == 1) lamp.intensity = baseIntensity[i];
            }
        }
        private void Burst(Vector3 position)
        {
            var effect = new GameObject("BulbBreakParticles");
            effect.transform.position = position;
            var particles = effect.AddComponent<ParticleSystem>();
            Shader shader = Shader.Find("Sprites/Default");
            if (shader != null)
            {
                Material material = new Material(shader);
                particles.GetComponent<ParticleSystemRenderer>().sharedMaterial = material;
                Destroy(material, 1.1f);
            }
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = particles.main;
            main.loop = false; main.duration = 0.3f; main.startLifetime = 0.5f;
            main.startSpeed = 1.2f; main.startSize = 0.025f; main.gravityModifier = 0.8f;
            main.startColor = new Color(1f, 0.85f, 0.5f); main.maxParticles = 12;
            var emission = particles.emission; emission.rateOverTime = 0f;
            particles.Play(); particles.Emit(12);
            Destroy(effect, 1f);
        }
        private void Restore()
        {
            if (lights == null) return;
            for (int i = 0; i < lights.Length; i++) if (lights[i] != null) lights[i].intensity = baseIntensity[i];
        }
        private void OnDisable() => Restore();
    }
}
