using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace ChulaEarthquakeVR.Tests
{
    public sealed class GrabOcclusionTests
    {
        private GameObject player, wall, target;

        [TearDown]
        public void Cleanup()
        {
            if (target != null) Object.DestroyImmediate(target);
            if (wall != null) Object.DestroyImmediate(wall);
            if (player != null) Object.DestroyImmediate(player);
        }

        [Test]
        public void OutOfReachWallBlocksThirdPersonRay()
        {
            Setup();
            wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.transform.position = new Vector3(1000f, 1f, -6f);
            Physics.SyncTransforms();
            Assert.IsFalse(Cast(), "The wall is nearer the camera and must block the reachable target.");
        }

        [Test]
        public void ReachableTargetWithoutWallIsDetected()
        {
            Setup();
            Physics.SyncTransforms();
            Assert.IsTrue(Cast(), "An unobstructed target within reach should remain selectable.");
        }

        private void Setup()
        {
            player = new GameObject("GrabTestPlayer");
            player.transform.position = new Vector3(1000f, 0f, 0f);
            player.AddComponent<DesktopGrabInteractor>();
            target = GameObject.CreatePrimitive(PrimitiveType.Cube);
            target.transform.position = new Vector3(1000f, 1f, 2f);
        }

        private bool Cast()
        {
            var method = typeof(DesktopGrabInteractor).GetMethod("RaycastTarget",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(method);
            var ray = new Ray(new Vector3(1000f, 1f, -8f), Vector3.forward);
            object[] args = { ray, default(RaycastHit) };
            return (bool)method.Invoke(player.GetComponent<DesktopGrabInteractor>(), args);
        }
    }
}
