using System;
using System.Collections.Generic;
using UnityEngine;

namespace ChulaEarthquakeVR
{
    public enum GameplayAudioCue
    {
        Footstep,
        FurnitureGrab,
        FurnitureRelease,
        FurnitureScrape,
        FurnitureImpact,
        WindowCrack,
        FootwearEquipped,
        PillowGrab,
        LightBreak,
        PlayerHit,
        QuakeStart,
        QuakeEnd,
        Success,
        Failure
    }

    [DefaultExecutionOrder(800)]
    [DisallowMultipleComponent]
    public sealed class GameplayAudioDirector : MonoBehaviour
    {
        private sealed class FootstepState
        {
            public Vector3 previousPosition;
            public float distance;
        }

        public static GameplayAudioDirector Active { get; private set; }

        private readonly Dictionary<GameplayAudioCue, AudioClip> clips = new Dictionary<GameplayAudioCue, AudioClip>();
        private readonly List<AudioClip> ownedClips = new List<AudioClip>();
        private readonly Dictionary<CharacterController, FootstepState> walkers = new Dictionary<CharacterController, FootstepState>();
        private AudioSource rumble;
        private AudioSource structure;
        private AudioSource oneShots;
        private GroundMotionPlayer motion;
        private float smoothedIntensity;
        private float nextWalkerScan;
        private int footstepVariation;
        private AudioClip rumbleClip;
        private AudioClip structureClip;

        private void Awake()
        {
            if (Active != null && Active != this)
            {
                Destroy(this);
                return;
            }
            Active = this;
            BuildLibrary();
            rumble = Source("EarthquakeRumble", rumbleClip, true, 0f, 0f);
            structure = Source("BuildingCreak", structureClip, true, 0f, 0f);
            oneShots = Source("GameplayOneShots", null, false, 0.75f, 0f);
        }

        private void Start()
        {
            ConnectMotion();
            RefreshWalkers();
        }

        private AudioSource Source(string objectName, AudioClip clip, bool loop, float priority, float spatialBlend)
        {
            var child = new GameObject(objectName);
            child.transform.SetParent(transform, false);
            AudioSource source = child.AddComponent<AudioSource>();
            source.clip = clip;
            source.loop = loop;
            source.playOnAwake = false;
            source.spatialBlend = spatialBlend;
            source.dopplerLevel = 0f;
            source.priority = Mathf.RoundToInt(Mathf.Lerp(128f, 32f, priority));
            return source;
        }

        private void Update()
        {
            if (motion == null) ConnectMotion();
            float target = motion == null ? 0f : motion.PresentationIntensity;
            smoothedIntensity = Mathf.Lerp(smoothedIntensity, target, 1f - Mathf.Exp(-5f * Time.unscaledDeltaTime));
            UpdateQuakeLayers(smoothedIntensity);

            if (Time.unscaledTime >= nextWalkerScan)
            {
                nextWalkerScan = Time.unscaledTime + 1f;
                RefreshWalkers();
                RefreshPhysicsEmitters();
            }
            UpdateFootsteps();
        }

        private void UpdateQuakeLayers(float intensity)
        {
            if (rumble == null || structure == null) return;
            rumble.volume = 0.46f * intensity;
            rumble.pitch = Mathf.Lerp(0.88f, 1.08f, intensity);
            structure.volume = 0.17f * Mathf.SmoothStep(0f, 1f, intensity);
            structure.pitch = Mathf.Lerp(0.82f, 1.03f, intensity);
            if (intensity > 0.005f)
            {
                if (!rumble.isPlaying) rumble.Play();
                if (!structure.isPlaying) structure.Play();
            }
            else
            {
                if (rumble.isPlaying) rumble.Stop();
                if (structure.isPlaying) structure.Stop();
            }
        }

        private void ConnectMotion()
        {
            GroundMotionPlayer found = FindFirstObjectByType<GroundMotionPlayer>();
            if (found == motion) return;
            if (motion != null)
            {
                motion.QuakeStarted -= OnQuakeStarted;
                motion.QuakeEnded -= OnQuakeEnded;
            }
            motion = found;
            if (motion != null)
            {
                motion.QuakeStarted += OnQuakeStarted;
                motion.QuakeEnded += OnQuakeEnded;
            }
        }

