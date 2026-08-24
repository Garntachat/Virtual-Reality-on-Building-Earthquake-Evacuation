using UnityEngine;

namespace ChulaEarthquakeVR
{
    [RequireComponent(typeof(Collider))]
    public sealed class PlacementGoal : MonoBehaviour
    {
        [SerializeField] private string expectedItemId;
        [SerializeField] private TutorialTask task;
        [SerializeField] private Transform snapPoint;

        private void Reset() => GetComponent<Collider>().isTrigger = true;

        private void OnTriggerEnter(Collider other)
        {
            TaskItem item = other.GetComponentInParent<TaskItem>();
            if (item == null || item.ItemId != expectedItemId || task == null || task.IsComplete) return;
            Rigidbody body = item.GetComponent<Rigidbody>();
            if (body != null)
            {
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
                body.isKinematic = true;
            }
            Transform target = snapPoint == null ? transform : snapPoint;
            item.transform.SetPositionAndRotation(target.position, target.rotation);
            task.Complete();
        }

        public void Configure(string itemId, TutorialTask linkedTask, Transform target)
        {
            expectedItemId = itemId;
            task = linkedTask;
            snapPoint = target;
            GetComponent<Collider>().isTrigger = true;
        }
    }
}
