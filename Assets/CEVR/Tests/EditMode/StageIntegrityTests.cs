using NUnit.Framework;
using UnityEngine;

namespace ChulaEarthquakeVR.Tests
{
    public sealed class StageIntegrityTests
    {
        [Test]
        public void GeneratedStageInfo_ReportsCurrentBuilderVersion()
        {
            GameObject root = new GameObject("StageInfoTest");
            try
            {
                GeneratedStageInfo info = root.AddComponent<GeneratedStageInfo>();
                info.ConfigureCurrent();
                Assert.IsTrue(info.IsCurrent);
                Assert.AreEqual(GeneratedStageInfo.CurrentVersion, info.BuildVersion);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void HazardReset_RestoresCapturedPoseAndSafePhysicsState()
        {
            GameObject directorRoot = new GameObject("HazardDirectorTest");
            GameObject hazardRoot = new GameObject("HazardTestBody");
            try
            {
                Rigidbody body = hazardRoot.AddComponent<Rigidbody>();
                Vector3 initialPosition = new Vector3(1f, 2f, 3f);
                Quaternion initialRotation = Quaternion.Euler(0f, 25f, 0f);
                body.position = initialPosition;
                body.rotation = initialRotation;

                HazardDirector director = directorRoot.AddComponent<HazardDirector>();
                director.Configure(new[] { body });
                body.position = new Vector3(8f, -2f, 4f);
                body.rotation = Quaternion.Euler(45f, 90f, 10f);
                body.isKinematic = false;
                body.useGravity = true;

                director.ResetHazards();

                Assert.That(Vector3.Distance(body.position, initialPosition), Is.LessThan(0.0001f));
                Assert.That(Quaternion.Angle(body.rotation, initialRotation), Is.LessThan(0.0001f));
                Assert.IsTrue(body.isKinematic);
                Assert.IsFalse(body.useGravity);
            }
            finally
            {
                Object.DestroyImmediate(hazardRoot);
                Object.DestroyImmediate(directorRoot);
            }
        }
    }
}
