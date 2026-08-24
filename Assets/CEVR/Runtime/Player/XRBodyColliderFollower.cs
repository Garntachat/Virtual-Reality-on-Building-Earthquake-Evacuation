using UnityEngine;

namespace ChulaEarthquakeVR
{
    [RequireComponent(typeof(CapsuleCollider))]
    public sealed class XRBodyColliderFollower : MonoBehaviour
    {
        [SerializeField] private Transform head;
        [SerializeField, Min(0.2f)] private float radius = 0.22f;
        [SerializeField, Min(0.5f)] private float minimumHeight = 0.6f;
        [SerializeField, Min(1f)] private float maximumHeight = 2.1f;
        private CapsuleCollider body;

        private void Awake() => body = GetComponent<CapsuleCollider>();

        private void LateUpdate()
        {
            if (head == null) return;
            Vector3 localHead = transform.InverseTransformPoint(head.position);
            float height = Mathf.Clamp(localHead.y, minimumHeight, maximumHeight);
            body.radius = Mathf.Min(radius, height * 0.45f);
            body.height = height;
            body.center = new Vector3(localHead.x, height * 0.5f, localHead.z);
        }

        public void Configure(Transform headTransform) => head = headTransform;
    }
}
