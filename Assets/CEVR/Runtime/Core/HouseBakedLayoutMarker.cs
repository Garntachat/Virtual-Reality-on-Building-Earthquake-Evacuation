using UnityEngine;

namespace ChulaEarthquakeVR
{
    /// <summary>
    /// Marks the normal, serialized House furniture hierarchy baked into House.unity.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HouseBakedLayoutMarker : MonoBehaviour
    {
        public const int CurrentLayoutVersion = 1;
        [SerializeField] private int layoutVersion = CurrentLayoutVersion;

        public int LayoutVersion => layoutVersion;

        public void StampCurrentVersion()
        {
            layoutVersion = CurrentLayoutVersion;
        }
    }
}
