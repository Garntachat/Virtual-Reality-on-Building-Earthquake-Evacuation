using System;
using System.Collections.Generic;
using UnityEngine;

namespace ChulaEarthquakeVR
{
    public sealed class PlayerHealth : MonoBehaviour
    {
        [SerializeField, Min(1f)] private float maximumHealth = 100f;
        [SerializeField, Range(0f, 1f)] private float protectedDamageMultiplier = 0.20f;
        [SerializeField, Min(0f)] private float hitInvulnerabilitySeconds = 0.35f;

        private float lastHitTime = float.NegativeInfinity;
        private readonly HashSet<string> protectionSources = new HashSet<string>();
        public float CurrentHealth { get; private set; }
        public float MaximumHealth => maximumHealth;
        public bool IsProtected => protectionSources.Count > 0;
        public bool HasProtectiveFootwear { get; private set; }
        public bool IsDead => CurrentHealth <= 0f;

        public event Action<float, float, string> Damaged;
        public event Action Died;

        private void Awake() => ResetHealth();

        public void Configure(float maxHealth)
        {
            maximumHealth = Mathf.Max(1f, maxHealth);
            ResetHealth();
        }

        public void ResetHealth()
        {
            CurrentHealth = maximumHealth;
            protectionSources.Clear();
            HasProtectiveFootwear = false;
            lastHitTime = float.NegativeInfinity;
        }

        public void EquipProtectiveFootwear() => HasProtectiveFootwear = true;

        public void SetProtected(bool value) => SetProtection("legacy-cover", value);

        public void SetProtection(string sourceId, bool value)
        {
            string key = string.IsNullOrWhiteSpace(sourceId) ? "unknown-protection" : sourceId;
            if (value) protectionSources.Add(key);
            else protectionSources.Remove(key);
        }

        public bool ApplyDamage(float rawDamage, string sourceId)
        {
            if (IsDead || rawDamage <= 0f || Time.unscaledTime - lastHitTime < hitInvulnerabilitySeconds)
                return false;
            lastHitTime = Time.unscaledTime;
            float applied = rawDamage * (IsProtected ? protectedDamageMultiplier : 1f);
            CurrentHealth = Mathf.Max(0f, CurrentHealth - applied);
            GameplayAudioDirector.PlayCue(GameplayAudioCue.PlayerHit, 0.32f);
            Damaged?.Invoke(applied, CurrentHealth, sourceId ?? "unknown");
            if (IsDead) Died?.Invoke();
            return true;
        }
    }
}
