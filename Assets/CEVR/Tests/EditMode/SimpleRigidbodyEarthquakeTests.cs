using NUnit.Framework;
using UnityEngine;

namespace ChulaEarthquakeVR.Tests
{
    public sealed class SimpleRigidbodyEarthquakeTests
    {
        [Test]
        public void PerItemForce_NeverOverridesUnityMassOrDamping()
        {
            var item = new GameObject("Editable House Chair");
            try
            {
                Rigidbody body = item.AddComponent<Rigidbody>();
                body.mass = 5f;
                body.linearDamping = 1.5f;
                body.angularDamping = 0.75f;
                body.interpolation = RigidbodyInterpolation.None;
                body.collisionDetectionMode = CollisionDetectionMode.Discrete;
                body.constraints = RigidbodyConstraints.FreezePositionY;

                InertialRigidbody shaker = item.AddComponent<InertialRigidbody>();
                shaker.Configure(null, 1.5f, false);
                shaker.ApplySimpleWave(new Vector3(0.5f, 0.1f, -0.3f));

                Assert.That(body.mass, Is.EqualTo(5f));
                Assert.That(body.linearDamping, Is.EqualTo(1.5f));
                Assert.That(body.angularDamping, Is.EqualTo(0.75f));
                Assert.That(body.interpolation, Is.EqualTo(RigidbodyInterpolation.None));
                Assert.That(body.collisionDetectionMode, Is.EqualTo(CollisionDetectionMode.Discrete));
                Assert.That(body.constraints, Is.EqualTo(RigidbodyConstraints.FreezePositionY));
                Assert.That(shaker.ForceNewtons, Is.EqualTo(15f));
                Assert.That(shaker.TorqueNewtonMeters, Is.EqualTo(5f));
                Assert.That(shaker.ShakeStrength, Is.EqualTo(1.5f));
            }
            finally { Object.DestroyImmediate(item); }
        }

        [Test]
        public void ToppleableSettings_UseOneForceComponent()
        {
            var item = new GameObject("Editable Tall Cabinet");
            try
            {
                item.AddComponent<Rigidbody>();
                ToppleableFurniture topple = item.AddComponent<ToppleableFurniture>();
                topple.Configure(null, 1.25f, 1.2f);
                InertialRigidbody shaker = item.AddComponent<InertialRigidbody>();
                shaker.Configure(null, 0.8f, false);
                Assert.That(topple.UpperForceScale, Is.EqualTo(1.25f));
                Assert.That(topple.ForceHeight, Is.EqualTo(1.2f));
                Assert.IsFalse(topple.AnchoredToStructure);
                Assert.DoesNotThrow(() => shaker.ApplySimpleWave(Vector3.right));
            }
            finally { Object.DestroyImmediate(item); }
        }

        [Test]
        public void DisabledShaking_DoesNotModifyRigidbodySettings()
        {
            var item = new GameObject("Disabled Furniture");
            try
            {
                Rigidbody body = item.AddComponent<Rigidbody>();
                body.mass = 12f;
                body.linearDamping = 4f;
                InertialRigidbody shaker = item.AddComponent<InertialRigidbody>();
                shaker.Configure(null, 0f, false);
                shaker.ApplySimpleWave(Vector3.one);
                Assert.That(shaker.ShakeStrength, Is.EqualTo(0f));
                Assert.That(body.mass, Is.EqualTo(12f));
                Assert.That(body.linearDamping, Is.EqualTo(4f));
            }
            finally { Object.DestroyImmediate(item); }
        }
    }
}
