using System;
using System.Collections.Generic;
using UnityEngine;

namespace ChulaEarthquakeVR
{
    public sealed class TutorialTaskSequence : MonoBehaviour
    {
        [SerializeField] private List<TutorialTask> tasks = new List<TutorialTask>();
        public int TotalCount => tasks.Count;
        public int CompletedCount { get; private set; }
        public bool AllComplete => TotalCount == 0 || CompletedCount >= TotalCount;
        public event Action<int, int, TutorialTask> ProgressChanged;

        public TutorialTask NextIncomplete
        {
            get
            {
                foreach (TutorialTask task in tasks)
                    if (task != null && !task.IsComplete) return task;
                return null;
            }
        }

        private void OnEnable()
        {
            foreach (TutorialTask task in tasks) if (task != null) task.Completed += OnTaskCompleted;
        }

        private void OnDisable()
        {
            foreach (TutorialTask task in tasks) if (task != null) task.Completed -= OnTaskCompleted;
        }

        public void Configure(IEnumerable<TutorialTask> taskList)
        {
            if (isActiveAndEnabled) OnDisable();
            tasks = taskList == null ? new List<TutorialTask>() : new List<TutorialTask>(taskList);
            if (isActiveAndEnabled) OnEnable();
            ResetSequence();
        }

        public void ResetSequence()
        {
            CompletedCount = 0;
            foreach (TutorialTask task in tasks) if (task != null) task.ResetTask();
            ProgressChanged?.Invoke(CompletedCount, TotalCount, null);
        }

        private void OnTaskCompleted(TutorialTask task)
        {
            CompletedCount = 0;
            foreach (TutorialTask item in tasks) if (item != null && item.IsComplete) CompletedCount++;
            ProgressChanged?.Invoke(CompletedCount, TotalCount, task);
        }
    }
}
