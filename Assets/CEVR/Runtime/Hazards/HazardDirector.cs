using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ChulaEarthquakeVR
{
    public sealed class HazardDirector : MonoBehaviour
    {
        [SerializeField] private List<Rigidbody> stagedHazards = new List<Rigidbody>();
        [SerializeField, Min(0.05f)] private float releaseIntervalSeconds = 1.2f;
        [SerializeField, Min(0f)] private float firstReleaseDelaySeconds = 2f;
        private Coroutine releaseRoutine;
        private Vector3[] initialPositions = System.Array.Empty<Vector3>();
        private Quaternion[] initialRotations = System.Array.Empty<Quaternion>();

        private void Awake() => CaptureInitialPoses();

        public void Configure(IEnumerable<Rigidbody> hazards)
        {
            stagedHazards = hazards == null ? new List<Rigidbody>() : new List<Rigidbody>(hazards);
            CaptureInitialPoses();
            ResetHazards();
        }

        public void BeginHazards(bool damageEnabled)
        {
            StopHazards();
            foreach (Rigidbody body in stagedHazards)
            {
                if (body == null) continue;
                FallingHazard hazard = body.GetComponent<FallingHazard>();
                if (hazard != null) hazard.Arm(damageEnabled);
            }
            releaseRoutine = StartCoroutine(ReleaseRoutine());
        }

        public void StopHazards()
        {
            if (releaseRoutine != null) StopCoroutine(releaseRoutine);
            releaseRoutine = null;
            foreach (Rigidbody body in stagedHazards)
                if (body != null && body.TryGetComponent(out FallingHazard hazard)) hazard.Arm(false);
        }

        public void ResetHazards()
        {
            StopHazards();
            if (initialPositions.Length != stagedHazards.Count) CaptureInitialPoses();
            for (int i = 0; i < stagedHazards.Count; i++)
            {
                Rigidbody body = stagedHazards[i];
                if (body == null) continue;
                body.isKinematic = true;
                body.useGravity = false;
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
                if (i < initialPositions.Length)
                {
                    body.position = initialPositions[i];
                    body.rotation = initialRotations[i];
                }
            }
        }

        private void CaptureInitialPoses()
        {
            initialPositions = new Vector3[stagedHazards.Count];
            initialRotations = new Quaternion[stagedHazards.Count];
            for (int i = 0; i < stagedHazards.Count; i++)
            {
                Rigidbody body = stagedHazards[i];
                if (body == null) continue;
                initialPositions[i] = body.position;
                initialRotations[i] = body.rotation;
            }
        }

        private IEnumerator ReleaseRoutine()
        {
            yield return new WaitForSecondsRealtime(firstReleaseDelaySeconds);
            foreach (Rigidbody body in stagedHazards)
            {
                if (body == null) continue;
                body.isKinematic = false;
                body.useGravity = true;
                yield return new WaitForSecondsRealtime(releaseIntervalSeconds);
            }
            releaseRoutine = null;
        }
    }
}
