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

        private GUIStyle markerStyle;
        private void OnGUI()
        {
            Camera camera = Camera.main;
            if (task == null || task.IsComplete || camera == null || camera.stereoEnabled) return;
            GameFlowController flow = FindFirstObjectByType<GameFlowController>();
            if (flow == null || !flow.IsTraining) return;
            if (markerStyle == null)
                markerStyle = new GUIStyle(GUI.skin.box) { fontSize = 15, alignment = TextAnchor.MiddleCenter };
            Vector3 screen = camera.WorldToScreenPoint(transform.position + Vector3.up * 0.25f);
            if (screen.z <= 0 || screen.x < 0 || screen.x > Screen.width || screen.y < 0 || screen.y > Screen.height) return;
            GUI.Box(new Rect(screen.x - 115f, Screen.height - screen.y - 20f, 230f, 42f),
                "PLACE " + expectedItemId.Replace('-', ' ').ToUpperInvariant() + " HERE", markerStyle);
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
