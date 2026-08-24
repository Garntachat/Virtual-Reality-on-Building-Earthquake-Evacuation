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
        private PlayerHealth occupant;
        public bool IsOccupied => participantColliders.Count > 0;
        public string ZoneId => zoneId;
        public event Action<bool, string> OccupancyChanged;

        private void Reset() => GetComponent<Collider>().isTrigger = true;

        private void OnTriggerEnter(Collider other)
        {
            PlayerHealth health = other.GetComponentInParent<PlayerHealth>();
            if (health == null || !participantColliders.Add(other)) return;
            occupant = health;
            if (participantColliders.Count == 1)
            {
                occupant.SetProtected(true);
                OccupancyChanged?.Invoke(true, zoneId);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (!participantColliders.Remove(other)) return;
            if (participantColliders.Count == 0)
            {
                occupant?.SetProtected(false);
                occupant = null;
                OccupancyChanged?.Invoke(false, zoneId);
            }
        }

        private void OnDisable()
        {
            occupant?.SetProtected(false);
            occupant = null;
            participantColliders.Clear();
        }

        public void Configure(string id)
        {
            zoneId = string.IsNullOrWhiteSpace(id) ? name : id;
            GetComponent<Collider>().isTrigger = true;
        }
    }
}
