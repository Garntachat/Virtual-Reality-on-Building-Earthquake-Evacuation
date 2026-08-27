using System.Globalization;
using UnityEngine;

namespace ChulaEarthquakeVR
{
    public sealed class PoseTelemetrySampler : MonoBehaviour
    {
        [SerializeField] private SessionLogger logger;
        [SerializeField] private Transform head;
        [SerializeField] private Transform leftHand;
        [SerializeField] private Transform rightHand;
        [SerializeField, Range(1f, 20f)] private float sampleRateHz = 10f;
        private float nextSampleTime;

        private void Update()
        {
            if (logger == null || !logger.IsSessionOpen || head == null || Time.unscaledTime < nextSampleTime) return;
            nextSampleTime = Time.unscaledTime + 1f / sampleRateHz;
            Vector3 lp = leftHand == null ? Vector3.zero : leftHand.position;
            Vector3 rp = rightHand == null ? Vector3.zero : rightHand.position;
            Quaternion hq = head.rotation;
            logger.LogEvent("pose_sample",
                $"{{\"hx\":{N(head.position.x)},\"hy\":{N(head.position.y)},\"hz\":{N(head.position.z)}," +
                $"\"hqx\":{N(hq.x)},\"hqy\":{N(hq.y)},\"hqz\":{N(hq.z)},\"hqw\":{N(hq.w)}," +
                $"\"lx\":{N(lp.x)},\"ly\":{N(lp.y)},\"lz\":{N(lp.z)}," +
                $"\"rx\":{N(rp.x)},\"ry\":{N(rp.y)},\"rz\":{N(rp.z)}," +
                $"\"leftTracked\":{B(leftHand != null)},\"rightTracked\":{B(rightHand != null)}}}");
        }

        public void Configure(SessionLogger sessionLogger, Transform headTransform, Transform left, Transform right)
        {
            logger = sessionLogger;
            head = headTransform;
            leftHand = left;
            rightHand = right;
        }

        private static string N(float value) => value.ToString("F3", CultureInfo.InvariantCulture);
        private static string B(bool value) => value.ToString().ToLowerInvariant();
    }
}
