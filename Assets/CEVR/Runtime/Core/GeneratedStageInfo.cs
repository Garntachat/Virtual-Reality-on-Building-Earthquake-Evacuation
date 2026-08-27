using UnityEngine;

namespace ChulaEarthquakeVR
{
    [DisallowMultipleComponent]
    public sealed class GeneratedStageInfo : MonoBehaviour
    {
        public const string CurrentVersion = "0.3.0";

        [SerializeField] private string buildVersion = "unbuilt";

        public string BuildVersion => buildVersion;
        public bool IsCurrent => buildVersion == CurrentVersion;

        public void ConfigureCurrent() => buildVersion = CurrentVersion;
    }
}
