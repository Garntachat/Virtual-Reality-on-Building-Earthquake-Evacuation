using NUnit.Framework;
using UnityEngine;

namespace ChulaEarthquakeVR.Tests
{
    public sealed class PreQuakeChairInteractionTests
    {
        [Test]
        public void ParkedChair_CanBeIntentionallyMoved_ThenParksAgainWithoutChangingSettings()
        {
            var chair = new GameObject("HouseTableChair");
            try
            {
                Rigidbody body = chair.AddComponent<Rigidbody>();
                body.mass = 6.5f;
                body.linearDamping = 1.2f;
                body.angularDamping = 1.8f;
                body.useGravity = false;
                body.isKinematic = true;

                chair.AddComponent<MovableFurniture>();
                InertialRigidbody inertia = chair.AddComponent<InertialRigidbody>();
                inertia.Configure(null, 0.65f, false);
                inertia.ConfigureForces(22f, 6f);
                inertia.ConfigureQuakeActivation(true);

                Assert.IsTrue(inertia.BeginManualInteraction());
                Assert.IsFalse(body.isKinematic, "The player must still be able to slide a chair.");
                inertia.EndManualInteraction();
                Assert.IsTrue(body.isKinematic, "The unattended pre-quake chair must stay parked.");
                Assert.That(body.mass, Is.EqualTo(6.5f));
                Assert.That(body.linearDamping, Is.EqualTo(1.2f));
                Assert.That(body.angularDamping, Is.EqualTo(1.8f));
                Assert.That(inertia.ShakeStrength, Is.EqualTo(0.65f));
                Assert.That(inertia.ForceNewtons, Is.EqualTo(22f));
                Assert.That(inertia.TorqueNewtonMeters, Is.EqualTo(6f));
            }
            finally { Object.DestroyImmediate(chair); }
        }

        [Test]
        public void ParkedNonInteractiveFurniture_CannotBeReleasedByChairGrabPath()
        {
            var table = new GameObject("HeavyHouseTable");
            try
            {
                Rigidbody body = table.AddComponent<Rigidbody>();
                body.isKinematic = true;
                InertialRigidbody inertia = table.AddComponent<InertialRigidbody>();
                inertia.ConfigureQuakeActivation(true);
                Assert.IsFalse(inertia.BeginManualInteraction());
                Assert.IsTrue(body.isKinematic);
            }
            finally { Object.DestroyImmediate(table); }
        }
    }
}
