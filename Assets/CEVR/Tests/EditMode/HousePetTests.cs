using NUnit.Framework;
using UnityEngine;

namespace ChulaEarthquakeVR.Tests
{
    public sealed class HousePetTests
    {
        private GameObject pet;
        private GameObject floor;
        private GameObject wall;
        private HousePetController controller;
        private Rigidbody body;

        [SetUp]
        public void SetUp()
        {
            floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.transform.position = new Vector3(0, 999.5f, 0);
            floor.transform.localScale = new Vector3(10, 1, 10);
            pet = new GameObject("TestPet");
            pet.transform.position = new Vector3(0, 1000.02f, 0);
            body = pet.AddComponent<Rigidbody>();
            body.useGravity = false;
            controller = pet.AddComponent<HousePetController>();
            controller.Configure(null, null, null, null);
            Physics.SyncTransforms();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(pet);
            Object.DestroyImmediate(floor);
            if (wall != null) Object.DestroyImmediate(wall);
        }

        [Test]
        public void DesktopOwnershipCannotBeClaimedTwice()
        {
            Assert.That(controller.TryHold(), Is.True);
            Assert.That(controller.TryHold(), Is.False);
            controller.Release();
            Assert.That(controller.TryHold(), Is.True);
        }

        [Test]
        public void HeldPetDoesNotFightInteractorVelocity()
        {
            controller.TryHold();
            body.linearVelocity = new Vector3(1, 2, 3);
            controller.SendMessage("FixedUpdate");
            Assert.That(body.linearVelocity, Is.EqualTo(new Vector3(1, 2, 3)));
        }

        [Test]
        public void AirbornePetIsNotSteeredBackToHome()
        {
            pet.transform.position += Vector3.up * 2;
            Physics.SyncTransforms();
            body.linearVelocity = new Vector3(1, -2, 3);
            controller.SendMessage("FixedUpdate");
            Assert.That(body.linearVelocity, Is.EqualTo(new Vector3(1, -2, 3)));
        }

        [Test]
        public void WallBlocksForwardWalking()
        {
            wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.transform.position = new Vector3(0, 1000.4f, .45f);
            wall.transform.localScale = new Vector3(1, 1, .1f);
            Physics.SyncTransforms();
            controller.SendMessage("FixedUpdate");
            Assert.That(body.linearVelocity.z, Is.EqualTo(0f));
        }

        [Test]
        public void ClearFloorAllowsForwardWalking()
        {
            controller.SendMessage("FixedUpdate");
            Assert.That(body.linearVelocity.z, Is.GreaterThan(0f));
        }
    }
}
