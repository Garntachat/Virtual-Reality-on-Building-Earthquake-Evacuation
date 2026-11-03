using NUnit.Framework;
using UnityEngine;

namespace ChulaEarthquakeVR.Tests
{
    public sealed class SimpleEarthquakeSettingsTests
    {
        [Test]
        public void PhysicsItem_PreservesManuallyEditedShakeStrength()
        {
            GameObject objectRoot = new GameObject("TestEditableChair");
            GameObject motionRoot = new GameObject("TestMotion");
            try
            {
                objectRoot.AddComponent<Rigidbody>();
                InertialRigidbody item = objectRoot.AddComponent<InertialRigidbody>();
                GroundMotionPlayer motion = motionRoot.AddComponent<GroundMotionPlayer>();
                item.Configure(null, 2.25f, false);
                item.ConnectMotionIfMissing(motion);

                Assert.That(item.ShakeStrength, Is.EqualTo(2.25f).Within(0.001f));
                Assert.That(objectRoot.GetComponent<Rigidbody>().mass, Is.GreaterThan(0f));
            }
            finally
            {
                Object.DestroyImmediate(objectRoot);
                Object.DestroyImmediate(motionRoot);
            }
        }

        [Test]
        public void DecorativeItem_PreservesManuallyEditedShakeStrength()
        {
            GameObject objectRoot = new GameObject("TestEditableSofa");
            GameObject motionRoot = new GameObject("TestMotion");
            try
            {
                DecorativeQuakeResponse item = objectRoot.AddComponent<DecorativeQuakeResponse>();
                GroundMotionPlayer motion = motionRoot.AddComponent<GroundMotionPlayer>();
                item.Configure(null, DecorativeQuakeMode.Heavy, 0f);
                item.ConnectMotionIfMissing(motion);
                Assert.That(item.ShakeStrength, Is.EqualTo(0f).Within(0.001f));
            }
            finally
            {
                Object.DestroyImmediate(objectRoot);
                Object.DestroyImmediate(motionRoot);
            }
        }
    }
}