        private void RefreshWalkers()
        {
            foreach (CharacterController controller in FindObjectsByType<CharacterController>(FindObjectsSortMode.None))
                if (!walkers.ContainsKey(controller))
                    walkers.Add(controller, new FootstepState { previousPosition = controller.transform.position });
            var removed = new List<CharacterController>();
            foreach (CharacterController controller in walkers.Keys) if (controller == null) removed.Add(controller);
            foreach (CharacterController controller in removed) walkers.Remove(controller);
        }

        private void UpdateFootsteps()
        {
            foreach (KeyValuePair<CharacterController, FootstepState> pair in walkers)
            {
                CharacterController controller = pair.Key;
                if (controller == null || !controller.enabled) continue;
                FootstepState state = pair.Value;
                Vector3 current = controller.transform.position;
                Vector3 delta = current - state.previousPosition;
                delta.y = 0f;
                state.previousPosition = current;
                float moved = delta.magnitude;
                if (moved > 0.2f) { state.distance = 0f; continue; }
                state.distance += moved;
                bool crawling = controller.height < 0.8f;
                float stride = crawling ? 0.58f : 0.82f;
                if (state.distance < stride || moved <= 0.0001f) continue;
                state.distance %= stride;
                float volume = crawling ? 0.10f : 0.17f;
                PlayInternal(GameplayAudioCue.Footstep, volume, footstepVariation++ % 2 == 0 ? 0.96f : 1.04f);
            }
        }

        private static void RefreshPhysicsEmitters()
        {
            foreach (Rigidbody body in FindObjectsByType<Rigidbody>(FindObjectsSortMode.None))
            {
                bool audibleHazard = body.GetComponent<MovableFurniture>() != null ||
                                     body.GetComponent<FallingHazard>() != null ||
                                     body.GetComponent<ToppleableFurniture>() != null;
                if (audibleHazard && body.GetComponent<FurnitureImpactAudio>() == null)
                    body.gameObject.AddComponent<FurnitureImpactAudio>();
            }
        }

        private void OnQuakeStarted() => PlayInternal(GameplayAudioCue.QuakeStart, 0.30f, 1.25f);
        private void OnQuakeEnded() => PlayInternal(GameplayAudioCue.QuakeEnd, 0.22f, 1.35f);

        public static void PlayCue(GameplayAudioCue cue, float volume = 1f, float pitch = 1f)
        {
            Active?.PlayInternal(cue, Mathf.Clamp01(volume), Mathf.Clamp(pitch, 0.6f, 1.6f));
        }

        private void PlayInternal(GameplayAudioCue cue, float volume, float pitch)
        {
            if (oneShots == null || !clips.TryGetValue(cue, out AudioClip clip) || clip == null) return;
            oneShots.pitch = pitch;
            oneShots.PlayOneShot(clip, volume);
        }

