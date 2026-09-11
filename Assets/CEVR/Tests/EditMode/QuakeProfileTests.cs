using NUnit.Framework;
using UnityEngine;

namespace ChulaEarthquakeVR.Tests
{
    public sealed class QuakeProfileTests
    {
        [Test]
        public void PreviewEnvelope_BuildsMonotonicallyThenHoldsAndReleases()
        {
            float previous = 0f;
            for (int i = 0; i <= 1400; i++)
            {
                float value = GroundMotionPlayer.PreviewEnvelope(i * 0.01f, 20f);
                Assert.GreaterOrEqual(value, previous);
                Assert.That(value, Is.InRange(0f, 1f));
                previous = value;
            }
            Assert.AreEqual(0f, GroundMotionPlayer.PreviewEnvelope(0f, 20f));
            Assert.AreEqual(1f, GroundMotionPlayer.PreviewEnvelope(14f, 20f), 0.00001f);
            Assert.AreEqual(1f, GroundMotionPlayer.PreviewEnvelope(18f, 20f), 0.00001f);
            Assert.AreEqual(0f, GroundMotionPlayer.PreviewEnvelope(20f, 20f), 0.00001f);
            Assert.Less(GroundMotionPlayer.PreviewEnvelope(19f, 20f), 1f);
        }

        private QuakeProfile profile;

        [SetUp]
        public void SetUp()
        {
            profile = ScriptableObject.CreateInstance<QuakeProfile>();
            profile.Initialize("test", 2f, new[]
            {
                Vector3.zero,
                new Vector3(2f, 0f, 0f),
                new Vector3(4f, 0f, 0f)
            });
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(profile);

        [Test]
        public void Evaluate_InterpolatesAndClamps()
        {
            Assert.That(profile.Evaluate(0.25f).x, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(profile.Evaluate(999f).x, Is.EqualTo(4f).Within(0.0001f));
        }

        [Test]
        public void Profile_ReportsExpectedDurationAndValidity()
        {
            Assert.That(profile.DurationSeconds, Is.EqualTo(1f).Within(0.0001f));
            Assert.IsTrue(profile.IsValid(out string error), error);
        }

        [Test]
        public void Initialize_RejectsNonFiniteSamples()
        {
            Assert.Throws<System.ArgumentException>(() => profile.Initialize(
                "bad", 100f, new[] { Vector3.zero, new Vector3(float.NaN, 0f, 0f) }));
        }
    }
}
