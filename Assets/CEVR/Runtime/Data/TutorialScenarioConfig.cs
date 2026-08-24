using UnityEngine;

namespace ChulaEarthquakeVR
{
    [CreateAssetMenu(fileName = "TutorialScenario", menuName = "CEVR/Tutorial Scenario Config")]
    public sealed class TutorialScenarioConfig : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string scenarioId = "CEVR_CHULA_LAB_TUTORIAL_V1";
        [SerializeField] private StudyMode mode = StudyMode.Training;
        [SerializeField, TextArea] private string locationNote =
            "Fictionalized Chulalongkorn Engineering-inspired teaching lab. Not an official digital twin.";

        [Header("Timing")]
        [SerializeField, Min(1f)] private float orientationSeconds = 6f;
        [SerializeField, Min(5f)] private float normalActivitySeconds = 30f;
        [SerializeField, Min(5f)] private float taskWatchdogSeconds = 120f;
        [SerializeField, Min(5f)] private float earthquakeSeconds = 20f;
        [SerializeField, Min(10f)] private float evacuationTimeoutSeconds = 60f;

        [Header("Earthquake preview")]
        [SerializeField, Range(0.01f, 0.5f)] private float previewPeakG = 0.16f;
        [SerializeField, Min(0.1f)] private float previewFrequencyHz = 2.1f;
        [SerializeField] private int deterministicSeed = 20260824;

        [Header("Rules")]
        [SerializeField] private bool requireAllNormalActivityTasks = true;
        [SerializeField] private bool damageEnabled = true;
        [SerializeField, Min(1f)] private float maximumHealth = 100f;

        [Header("Training prompts")]
        [SerializeField] private string orientationPrompt =
            "Look around the engineering lab and learn the controls.";
        [SerializeField] private string activityPrompt =
            "Complete the two lab setup tasks before the class begins.";
        [SerializeField] private string quakePrompt =
            "EARTHQUAKE: Drop, take cover under the sturdy table, and hold on.";
        [SerializeField] private string evacuationPrompt =
            "Shaking has stopped. Walk to the green exit and assembly point.";

        [Header("Research prompts — neutral wording")]
        [SerializeField] private string neutralActivityPrompt =
            "Continue the assigned activity naturally.";
        [SerializeField] private string neutralQuakePrompt =
            "An environmental event has started. Act as you think appropriate. You may stop at any time.";
        [SerializeField] private string neutralEvacuationPrompt =
            "The environmental event has ended. Continue as you think appropriate.";

        public string ScenarioId => scenarioId;
        public StudyMode Mode => mode;
        public string LocationNote => locationNote;
        public float OrientationSeconds => orientationSeconds;
        public float NormalActivitySeconds => normalActivitySeconds;
        public float TaskWatchdogSeconds => taskWatchdogSeconds;
        public float EarthquakeSeconds => earthquakeSeconds;
        public float EvacuationTimeoutSeconds => evacuationTimeoutSeconds;
        public float PreviewPeakG => previewPeakG;
        public float PreviewFrequencyHz => previewFrequencyHz;
        public int DeterministicSeed => deterministicSeed;
        public bool RequireAllNormalActivityTasks => requireAllNormalActivityTasks;
        public bool DamageEnabled => damageEnabled;
        public float MaximumHealth => maximumHealth;

        public string PromptFor(GameplayPhase phase)
        {
            bool training = mode == StudyMode.Training;
            return phase switch
            {
                GameplayPhase.Orientation => orientationPrompt,
                GameplayPhase.NormalActivity => training ? activityPrompt : neutralActivityPrompt,
                GameplayPhase.Earthquake => training ? quakePrompt : neutralQuakePrompt,
                GameplayPhase.PostQuakeEvacuation => training ? evacuationPrompt : neutralEvacuationPrompt,
                GameplayPhase.Success => "Tutorial complete. You reached the assembly point safely.",
                GameplayPhase.Failure => "Simulation stopped. Please remove the headset only when seated safely.",
                GameplayPhase.Debrief => "Simulation complete. Follow the facilitator's debrief instructions.",
                _ => string.Empty
            };
        }

        public void ConfigureForBuilder(StudyMode studyMode)
        {
            mode = studyMode;
        }
    }
}
