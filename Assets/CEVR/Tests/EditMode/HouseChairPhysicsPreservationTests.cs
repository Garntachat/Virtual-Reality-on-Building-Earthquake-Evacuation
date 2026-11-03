using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ChulaEarthquakeVR.Tests
{
    public sealed class HouseChairPhysicsPreservationTests
    {
        [Test]
        public void SavedHouseChair_KeepsMassDampingAndShakeStrength_WhenBoundForPlay()
        {
            Scene preview = EditorSceneManager.NewPreviewScene();
            try
            {
                var furniture = new GameObject("HouseFurniture");
                SceneManager.MoveGameObjectToScene(furniture, preview);
                var gameplay = new GameObject("CEVR_UniversalGameplay");
                SceneManager.MoveGameObjectToScene(gameplay, preview);

                GameObject chair = GameObject.CreatePrimitive(PrimitiveType.Cube);
                SceneManager.MoveGameObjectToScene(chair, preview);
                chair.name = "HouseLayout_DiningChair_Right";
                chair.transform.SetParent(furniture.transform, true);

                BoxCollider collider = chair.GetComponent<BoxCollider>();
                collider.size = new Vector3(0.93f, 1.10f, 0.71f);
                Rigidbody body = chair.AddComponent<Rigidbody>();
                body.mass = 17f;
                body.linearDamping = 3.25f;
                body.angularDamping = 2.25f;
                body.collisionDetectionMode = CollisionDetectionMode.Discrete;
                body.constraints = RigidbodyConstraints.FreezeRotationX;
                var shaker = chair.AddComponent<InertialRigidbody>();
                shaker.Configure(null, 2.2f, false);

                GameObject proxy = new GameObject("HouseChair_DiningRight");
                SceneManager.MoveGameObjectToScene(proxy, preview);
                proxy.transform.SetParent(gameplay.transform, true);
                proxy.AddComponent<BoxCollider>();
                proxy.AddComponent<Rigidbody>();

                bool synced = HouseSavedSceneRuntimeBinder.Bind(
                    preview, furniture.transform, null, null, out string report);
                Assert.IsTrue(synced, report);
                Assert.That(body.mass, Is.EqualTo(17f));
                Assert.That(body.linearDamping, Is.EqualTo(3.25f));
                Assert.That(body.angularDamping, Is.EqualTo(2.25f));
                Assert.That(body.collisionDetectionMode, Is.EqualTo(CollisionDetectionMode.Discrete));
                Assert.That(body.constraints, Is.EqualTo(RigidbodyConstraints.FreezeRotationX));
                Assert.That(shaker.ShakeStrength, Is.EqualTo(2.2f));
                Assert.That(collider.size.x, Is.EqualTo(0.93f));
                Assert.IsTrue(collider.enabled, "Chair must remain grabbable with its saved collider.");
                Assert.AreEqual(1, chair.GetComponents<InertialRigidbody>().Length);
            }
            finally
            {
                if (preview.IsValid()) EditorSceneManager.ClosePreviewScene(preview);
            }
        }
    }
}
