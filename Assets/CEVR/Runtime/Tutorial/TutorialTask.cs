using System;
using UnityEngine;

namespace ChulaEarthquakeVR
{
    public sealed class TutorialTask : MonoBehaviour
    {
        [SerializeField] private string taskId = "task";
        [SerializeField] private string description = "Complete task";
        public string TaskId => taskId;
        public string Description => description;
        public bool IsComplete { get; private set; }
        public event Action<TutorialTask> Completed;

        public void Configure(string id, string taskDescription)
        {
            taskId = string.IsNullOrWhiteSpace(id) ? name : id;
            description = taskDescription ?? string.Empty;
        }

        public void ResetTask() => IsComplete = false;

        public void Complete()
        {
            if (IsComplete) return;
            IsComplete = true;
            Completed?.Invoke(this);
        }
    }
}
