using System.Reflection;
using ChulaEarthquakeVR.Editor;
using NUnit.Framework;
using UnityEngine;

namespace ChulaEarthquakeVR.Tests
{
    public sealed class TutorialCondoFurnitureCoverageTests
    {
        private static bool IsCandidate(Transform root, string sceneName)
        {
            MethodInfo method = typeof(SceneFurnitureEarthquakeCoverage).GetMethod(
                "IsFurniture", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.IsNotNull(method);
            return (bool)method.Invoke(null, new object[] { root, sceneName });
        }

        [Test]
        public void TutorialTeamModel_IsAnIndependentlyEditableFurnitureRoot()
        {
            GameObject group = new GameObject("Tutorial Editable Furniture");
            try
            {
                GameObject sofa = new GameObject("TeamFurniture_Sofa");
                sofa.transform.SetParent(group.transform);
                GameObject mesh = GameObject.CreatePrimitive(PrimitiveType.Cube);
                mesh.name = "Mesh";
                mesh.transform.SetParent(sofa.transform);
                Assert.IsTrue(IsCandidate(sofa.transform, "CEVR_ChulaEngineering_Tutorial"));
                Assert.IsFalse(IsCandidate(group.transform, "CEVR_ChulaEngineering_Tutorial"),
                    "The whole furnished scene must never be one moving object.");
            }
            finally { Object.DestroyImmediate(group); }
        }

        [Test]
        public void TutorialSturdyCoverTable_RecognizedButNeverConvertedToDynamicPhysics()
        {
            GameObject room = new GameObject("TutorialObjects");
            try
            {
                GameObject table = GameObject.CreatePrimitive(PrimitiveType.Cube);
                table.name = "SturdyCoverTableTop";
                table.transform.SetParent(room.transform);
                Assert.IsTrue(IsCandidate(table.transform, "CEVR_ChulaEngineering_Tutorial"));
                Assert.IsNull(table.GetComponent<Rigidbody>());
            }
            finally { Object.DestroyImmediate(room); }
        }

        [Test]
        public void CondoGameplayFurniture_IsRecognizedButImportedBuildingMeshIsExcluded()
        {
            GameObject game = new GameObject("Condo Gameplay");
            GameObject architecture = new GameObject("Condo Architecture");
            try
            {
                GameObject chair = new GameObject("Condo Chair - Left");
                chair.transform.SetParent(game.transform);
                GameObject seat = GameObject.CreatePrimitive(PrimitiveType.Cube);
                seat.name = "Seat";
                seat.transform.SetParent(chair.transform);

                GameObject building = GameObject.CreatePrimitive(PrimitiveType.Cube);
                building.name = "pb_Mesh-32172";
                building.transform.SetParent(architecture.transform);
                Assert.IsTrue(IsCandidate(chair.transform, "Condo"));
                Assert.IsFalse(IsCandidate(seat.transform, "Condo"),
                    "Only the parent chair gets the response, not each mesh part.");
                Assert.IsFalse(IsCandidate(building.transform, "Condo"),
                    "The imported building shell must not receive earthquake forces.");
            }
            finally
            {
                Object.DestroyImmediate(game);
                Object.DestroyImmediate(architecture);
            }
        }

        [Test]
        public void CondoSafetyFloor_IsNeverTreatedAsFurniture()
        {
            GameObject room = new GameObject("Condo Gameplay");
            try
            {
                GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
                floor.name = "Gameplay Safety Floor";
                floor.transform.SetParent(room.transform);
                Assert.IsFalse(IsCandidate(floor.transform, "Condo"));
            }
            finally { Object.DestroyImmediate(room); }
        }
    }
}
