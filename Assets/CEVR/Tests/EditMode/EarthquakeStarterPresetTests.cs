using ChulaEarthquakeVR.Editor;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ChulaEarthquakeVR.Tests
{
    public sealed class EarthquakeStarterPresetTests
    {
        [Test]
        public void ObjectTypes_GetDifferentReasonableStarterMasses()
        {
            Assert.That(EarthquakeStarterPresets.Suggest("Condo Chair - Left").Mass,
                Is.EqualTo(6.5f));
            Assert.That(EarthquakeStarterPresets.Suggest("HouseLayout_Fridge").Mass,
                Is.EqualTo(65f));
            Assert.That(EarthquakeStarterPresets.Suggest("HouseLayout_Wandrobe").Mass,
                Is.EqualTo(42f));
            Assert.That(EarthquakeStarterPresets.Suggest("StagedFallingHazard_1").Mass,
                Is.EqualTo(2.2f));
            Assert.That(EarthquakeStarterPresets.Suggest("HangingLamp_1").Mass,
                Is.EqualTo(3f));
        }

        [Test]
        public void GeneratedChair_GetsPresetExactlyOnce_ThenUserChangesSurvive()
        {
            Scene scene = EditorSceneManager.NewPreviewScene();
            try
            {
                GameObject chair = new GameObject("Condo Chair - Left");
                SceneManager.MoveGameObjectToScene(chair, scene);
                Rigidbody body = chair.AddComponent<Rigidbody>();
                body.mass = 7.5f;
                body.linearDamping = 2.5f;
                body.angularDamping = 4f;
                var shaker = chair.AddComponent<InertialRigidbody>();
                shaker.Configure(null, 0.65f, false);

                Assert.That(EarthquakeStarterPresets.ApplyOnce(scene), Is.EqualTo(1));
                Assert.That(body.mass, Is.EqualTo(6.5f));
                Assert.That(body.linearDamping, Is.EqualTo(1.2f));
                Assert.That(body.angularDamping, Is.EqualTo(1.8f));
                Assert.That(shaker.ForceNewtons, Is.EqualTo(22f));
                Assert.That(shaker.TorqueNewtonMeters, Is.EqualTo(6f));
                Assert.IsTrue(shaker.StarterPresetProcessed);

                // Editing in Inspector outside Play must become authoritative.
                body.mass = 12f;
                body.linearDamping = 3.8f;
                body.angularDamping = 4.1f;
                shaker.ConfigureForces(37f, 9f);

                Assert.IsFalse(EarthquakeStarterPresets.HasPending(scene));
                Assert.That(EarthquakeStarterPresets.ApplyOnce(scene), Is.EqualTo(0));
                Assert.That(body.mass, Is.EqualTo(12f));
                Assert.That(body.linearDamping, Is.EqualTo(3.8f));
                Assert.That(body.angularDamping, Is.EqualTo(4.1f));
                Assert.That(shaker.ForceNewtons, Is.EqualTo(37f));
                Assert.That(shaker.TorqueNewtonMeters, Is.EqualTo(9f));
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }

        [Test]
        public void ExistingCustomizedTutorialItem_IsNeverOverwritten()
        {
            Scene scene = EditorSceneManager.NewPreviewScene();
            try
            {
                GameObject lamp = new GameObject("HangingLamp_1");
                SceneManager.MoveGameObjectToScene(lamp, scene);
                Rigidbody body = lamp.AddComponent<Rigidbody>();
                body.mass = 9.1f; // Artist changed this from the generator's 3 kg.
                body.linearDamping = 2.25f;
                body.angularDamping = 1.5f;
                var shaker = lamp.AddComponent<InertialRigidbody>();
                shaker.Configure(null, 0.74f, true);
                shaker.ConfigureForces(35f, 12f);

                Assert.AreEqual(0, EarthquakeStarterPresets.ApplyOnce(scene));
                Assert.IsTrue(shaker.StarterPresetProcessed);
                Assert.That(body.mass, Is.EqualTo(9.1f));
                Assert.That(body.linearDamping, Is.EqualTo(2.25f));
                Assert.That(body.angularDamping, Is.EqualTo(1.5f));
                Assert.That(shaker.ForceNewtons, Is.EqualTo(35f));
                Assert.That(shaker.TorqueNewtonMeters, Is.EqualTo(12f));
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }

        [Test]
        public void HousePhysicsProxy_UsesSavedStarterPreset_AndNeverAddsSecondShaker()
        {
            Scene scene = EditorSceneManager.NewPreviewScene();
            try
            {
                var fridge = new GameObject("HouseLayout_Fridge");
                SceneManager.MoveGameObjectToScene(fridge, scene);
                var settings = fridge.AddComponent<HouseProxyEarthquakeSettings>();
                settings.SetDefaultsForNewComponent(false);
                Assert.AreEqual(1, EarthquakeStarterPresets.ApplyOnce(scene));
                Assert.That(settings.Mass, Is.EqualTo(65f));
                Assert.That(settings.LinearDamping, Is.EqualTo(0.8f));
                Assert.IsTrue(settings.StarterPresetProcessed);

                var proxy = new GameObject("HouseTallCabinet_Left");
                SceneManager.MoveGameObjectToScene(proxy, scene);
                Rigidbody physicsBody = proxy.AddComponent<Rigidbody>();
                var shaker = proxy.AddComponent<InertialRigidbody>();
                settings.ApplyToExistingProxy(proxy, null);
                Assert.That(physicsBody.mass, Is.EqualTo(65f));
                Assert.That(physicsBody.linearDamping, Is.EqualTo(0.8f));
                Assert.That(shaker.ForceNewtons, Is.EqualTo(100f));
                Assert.AreEqual(1, proxy.GetComponents<InertialRigidbody>().Length);
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }

        [Test]
        public void TutorialFallingHazard_PresetDoesNotReleaseItsKinematicBody()
        {
            Scene scene = EditorSceneManager.NewPreviewScene();
            try
            {
                GameObject books = new GameObject("StagedFallingHazard_1");
                SceneManager.MoveGameObjectToScene(books, scene);
                Rigidbody body = books.AddComponent<Rigidbody>();
                body.mass = 2.5f;
                body.isKinematic = true;
                body.useGravity = false;
                var shaker = books.AddComponent<InertialRigidbody>();
                shaker.Configure(null, 1f, true);
                Assert.AreEqual(1, EarthquakeStarterPresets.ApplyOnce(scene));
                Assert.That(body.mass, Is.EqualTo(2.2f));
                Assert.That(body.linearDamping, Is.EqualTo(0.3f));
                Assert.That(shaker.ForceNewtons, Is.EqualTo(7f));
                Assert.IsTrue(body.isKinematic);
                Assert.IsFalse(body.useGravity);
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }

        [Test]
        public void CondoCabinet_GetsDifferentPresetAndPreservesItsHazardState()
        {
            Scene scene = EditorSceneManager.NewPreviewScene();
            try
            {
                GameObject cabinet = new GameObject("Condo Unsecured Cabinet");
                SceneManager.MoveGameObjectToScene(cabinet, scene);
                Rigidbody body = cabinet.AddComponent<Rigidbody>();
                body.mass = 22f;
                body.isKinematic = true;
                body.useGravity = false;
                var shaker = cabinet.AddComponent<InertialRigidbody>();
                shaker.Configure(null, 1f, false);
                Assert.AreEqual(1, EarthquakeStarterPresets.ApplyOnce(scene));
                Assert.That(body.mass, Is.EqualTo(38f));
                Assert.That(body.linearDamping, Is.EqualTo(0.8f));
                Assert.That(body.angularDamping, Is.EqualTo(1.1f));
                Assert.That(shaker.ForceNewtons, Is.EqualTo(95f));
                Assert.That(shaker.TorqueNewtonMeters, Is.EqualTo(40f));
                Assert.IsTrue(body.isKinematic);
                Assert.IsFalse(body.useGravity);
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }

        [Test]
        public void HouseCustomizedProxy_LeavesAllSavedMassAndDampingUntouched()
        {
            Scene scene = EditorSceneManager.NewPreviewScene();
            try
            {
                GameObject fridge = new GameObject("HouseLayout_Fridge");
                SceneManager.MoveGameObjectToScene(fridge, scene);
                HouseProxyEarthquakeSettings settings =
                    fridge.AddComponent<HouseProxyEarthquakeSettings>();
                settings.SetDefaultsForNewComponent(false);
                var fields = new UnityEditor.SerializedObject(settings);
                fields.FindProperty("mass").floatValue = 88f;
                fields.FindProperty("linearDamping").floatValue = 2f;
                fields.ApplyModifiedProperties();
                Assert.AreEqual(0, EarthquakeStarterPresets.ApplyOnce(scene));
                Assert.IsTrue(settings.StarterPresetProcessed);
                Assert.That(settings.Mass, Is.EqualTo(88f));
                Assert.That(settings.LinearDamping, Is.EqualTo(2f));
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }

        [Test]
        public void DecorativeFurniture_HasEditableVisualDamping_NotFakeMass()
        {
            Scene scene = EditorSceneManager.NewPreviewScene();
            try
            {
                GameObject sofa = new GameObject("TeamFurniture_Sofa");
                SceneManager.MoveGameObjectToScene(sofa, scene);
                var visual = sofa.AddComponent<DecorativeQuakeResponse>();
                visual.Configure(null, DecorativeQuakeMode.Heavy, 0f);

                Assert.AreEqual(1, EarthquakeStarterPresets.ApplyOnce(scene));
                Assert.That(visual.VisualDamping, Is.EqualTo(4f));
                Assert.That(visual.ShakeStrength, Is.EqualTo(0f));
                Assert.IsNull(sofa.GetComponent<Rigidbody>());
                Assert.AreEqual(0, EarthquakeStarterPresets.ApplyOnce(scene));
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }
    }
}
