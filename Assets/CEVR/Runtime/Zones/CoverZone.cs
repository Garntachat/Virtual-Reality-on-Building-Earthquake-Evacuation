using System;
using System.Collections.Generic;
using UnityEngine;

namespace ChulaEarthquakeVR
{
    [RequireComponent(typeof(Collider))]
    public sealed class CoverZone : MonoBehaviour
    {
        [SerializeField] private string zoneId = "cover-01";
        private readonly HashSet<Collider> participantColliders = new HashSet<Collider>();
        private readonly Dictionary<PlayerHealth, int> occupantColliderCounts = new Dictionary<PlayerHealth, int>();
        public bool IsOccupied => participantColliders.Count > 0;
        public string ZoneId => zoneId;
        public event Action<bool, string> OccupancyChanged;

        private void Reset() => GetComponent<Collider>().isTrigger = true;

        private void OnTriggerEnter(Collider other)
        {
            PlayerHealth health = other.GetComponentInParent<PlayerHealth>();
            bool wasOccupied = IsOccupied;
            if (health == null || !participantColliders.Add(other)) return;
            occupantColliderCounts.TryGetValue(health, out int count);
            occupantColliderCounts[health] = count + 1;
            if (count == 0)
            {
                health.SetProtection("cover:" + zoneId, true);
            }
            if (!wasOccupied) OccupancyChanged?.Invoke(true, zoneId);
        }

        private void OnTriggerExit(Collider other)
        {
            if (!participantColliders.Remove(other)) return;
            PlayerHealth health = other.GetComponentInParent<PlayerHealth>();
            if (health == null || !occupantColliderCounts.TryGetValue(health, out int count)) return;
            if (count <= 1)
            {
                occupantColliderCounts.Remove(health);
                health.SetProtection("cover:" + zoneId, false);
            }
            else occupantColliderCounts[health] = count - 1;
            if (!IsOccupied) OccupancyChanged?.Invoke(false, zoneId);
        }

        private void OnDisable()
        {
            foreach (PlayerHealth health in occupantColliderCounts.Keys)
                if (health != null) health.SetProtection("cover:" + zoneId, false);
            occupantColliderCounts.Clear();
            participantColliders.Clear();
        }

        public void Configure(string id)
        {
            zoneId = string.IsNullOrWhiteSpace(id) ? name : id;
            GetComponent<Collider>().isTrigger = true;
        }
    }
}