        private void BuildLibrary()
        {
            rumbleClip = Periodic("CEVR_Rumble", 4f, (t, i) =>
                (Mathf.Sin(2f * Mathf.PI * 32f * t) * 0.52f +
                 Mathf.Sin(2f * Mathf.PI * 47f * t) * 0.25f +
                 Mathf.Sin(2f * Mathf.PI * 73f * t) * 0.12f) *
                (0.72f + 0.28f * Mathf.Sin(2f * Mathf.PI * 2f * t)));
            structureClip = Periodic("CEVR_StructureCreak", 4f, (t, i) =>
                Mathf.Sin(2f * Mathf.PI * (67f + 7f * Mathf.Sin(2f * Mathf.PI * 0.5f * t)) * t) *
                (0.18f + 0.08f * Mathf.Sin(2f * Mathf.PI * 3f * t)));
            clips[GameplayAudioCue.QuakeStart] = NoiseBurst("CEVR_QuakeStart", 0.65f, 44f, 0.42f, 3);
            clips[GameplayAudioCue.QuakeEnd] = NoiseBurst("CEVR_QuakeEnd", 0.50f, 86f, 0.24f, 7);
            clips[GameplayAudioCue.Footstep] = NoiseBurst("CEVR_Footstep", 0.18f, 92f, 0.52f, 11);
            clips[GameplayAudioCue.FurnitureGrab] = NoiseBurst("CEVR_ChairGrab", 0.22f, 145f, 0.55f, 29);
            clips[GameplayAudioCue.FurnitureRelease] = NoiseBurst("CEVR_ChairRelease", 0.28f, 105f, 0.58f, 37);
            clips[GameplayAudioCue.FurnitureScrape] = NoiseBurst("CEVR_ChairScrape", 0.24f, 76f, 0.38f, 43);
            clips[GameplayAudioCue.FurnitureImpact] = NoiseBurst("CEVR_FurnitureImpact", 0.34f, 62f, 0.68f, 53);
            clips[GameplayAudioCue.WindowCrack] = Crack("CEVR_WindowCrack", 0.62f, 71);
            clips[GameplayAudioCue.FootwearEquipped] = NoiseBurst("CEVR_ShoesOn", 0.30f, 180f, 0.30f, 83);
            clips[GameplayAudioCue.PillowGrab] = NoiseBurst("CEVR_PillowRustle", 0.28f, 220f, 0.20f, 97);
            clips[GameplayAudioCue.LightBreak] = Crack("CEVR_LightBreak", 0.40f, 113);
            clips[GameplayAudioCue.PlayerHit] = NoiseBurst("CEVR_PlayerHit", 0.38f, 54f, 0.60f, 127);
            clips[GameplayAudioCue.Success] = Periodic("CEVR_Success", 0.85f, (t, i) =>
            {
                float frequency = t < 0.28f ? 523.25f : t < 0.56f ? 659.25f : 783.99f;
                return Mathf.Sin(2f * Mathf.PI * frequency * t) * Mathf.Exp(-t * 1.8f) * 0.28f;
            });
            clips[GameplayAudioCue.Failure] = Periodic("CEVR_Failure", 0.75f, (t, i) =>
                Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(180f, 92f, t / 0.75f) * t) * Mathf.Exp(-t * 2.2f) * 0.34f);
        }

        private AudioClip Periodic(string clipName, float seconds, Func<float, int, float> sample)
        {
            const int sampleRate = 44100;
            int count = Mathf.CeilToInt(seconds * sampleRate);
            var data = new float[count];
            for (int i = 0; i < count; i++) data[i] = Mathf.Clamp(sample((float)i / sampleRate, i), -1f, 1f);
            return Store(clipName, data, sampleRate);
        }

        private AudioClip NoiseBurst(string clipName, float seconds, float toneHz, float noiseMix, int seed)
        {
            int state = seed;
            return Periodic(clipName, seconds, (t, i) =>
            {
                state = unchecked(state * 1103515245 + 12345);
                float noise = ((state >> 8) & 65535) / 32767.5f - 1f;
                float decay = Mathf.Exp(-t * 15f);
                return (Mathf.Sin(2f * Mathf.PI * toneHz * t) * (1f - noiseMix) + noise * noiseMix) * decay * 0.75f;
            });
        }

        private AudioClip Crack(string clipName, float seconds, int seed)
        {
            int state = seed;
            return Periodic(clipName, seconds, (t, i) =>
            {
                state = unchecked(state * 1664525 + 1013904223);
                float noise = ((state >> 9) & 32767) / 16383.5f - 1f;
                float burst = Mathf.Exp(-t * 12f) + 0.7f * Mathf.Exp(-Mathf.Abs(t - 0.12f) * 45f) +
                              0.45f * Mathf.Exp(-Mathf.Abs(t - 0.27f) * 55f);
                return noise * Mathf.Clamp01(burst) * 0.82f;
            });
        }

        private AudioClip Store(string clipName, float[] data, int sampleRate)
        {
            AudioClip clip = AudioClip.Create(clipName, data.Length, 1, sampleRate, false);
            clip.SetData(data, 0);
            ownedClips.Add(clip);
            return clip;
        }

        private void OnDestroy()
        {
            if (motion != null)
            {
                motion.QuakeStarted -= OnQuakeStarted;
                motion.QuakeEnded -= OnQuakeEnded;
            }
            foreach (AudioClip clip in ownedClips) if (clip != null) Destroy(clip);
            ownedClips.Clear();
            clips.Clear();
            if (Active == this) Active = null;
        }
    }
}
