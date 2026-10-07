using UnityEngine;

namespace ChulaEarthquakeVR
{
    /// <summary>
    /// Keeps a serialized House visual under HouseFurniture while making it follow the corresponding
    /// runtime gameplay/physics anchor. This preserves one editable scene hierarchy and avoids
    /// reparenting the saved furniture during Play.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HouseVisualRuntimeFollower : MonoBehaviour
    {
        private Transform target;
        private Vector3 targetLocalPositionOffset;
        private Quaternion targetLocalRotationOffset;

        public void Configure(Transform runtimeTarget)
        {
            target = runtimeTarget;
            if (target == null) return;

            targetLocalPositionOffset = target.InverseTransformPoint(transform.position);
            targetLocalRotationOffset = Quaternion.Inverse(target.rotation) * transform.rotation;
            Apply();
        }

        private void LateUpdate()
        {
            Apply();
        }

        private void Apply()
        {
            if (target == null) return;
            transform.position = target.TransformPoint(targetLocalPositionOffset);
            transform.rotation = target.rotation * targetLocalRotationOffset;
        }
    }
}
