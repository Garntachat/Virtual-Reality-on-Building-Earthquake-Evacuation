using NUnit.Framework;
using UnityEngine;

namespace ChulaEarthquakeVR.Tests
{
    public sealed class CondoGameplayTests
    {
        [Test]
        public void CondoLayout_BuildsOnce_AndPreservesSavedFurniture()
        {
            var root = new GameObject("Condo Gameplay Test");
            try
            {
                CondoGameplayLayout layout = root.AddComponent<CondoGameplayLayout>();
                layout.Build(null, null, null, null);
                Assert.IsTrue(layout.IsPrepared);
                Assert.IsNotNull(layout.AssemblyPoint);
                Assert.IsNotNull(layout.CoverTarget);
                Assert.AreEqual(4, layout.StagedHazards.Length);
                Assert.AreEqual(4, root.GetComponentsInChildren<MovableFurniture>().Length);
                Assert.AreEqual(2, root.GetComponentsInChildren<WearableShoes>().Length);

                Transform chair = root.transform.Find("Condo Chair - Cover Approach");
                Assert.IsNotNull(chair);
                chair.localPosition += Vector3.right * 0.4f;
                Vector3 editedPosition = chair.localPosition;
                int savedChildren = root.transform.childCount;
                layout.Build(null, null, null, null);

                Assert.AreEqual(savedChildren, root.transform.childCount,
                    "Repeated initialization must not duplicate saved objects.");
                Assert.That(Vector3.Distance(editedPosition, chair.localPosition), Is.LessThan(0.001f),
                    "Opening or playing the scene must not reset a moved chair.");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void CondoProps_ExposePhysicsAndSafetyComponents()
        {
            var root = new GameObject("Condo Gameplay Test");
            try
            {
                CondoGameplayLayout layout = root.AddComponent<CondoGameplayLayout>();
                layout.Build(null, null, null, null);
                foreach (MovableFurniture chair in root.GetComponentsInChildren<MovableFurniture>())
                {
                    Assert.IsNotNull(chair.GetComponent<Rigidbody>());
                    Assert.IsNotNull(chair.GetComponent<InertialRigidbody>());
                }
                Assert.IsNotNull(root.GetComponentInChildren<ProtectivePillow>());
                Assert.IsNotNull(root.GetComponentInChildren<CoverZone>());
                Assert.AreEqual(4, root.GetComponentsInChildren<FallingHazard>().Length);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }
    }
}
