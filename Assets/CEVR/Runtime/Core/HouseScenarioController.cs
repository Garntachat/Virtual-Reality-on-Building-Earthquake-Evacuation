using System;
using System.Collections;
using System.Globalization;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ChulaEarthquakeVR
{
    public sealed class HouseScenarioController : MonoBehaviour
    {
        private enum HousePhase { Preparation, Earthquake, Evacuation, Success, Failure }

        [SerializeField] private float preparationSeconds = 30f;
        [SerializeField] private float earthquakeSeconds = 20f;
        [SerializeField] private float evacuationSeconds = 60f;
        private GroundMotionPlayer motion;
        private HazardDirector hazards;
        private PlayerHealth health;
        private Transform player;
        private Transform assemblyPoint;
        private SessionLogger logger;
        private HousePhase phase;
        private float remaining;
        private Coroutine routine;
        private GUIStyle headingStyle;
        private GUIStyle objectiveStyle;

        public void Configure(
            GroundMotionPlayer motionPlayer, HazardDirector hazardDirector, PlayerHealth playerHealth,
            Transform playerRoot, Transform assembly, SessionLogger sessionLogger)
        {
            motion = motionPlayer;
            hazards = hazardDirector;
            health = playerHealth;
            player = playerRoot;
            assemblyPoint = assembly;
            logger = sessionLogger;
        }

        private void Start()
        {
            if (motion == null || hazards == null || health == null || player == null || assemblyPoint == null)
            {
                Debug.LogError("House scenario wiring is incomplete.", this);
                enabled = false;
                return;
            }
            health.Configure(100f);
            motion.ConfigurePreview(earthquakeSeconds, 0.16f, 2.1f, 20260910);
            hazards.ResetHazards();
            try
            {
                logger?.BeginSession("HOUSE-TUTORIAL", "CEVR_HOUSE_TUTORIAL_V1", StudyMode.Training);
                logger?.LogEvent("house_scenario_started");
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"House tutorial will continue without a session log: {exception.Message}", this);
            }
            routine = StartCoroutine(RunScenario());
        }

        private void Update()
        {
            if (Keyboard.current != null &&
                (Keyboard.current.f12Key.wasPressedThisFrame || Keyboard.current.backspaceKey.wasPressedThisFrame))
                Abort("participant_or_facilitator_stop");
            if (!HasEnded && health != null && health.IsDead) Fail("health_depleted");
        }

        private IEnumerator RunScenario()
        {
            phase = HousePhase.Preparation;
            yield return Countdown(preparationSeconds);

            phase = HousePhase.Earthquake;
            logger?.LogEvent("earthquake_onset");
            hazards.BeginHazards(true);
            motion.StartQuake();
            while (motion.IsPlaying && !health.IsDead)
            {
                remaining = Mathf.Max(0f, motion.DurationSeconds - motion.ElapsedSeconds);
                yield return null;
            }
            motion.StopQuake();
            hazards.StopHazards();
            if (health.IsDead)
            {
                Fail("health_depleted");
                yield break;
            }

            phase = HousePhase.Evacuation;
            logger?.LogEvent("earthquake_ended");
            float elapsed = 0f;
            while (elapsed < evacuationSeconds)
            {
                // Check before the arrival test, including death on the same frame as arrival.
                if (health.IsDead)
                {
                    Fail("health_depleted");
                    yield break;
                }
                remaining = evacuationSeconds - elapsed;
                Vector3 offset = player.position - assemblyPoint.position;
                offset.y = 0f;
                if (offset.magnitude <= 1.45f)
                {
                    phase = HousePhase.Success;
                    GameplayAudioDirector.PlayCue(GameplayAudioCue.Success, 0.32f);
                    logger?.LogEvent("house_tutorial_success",
                        $"{{\"healthRemaining\":{health.CurrentHealth.ToString("0.##", CultureInfo.InvariantCulture)}}}");
                    logger?.EndSession();
                    routine = null;
                    yield break;
                }
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
            Fail("evacuation_timeout");
        }

        private IEnumerator Countdown(float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                remaining = duration - elapsed;
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        private void Abort(string reason)
        {
            if (phase == HousePhase.Success || phase == HousePhase.Failure) return;
            Fail(reason);
        }

        private void Fail(string reason)
        {
            if (phase == HousePhase.Success || phase == HousePhase.Failure) return;
            if (routine != null) StopCoroutine(routine);
            routine = null;
            motion?.StopQuake();
            hazards?.StopHazards();
            phase = HousePhase.Failure;
            GameplayAudioDirector.PlayCue(GameplayAudioCue.Failure, 0.30f);
            logger?.LogEvent("house_tutorial_failure", $"{{\"reason\":\"{reason}\"}}");
            logger?.EndSession();
        }

        private void OnDisable()
        {
            if (routine != null) StopCoroutine(routine);
            routine = null;
            motion?.StopQuake();
            hazards?.StopHazards();
            logger?.EndSession();
        }

        public bool IsQuaking => phase == HousePhase.Earthquake;
        public bool HasEnded => phase == HousePhase.Success || phase == HousePhase.Failure;
        public bool IsEvacuating => phase == HousePhase.Evacuation;

        private void OnGUI()
        {
            EnsureStyles();
            string heading;
            string objective;
            switch (phase)
            {
                case HousePhase.Preparation:
                    heading = $"HOUSE PREPARATION  •  EARTHQUAKE IN {remaining:0.0}s";
                    objective = "Wear the shoes, move furniture, and locate the pillow and safe cover.";
                    break;
                case HousePhase.Earthquake:
                    heading = health.CurrentHealth < health.MaximumHealth - 0.01f
                        ? $"EARTHQUAKE  •  {remaining:0.0}s  •  HEALTH {health.CurrentHealth:0}"
                        : $"EARTHQUAKE  •  {remaining:0.0}s";
                    objective = "Hold the pillow over your head or crawl under the table. Stay away from windows and falling furniture.";
                    break;
                case HousePhase.Evacuation:
                    heading = $"SHAKING STOPPED  •  EVACUATE {remaining:0.0}s";
                    objective = "Move to the green outdoor assembly marker.";
                    break;
                case HousePhase.Success:
                    heading = "HOUSE TUTORIAL COMPLETE";
                    objective = "You reached the assembly point after the shaking stopped.";
                    break;
                default:
                    heading = "SIMULATION STOPPED";
                    objective = "The run ended. Press Play again when ready.";
                    break;
            }
            Camera camera = Camera.main;
            if (camera != null && camera.stereoEnabled) return;
            float width = Mathf.Min(620f, (camera == null ? Screen.width : camera.pixelWidth) - 24f);
            GUI.Box(new Rect(12f, 18f, width, 138f), GUIContent.none);
            GUI.Label(new Rect(24f, 25f, width - 24f, 50f), heading, headingStyle);
            GUI.Label(new Rect(24f, 80f, width - 24f, 70f), objective, objectiveStyle);
        }

        private void EnsureStyles()
        {
            if (headingStyle != null) return;
            headingStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 20,
                fontStyle = FontStyle.Bold
            };
            headingStyle.normal.textColor = new Color(1f, 0.3f, 0.58f);
            objectiveStyle = new GUIStyle(headingStyle) { fontSize = 16, fontStyle = FontStyle.Normal, wordWrap = true };
            objectiveStyle.normal.textColor = Color.white;
        }
    }
}
