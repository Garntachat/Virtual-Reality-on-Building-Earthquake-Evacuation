using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace ChulaEarthquakeVR.Tests
{
    public sealed class HouseProxyEarthquakeSettingsTests
    {
        [Test]
        public void SavedFridgeSettings_AppliedToOneExistingPhysicsBody()
        {
            var visual = new GameObject("HouseLayout_Fridge");
            var proxy = new GameObject("HouseTallCabinet_Left");
            try
            {
                HouseProxyEarthquakeSettings settings =
                    visual.AddComponent<HouseProxyEarthquakeSettings>();
                settings.SetDefaultsForNewComponent(false);
                Set(settings, "mass", 65f);
                Set(settings, "linearDamping", 3.5f);
                Set(settings, "angularDamping", 7f);
                Set(settings, "forceNewtons", 24f);
                Set(settings, "torqueNewtonMeters", 8f);
                Set(settings, "shakeStrength", 0.45f);

                Rigidbody body = proxy.AddComponent<Rigidbody>();
                var inertial = proxy.AddComponent<InertialRigidbody>();
                settings.ApplyToExistingProxy(proxy, null);
                Assert.That(body.mass, Is.EqualTo(65f));
                Assert.That(body.linearDamping, Is.EqualTo(3.5f));
                Assert.That(body.angularDamping, Is.EqualTo(7f));
                Assert.That(inertial.ForceNewtons, Is.EqualTo(24f));
                Assert.That(inertial.TorqueNewtonMeters, Is.EqualTo(8f));
                Assert.That(inertial.ShakeStrength, Is.EqualTo(0.45f));
                Assert.AreEqual(1, proxy.GetComponents<InertialRigidbody>().Length);

                // Rebinding for another training run must not add a second shaker.
                settings.ApplyToExistingProxy(proxy, null);
                Assert.AreEqual(1, proxy.GetComponents<InertialRigidbody>().Length);
                Assert.That(body.mass, Is.EqualTo(65f));
            }
            finally
            {
                Object.DestroyImmediate(visual);
                Object.DestroyImmediate(proxy);
            }
        }

        [Test]
        public void FallingObjects_HaveSeparateDefaultsFromHeavyCabinet()
        {
            var go = new GameObject("HouseLayout_HazardBooks_1");
            try
            {
                var settings = go.AddComponent<HouseProxyEarthquakeSettings>();
                settings.SetDefaultsForNewComponent(true);
                Assert.That(settings.Mass, Is.EqualTo(2.2f));
                Assert.That(settings.ShakeStrength, Is.EqualTo(1f));
                Assert.That(settings.ForceNewtons, Is.EqualTo(15f));
            }
            finally { Object.DestroyImmediate(go); }
        }

        private static void Set(HouseProxyEarthquakeSettings settings, string name, float value)
        {
            FieldInfo field = typeof(HouseProxyEarthquakeSettings).GetField(
                name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, "Expected editable serialized field: " + name);
            field.SetValue(settings, value);
        }
    }
}
