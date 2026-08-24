using System;
using UnityEngine;

namespace ChulaEarthquakeVR
{
    [Serializable]
    public struct FloorResponseState
    {
        public Vector3 relativeDisplacement;
        public Vector3 relativeVelocity;
        public void Reset() { relativeDisplacement = Vector3.zero; relativeVelocity = Vector3.zero; }
    }

    [CreateAssetMenu(fileName = "FloorResponse", menuName = "CEVR/Floor Response Profile")]
    public sealed class FloorResponseProfile : ScriptableObject
    {
        [SerializeField] private string profileId = "floor-response-unconfigured";
        [SerializeField, Min(0.05f)] private float naturalPeriodSeconds = 0.6f;
        [SerializeField, Range(0.01f, 0.30f)] private float dampingRatio = 0.05f;
        [SerializeField, Min(0.1f)] private float horizontalGain = 1f;
        [SerializeField, Min(0f)] private float verticalGain = 1f;
        [SerializeField, Min(0.1f)] private float maxOutputG = 1.5f;
        [SerializeField, TextArea] private string calibrationNote =
            "Reduced-order SDOF response. It is not a structural analysis and must be calibrated before research use.";

        public string ProfileId => string.IsNullOrWhiteSpace(profileId) ? name : profileId;
        public string CalibrationNote => calibrationNote;

        public Vector3 Step(Vector3 groundAccelerationMs2, float deltaTime, ref FloorResponseState state)
        {
            if (deltaTime <= 0f) return Vector3.zero;
            float period = Mathf.Max(0.05f, naturalPeriodSeconds);
            float omega = 2f * Mathf.PI / period;
            Vector3 input = new Vector3(
                groundAccelerationMs2.x * horizontalGain,
                groundAccelerationMs2.y * verticalGain,
                groundAccelerationMs2.z * horizontalGain);
            float preferredStep = period / 20f;
            int substeps = Mathf.Clamp(Mathf.CeilToInt(deltaTime / preferredStep), 1, 16);
            float stepDt = deltaTime / substeps;
            Vector3 relativeAcceleration = Vector3.zero;
            for (int i = 0; i < substeps; i++)
            {
                relativeAcceleration = -input
                    - 2f * dampingRatio * omega * state.relativeVelocity
                    - omega * omega * state.relativeDisplacement;
                state.relativeVelocity += relativeAcceleration * stepDt;
                state.relativeDisplacement += state.relativeVelocity * stepDt;
            }
            float limitMs2 = maxOutputG * Physics.gravity.magnitude;
            return Vector3.ClampMagnitude(relativeAcceleration + input, limitMs2);
        }
    }
}
