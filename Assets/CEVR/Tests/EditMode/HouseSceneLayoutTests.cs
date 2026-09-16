using NUnit.Framework;
using UnityEngine;

namespace ChulaEarthquakeVR.Tests
{
    public sealed class HouseSceneLayoutTests
    {
        [Test]
        public void FurnitureAnchorsStayOnMeasuredGroundFloor()
        {
            Vector3[] anchors =
            {
                HouseSceneLayout.DiningTable,
                HouseSceneLayout.CoverObstacleChair,
                HouseSceneLayout.DiningLeftChair,
                HouseSceneLayout.DiningRightChair,
                HouseSceneLayout.SpareChair,
                HouseSceneLayout.Sofa,
                HouseSceneLayout.CoffeeTable,
                HouseSceneLayout.Television,
                HouseSceneLayout.Plant,
                HouseSceneLayout.Bed,
                HouseSceneLayout.WardrobeBottom,
                HouseSceneLayout.KitchenCabinet,
                HouseSceneLayout.KitchenSink,
                HouseSceneLayout.KitchenStove,
                HouseSceneLayout.FridgeBottom
            };

            foreach (Vector3 anchor in anchors)
            {
                Assert.That(anchor.y, Is.EqualTo(HouseSceneLayout.FloorY).Within(0.001f), anchor.ToString());
                Assert.That(HouseSceneLayout.IsInsideGroundFloor(anchor), Is.True, anchor.ToString());
            }
        }

        [Test]
        public void WindowMatchesMeasuredSouthWallOpening()
        {
            Assert.That(HouseSceneLayout.WindowCenter.x, Is.EqualTo(-2.0f).Within(0.01f));
            Assert.That(HouseSceneLayout.WindowCenter.z, Is.EqualTo(-7.43f).Within(0.02f));
            Assert.That(HouseSceneLayout.WindowCenter.y - HouseSceneLayout.WindowSize.y * 0.5f,
                Is.GreaterThan(HouseSceneLayout.FloorY + 0.50f));
            Assert.That(HouseSceneLayout.WindowSize.x, Is.LessThanOrEqualTo(1.05f));
        }

        [Test]
        public void CoverChairIntentionallyBlocksDirectTableApproachWithoutBlockingSpawn()
        {
            Assert.That(HouseSceneLayout.CoverObstacleChair.x,
                Is.EqualTo(HouseSceneLayout.DiningTable.x).Within(0.01f));
            Assert.That(HouseSceneLayout.CoverObstacleChair.z,
                Is.LessThan(HouseSceneLayout.DiningTable.z));
            Assert.That(HouseSceneLayout.PlayerSpawn.z,
                Is.LessThan(HouseSceneLayout.CoverObstacleChair.z - 0.75f));
        }

        [Test]
        public void KitchenAndBedroomRemainSeparatedFromCentralStairZone()
        {
            Assert.That(HouseSceneLayout.Bed.x, Is.LessThan(0f));
            Assert.That(HouseSceneLayout.KitchenCabinet.x, Is.GreaterThan(2f));
            Assert.That(HouseSceneLayout.KitchenCabinet.z, Is.GreaterThan(5.5f));
            Assert.That(HouseSceneLayout.FridgeBottom.z, Is.GreaterThan(5.5f));
        }
    }
}
