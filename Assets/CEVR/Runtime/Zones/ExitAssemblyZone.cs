using System;
using System.Collections.Generic;
using UnityEngine;

namespace ChulaEarthquakeVR
{
    [RequireComponent(typeof(Collider))]
    public sealed class ExitAssemblyZone : MonoBehaviour
    {
        [SerializeField] private string zoneId = "assembly-point";
        private readonly HashSet<Collider> participantColliders = new HashSet<Collider>();
        public bool SuccessEnabled { get; private set; }
        public bool IsOccupied => participantColliders.Count > 0;
        public event Action<bool, string> Entered;

        private void Reset() => GetComponent<Collider>().isTrigger = true;

        private void OnTriggerEnter(Collider other)
        {
            if (other.GetComponentInParent<PlayerHealth>() == null || !participantColliders.Add(other)) return;
            if (participantColliders.Count == 1) Entered?.Invoke(SuccessEnabled, zoneId);
        }

        private void OnTriggerExit(Collider other) => participantColliders.Remove(other);

        public void SetSuccessEnabled(bool enabled) => SuccessEnabled = enabled;

        public void Configure(string id)
        {
            zoneId = string.IsNullOrWhiteSpace(id) ? name : id;
            GetComponent<Collider>().isTrigger = true;
        }
    }
}
