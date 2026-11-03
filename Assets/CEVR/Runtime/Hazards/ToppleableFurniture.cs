using UnityEngine;

namespace ChulaEarthquakeVR
{
    // Optional tipping SETTINGS only: does not apply a second earthquake force.
    // InertialRigidbody applies the one force higher up on tall objects.
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class ToppleableFurniture : MonoBehaviour
    {
        [SerializeField] private GroundMotionPlayer motion;
        [SerializeField] private bool anchoredToStructure;
        [SerializeField, Min(0f)] private float upperForceScale = 1.35f;
        [SerializeField, Min(0.1f)] private float forceHeight = 1.4f;

        public bool AnchoredToStructure => anchoredToStructure;
        public float UpperForceScale => upperForceScale;
        public float ForceHeight => forceHeight;

        private void Awake()
        {
            if (anchoredToStructure)
                GetComponent<Rigidbody>().isKinematic = true;
        }

        // Keep old Configure signatures so scene builders and saved objects work.
        public void ConnectMotionIfMissing(GroundMotionPlayer source)
        {
            if (motion == null) motion = source;
        }

        public void Configure(GroundMotionPlayer source, float scale, float height)
        {
            motion = source;
            upperForceScale = Mathf.Max(0f, scale);
            forceHeight = Mathf.Max(0.1f, height);
        }
    }
}
