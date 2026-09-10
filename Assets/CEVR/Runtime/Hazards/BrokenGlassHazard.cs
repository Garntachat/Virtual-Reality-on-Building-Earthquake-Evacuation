using System.Collections.Generic;
using UnityEngine;

namespace ChulaEarthquakeVR
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public sealed class BrokenGlassHazard : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float barefootDamage = 10f;
        [SerializeField, Min(0f)] private float footwearDamage = 1f;
        [SerializeField, Min(0.1f)] private float damageIntervalSeconds = 0.8f;
        private readonly Dictionary<PlayerHealth, float> nextDamageTimes = new Dictionary<PlayerHealth, float>();

        private void Reset() => GetComponent<Collider>().isTrigger = true;

        private void OnTriggerStay(Collider other)
        {
            PlayerHealth health = other.GetComponentInParent<PlayerHealth>();
            if (health == null || health.IsDead) return;
            if (nextDamageTimes.TryGetValue(health, out float nextTime) && Time.unscaledTime < nextTime) return;
            nextDamageTimes[health] = Time.unscaledTime + damageIntervalSeconds;
            float damage = health.HasProtectiveFootwear ? footwearDamage : barefootDamage;
            health.ApplyDamage(damage, health.HasProtectiveFootwear ? "glass-with-footwear" : "glass-barefoot");
        }

        private void OnTriggerExit(Collider other)
        {
            PlayerHealth health = other.GetComponentInParent<PlayerHealth>();
            if (health != null) nextDamageTimes.Remove(health);
        }

        private void OnDisable() => nextDamageTimes.Clear();
    }
}
