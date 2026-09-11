using UnityEngine;
using UnityEngine.UI;

namespace ChulaEarthquakeVR
{
    public sealed class TutorialHud : MonoBehaviour
    {
        [SerializeField] private Text phaseText;
        [SerializeField] private Text objectiveText;
        [SerializeField] private Text timerText;
        [SerializeField] private Text taskText;
        [SerializeField] private Slider healthSlider;
        [SerializeField] private Image quakeIndicator;

        public void Configure(Text phase, Text objective, Text timer, Text tasks, Slider health, Image indicator)
        {
            phaseText = phase;
            objectiveText = objective;
            timerText = timer;
            taskText = tasks;
            healthSlider = health;
            quakeIndicator = indicator;
        }

        public void SetPhase(GameplayPhase phase, string objective)
        {
            if (phaseText != null)
            {
                phaseText.text = System.Text.RegularExpressions.Regex.Replace(phase.ToString(), "([a-z])([A-Z])", "$1 $2").ToUpperInvariant();
                phaseText.color = PhaseColor(phase);
            }
            if (objectiveText != null) objectiveText.text = objective ?? string.Empty;
            SetQuakeIndicator(phase == GameplayPhase.Earthquake, 0f);
        }

        public void SetObjective(string text)
        {
            if (objectiveText != null) objectiveText.text = text;
        }

        public void SetTimer(float seconds, string label)
        {
            if (timerText != null) timerText.text = $"{label}: {Mathf.Max(0f, seconds):0.0}s";
        }

        public void SetTaskProgress(int complete, int total, string latest)
        {
            if (taskText == null) return;
            taskText.text = string.Empty;
        }

        public void SetHealth(float current, float maximum)
        {
            if (healthSlider == null) return;
            healthSlider.minValue = 0f;
            healthSlider.maxValue = Mathf.Max(1f, maximum);
            healthSlider.value = current;
        }

        public void SetQuakeIndicator(bool active, float intensity)
        {
            if (quakeIndicator == null) return;
            quakeIndicator.enabled = active;
            quakeIndicator.color = new Color(0.95f, 0.18f, 0.10f, Mathf.Lerp(0.35f, 0.85f, intensity));
        }

        private static Color PhaseColor(GameplayPhase phase)
        {
            return phase switch
            {
                GameplayPhase.Earthquake => new Color(1f, 0.28f, 0.12f),
                GameplayPhase.PostQuakeEvacuation => new Color(0.12f, 0.86f, 0.46f),
                GameplayPhase.Success => new Color(0.12f, 0.9f, 0.48f),
                GameplayPhase.Failure => new Color(1f, 0.16f, 0.18f),
                GameplayPhase.Debrief => new Color(0.45f, 0.72f, 1f),
                _ => new Color(1f, 0.25f, 0.55f)
            };
        }
    }
}
