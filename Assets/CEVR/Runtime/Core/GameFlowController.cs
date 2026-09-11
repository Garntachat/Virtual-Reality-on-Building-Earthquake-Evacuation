using System;
using System.Collections;
using System.Globalization;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ChulaEarthquakeVR
{
    public sealed class GameFlowController : MonoBehaviour
    {
        [Header("Scenario")]
        [SerializeField] private TutorialScenarioConfig config;
        [SerializeField] private string participantCode = "TUTORIAL";
        [SerializeField] private bool autoStart = true;

        [Header("Systems")]
        [SerializeField] private GroundMotionPlayer motion;
        [SerializeField] private TutorialTaskSequence taskSequence;
        [SerializeField] private HazardDirector hazardDirector;
        [SerializeField] private PlayerHealth health;
        [SerializeField] private CoverZone coverZone;
        [SerializeField] private ExitAssemblyZone assemblyZone;
        [SerializeField] private TutorialHud hud;
        [SerializeField] private SessionLogger logger;

        private Coroutine runRoutine;
        private bool assemblyReached;
        private bool aborted;
        public GameplayPhase CurrentPhase { get; private set; } = GameplayPhase.Boot;

        private void OnEnable()
        {
            if (taskSequence != null) taskSequence.ProgressChanged += OnTaskProgress;
            if (health != null)
            {
                health.Damaged += OnDamaged;
                health.Died += OnDied;
            }
            if (coverZone != null) coverZone.OccupancyChanged += OnCoverChanged;
            if (assemblyZone != null) assemblyZone.Entered += OnAssemblyEntered;
        }

        private void OnDisable()
        {
            if (taskSequence != null) taskSequence.ProgressChanged -= OnTaskProgress;
            if (health != null)
            {
                health.Damaged -= OnDamaged;
                health.Died -= OnDied;
            }
            if (coverZone != null) coverZone.OccupancyChanged -= OnCoverChanged;
            if (assemblyZone != null) assemblyZone.Entered -= OnAssemblyEntered;
            if (runRoutine != null) StopCoroutine(runRoutine);
            runRoutine = null;
            motion?.StopQuake();
            hazardDirector?.StopHazards();
            logger?.EndSession();
        }

        private void Start()
        {
            if (autoStart) StartTutorial();
        }

        private void Update()
        {
            if (hud != null && motion != null && CurrentPhase == GameplayPhase.Earthquake)
                hud.SetQuakeIndicator(true, motion.PresentationIntensity);
        }

        public void Configure(
            TutorialScenarioConfig scenario,
            GroundMotionPlayer motionPlayer,
            TutorialTaskSequence tasks,
            HazardDirector hazards,
            PlayerHealth playerHealth,
            CoverZone cover,
            ExitAssemblyZone exit,
            TutorialHud tutorialHud,
            SessionLogger sessionLogger)
        {
            config = scenario;
            motion = motionPlayer;
            taskSequence = tasks;
            hazardDirector = hazards;
            health = playerHealth;
            coverZone = cover;
            assemblyZone = exit;
            hud = tutorialHud;
            logger = sessionLogger;
        }

        public void SetParticipantCode(string code) => participantCode = code;

        public void StartTutorial()
        {
            if (runRoutine != null) return;
            RuntimeStageRepair.EnsurePlayableStage();
            if (!ValidateSetup(out string error))
            {
                Debug.LogError(error, this);
                hud?.SetPhase(GameplayPhase.Failure, error);
                return;
            }
            if (config.Mode == StudyMode.Research && motion.IsUsingPreview)
            {
                const string message = "Research mode requires a validated recorded QuakeProfile; preview motion is tutorial-only.";
                Debug.LogError(message, this);
                hud?.SetPhase(GameplayPhase.Failure, message);
                return;
            }
            aborted = false;
            assemblyReached = false;
            health.Configure(config.MaximumHealth);
            hud.SetHealth(health.CurrentHealth, health.MaximumHealth);
            taskSequence.ResetSequence();
            hazardDirector.ResetHazards();
            assemblyZone.SetSuccessEnabled(false);
            if (config.Mode == StudyMode.Training)
                motion.ConfigurePreview(config.EarthquakeSeconds, config.PreviewPeakG,
                    config.PreviewFrequencyHz, config.DeterministicSeed);
            try
            {
                logger.BeginSession(participantCode, config.ScenarioId, config.Mode);
            }
            catch (Exception exception)
            {
                string message = $"Could not create the session log: {exception.Message}";
                Debug.LogError(message, this);
                hud.SetPhase(GameplayPhase.Failure, message);
                CurrentPhase = GameplayPhase.Failure;
                return;
            }
            logger.LogEvent("scenario_configured",
                $"{{\"location\":\"chula_engineering_inspired_fictional\"," +
                $"\"previewMotion\":{motion.IsUsingPreview.ToString().ToLowerInvariant()}," +
                $"\"motionProfile\":\"{JsonEscape(motion.ActiveProfileId)}\"," +
                $"\"floorResponse\":\"{JsonEscape(motion.ActiveFloorResponseId)}\"," +
                $"\"durationSeconds\":{Number(motion.DurationSeconds, "F3")}}}");
            runRoutine = StartCoroutine(RunTutorial());
        }

        public void AbortTutorial(string reason = "participant_or_facilitator_stop")
        {
            if (runRoutine == null) return;
            aborted = true;
            StopCoroutine(runRoutine);
            runRoutine = null;
            logger?.LogEvent("trial_aborted", $"{{\"reason\":\"{JsonEscape(reason)}\"}}");
            motion?.StopQuake();
            hazardDirector?.StopHazards();
            Transition(GameplayPhase.Failure);
            logger?.EndSession();
        }

        public bool IsTraining => config != null && config.Mode == StudyMode.Training;

        public void RestartCurrentScene()
        {
            logger?.EndSession();
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        private IEnumerator RunTutorial()
        {
            Transition(GameplayPhase.Orientation);
            yield return WaitPhase(config.OrientationSeconds, "ORIENTATION");

            Transition(GameplayPhase.NormalActivity);
            float activityElapsed = 0f;
            while (!aborted && !TutorialRunRules.CanStartEarthquake(
                       activityElapsed,
                       config.NormalActivitySeconds,
                       config.RequireAllNormalActivityTasks,
                       taskSequence.AllComplete,
                       config.TaskWatchdogSeconds))
            {
                activityElapsed += Time.unscaledDeltaTime;
                bool waitingForTasks = activityElapsed >= config.NormalActivitySeconds && !taskSequence.AllComplete;
                hud.SetTimer(waitingForTasks ? config.TaskWatchdogSeconds - activityElapsed :
                    config.NormalActivitySeconds - activityElapsed, waitingForTasks ? "TASK TIME LEFT" : "PREPARATION");
                if (config.Mode == StudyMode.Training)
                {
                    hud.SetObjective("Explore for 30 seconds. E: move a chair or pick up an item. Find the sturdy table.");
                }
                yield return null;
            }
            if (aborted) yield break;
            if (config.RequireAllNormalActivityTasks && !taskSequence.AllComplete)
                logger.LogEvent("task_watchdog_continued", $"{{\"elapsed\":{Number(activityElapsed)}}}");

            Transition(GameplayPhase.Earthquake);
            logger.LogEvent("earthquake_onset");
            hazardDirector.BeginHazards(config.DamageEnabled);
            motion.StartQuake();
            while (!aborted && !health.IsDead && motion.IsPlaying)
            {
                hud.SetTimer(motion.DurationSeconds - motion.ElapsedSeconds, "SHAKING");
                yield return null;
            }
            motion.StopQuake();
            hazardDirector.StopHazards();
            if (aborted) yield break;
            if (health.IsDead)
            {
                yield return FinishFailure("health_depleted");
                yield break;
            }

            logger.LogEvent("earthquake_ended");
            Transition(GameplayPhase.PostQuakeEvacuation);
            assemblyZone.SetSuccessEnabled(true);
            float evacuationElapsed = 0f;
            while (!aborted && !assemblyReached && !TutorialRunRules.IsFailure(
                       health.CurrentHealth, evacuationElapsed, config.EvacuationTimeoutSeconds))
            {
                evacuationElapsed += Time.unscaledDeltaTime;
                hud.SetTimer(config.EvacuationTimeoutSeconds - evacuationElapsed, "EVACUATE");
                yield return null;
            }
            if (aborted) yield break;
            if (!assemblyReached)
            {
                yield return FinishFailure(health.IsDead ? "health_depleted" : "evacuation_timeout");
                yield break;
            }

            Transition(GameplayPhase.Success);
            logger.LogEvent("tutorial_success", $"{{\"healthRemaining\":{Number(health.CurrentHealth)}}}");
            yield return new WaitForSecondsRealtime(3f);
            Transition(GameplayPhase.Debrief);
            logger.LogEvent("debrief_ready");
            logger.EndSession();
            runRoutine = null;
        }

        private IEnumerator WaitPhase(float duration, string label)
        {
            float elapsed = 0f;
            while (!aborted && elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                hud.SetTimer(duration - elapsed, label);
                yield return null;
            }
        }

        private IEnumerator FinishFailure(string reason)
        {
            Transition(GameplayPhase.Failure);
            logger.LogEvent("tutorial_failure", $"{{\"reason\":\"{reason}\"}}");
            yield return new WaitForSecondsRealtime(3f);
            Transition(GameplayPhase.Debrief);
            logger.EndSession();
            runRoutine = null;
        }

        private void Transition(GameplayPhase next)
        {
            CurrentPhase = next;
            hud?.SetPhase(next, config == null ? string.Empty : config.PromptFor(next));
            logger?.LogEvent("phase_changed", $"{{\"phase\":\"{next}\"}}");
        }

        private void OnTaskProgress(int complete, int total, TutorialTask task)
        {
            hud?.SetTaskProgress(complete, total, task == null ? string.Empty : task.Description);
            if (task != null) logger?.LogEvent("task_completed", $"{{\"taskId\":\"{JsonEscape(task.TaskId)}\"}}");
        }

        private void OnDamaged(float damage, float remaining, string source)
        {
            hud?.SetHealth(remaining, health.MaximumHealth);
            logger?.LogEvent("player_damaged",
                $"{{\"damage\":{Number(damage)},\"remaining\":{Number(remaining)}," +
                $"\"source\":\"{JsonEscape(source)}\",\"protected\":{health.IsProtected.ToString().ToLowerInvariant()}}}");
        }

        private void OnDied() => logger?.LogEvent("player_health_depleted");

        private void OnCoverChanged(bool entered, string zoneId)
        {
            logger?.LogEvent(entered ? "cover_enter" : "cover_exit", $"{{\"zoneId\":\"{JsonEscape(zoneId)}\"}}");
        }

        private void OnAssemblyEntered(bool successEnabled, string zoneId)
        {
            if (!successEnabled)
            {
                logger?.LogEvent("unsafe_exit_attempt", $"{{\"zoneId\":\"{JsonEscape(zoneId)}\",\"phase\":\"{CurrentPhase}\"}}");
                return;
            }
            assemblyReached = TutorialRunRules.IsSuccessfulEvacuation(CurrentPhase, true, health.CurrentHealth);
            logger?.LogEvent("assembly_enter", $"{{\"zoneId\":\"{JsonEscape(zoneId)}\",\"accepted\":{assemblyReached.ToString().ToLowerInvariant()}}}");
        }

        private bool ValidateSetup(out string error)
        {
            GeneratedStageInfo stageInfo = FindFirstObjectByType<GeneratedStageInfo>();
            if (stageInfo == null || !stageInfo.IsCurrent)
            {
                string found = stageInfo == null ? "missing" : stageInfo.BuildVersion;
                error = $"Generated scene is stale (found {found}, expected {GeneratedStageInfo.CurrentVersion}). " +
                        "Stop Play mode and run Tools > CEVR > 1. Build Chula Engineering Tutorial Stage.";
                return false;
            }
            if (config == null) { error = "Missing TutorialScenarioConfig."; return false; }
            if (motion == null || taskSequence == null || hazardDirector == null || health == null ||
                coverZone == null || assemblyZone == null || hud == null || logger == null)
            {
                error = "Tutorial scene wiring is incomplete. Rebuild the stage or check GameFlowController references.";
                return false;
            }
            error = string.Empty;
            return true;
        }

        private static string JsonEscape(string value)
        {
            return (value ?? string.Empty)
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\r", "\\r")
                .Replace("\n", "\\n");
        }

        private static string Number(float value, string format = "F2") =>
            value.ToString(format, CultureInfo.InvariantCulture);
    }
}
