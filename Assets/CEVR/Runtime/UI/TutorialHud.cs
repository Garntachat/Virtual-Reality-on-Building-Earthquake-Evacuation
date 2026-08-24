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
            if (phaseText != null) phaseText.text = phase.ToString().ToUpperInvariant();
            if (objectiveText != null) objectiveText.text = objective ?? string.Empty;
            SetQuakeIndicator(phase == GameplayPhase.Earthquake, 0f);
        }

        public void SetTimer(float seconds, string label)
        {
            if (timerText != null) timerText.text = $"{label}: {Mathf.Max(0f, seconds):0.0}s";
        }

        public void SetTaskProgress(int complete, int total, string latest)
        {
            if (taskText == null) return;
            string detail = string.IsNullOrWhiteSpace(latest) ? string.Empty : $"\nCompleted: {latest}";
            taskText.text = $"LAB TASKS: {complete}/{total}{detail}";
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
    }
}
