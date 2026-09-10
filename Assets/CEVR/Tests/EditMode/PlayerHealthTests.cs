using NUnit.Framework;
using UnityEngine;

namespace ChulaEarthquakeVR.Tests
{
    public sealed class PlayerHealthTests
    {
        private GameObject root;
        private PlayerHealth health;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("HealthTest");
            health = root.AddComponent<PlayerHealth>();
            health.Configure(100f);
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(root);

        [Test]
        public void HazardReset_KeepsStagedBodyKinematicWithoutWarnings()
        {
            Rigidbody body = root.AddComponent<Rigidbody>();
            body.isKinematic = true;
            HazardDirector director = root.AddComponent<HazardDirector>();
            director.Configure(new[] { body });
            director.ResetHazards();
            Assert.IsTrue(body.isKinematic);
            Assert.IsFalse(body.useGravity);
            UnityEngine.TestTools.LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void UnprotectedHit_AppliesFullDamage()
        {
            Assert.IsTrue(health.ApplyDamage(25f, "test"));
            Assert.That(health.CurrentHealth, Is.EqualTo(75f).Within(0.001f));
        }

        [Test]
        public void Cover_ReducesDamageToConfiguredTwentyPercent()
        {
            health.SetProtected(true);
            Assert.IsTrue(health.ApplyDamage(25f, "test"));
            Assert.That(health.CurrentHealth, Is.EqualTo(95f).Within(0.001f));
        }

        [Test]
        public void PillowProtection_RemainsWhenCoverZoneIsExited()
        {
            health.SetProtection("cover:test", true);
            health.SetProtection("pillow:test", true);
            health.SetProtection("cover:test", false);

            Assert.IsTrue(health.IsProtected);
            Assert.IsTrue(health.ApplyDamage(25f, "test"));
            Assert.That(health.CurrentHealth, Is.EqualTo(95f).Within(0.001f));
        }

        [Test]
        public void EquippingFootwear_SetsPersistentSafetyState()
        {
            health.EquipProtectiveFootwear();

            Assert.IsTrue(health.HasProtectiveFootwear);
        }

        [Test]
        public void Health_NeverDropsBelowZero()
        {
            health.ApplyDamage(200f, "test");
            Assert.AreEqual(0f, health.CurrentHealth);
            Assert.IsTrue(health.IsDead);
        }
    }
}
