using NUnit.Framework;
using UnityEngine;

namespace ChulaEarthquakeVR.Tests
{
    public sealed class HouseAuthoredFurnitureTests
    {
        [Test]
        public void BindingPreservesEditedPoseAndMovesColliderWithGameplayAnchor()
        {
            var root = new GameObject("SavedLayout");
            var target = new GameObject("TestFurnitureAnchor");
            var saved = new GameObject("EditedFurniture");
            try
            {
                saved.transform.SetParent(root.transform);
                saved.transform.position = new Vector3(3, 4, 5);
                saved.transform.rotation = Quaternion.Euler(0, 70, 0);
                var savedCollider = saved.AddComponent<BoxCollider>();
                var oldCollider = target.AddComponent<BoxCollider>();
                var layout = root.AddComponent<HouseAuthoredFurniture>();
                layout.bindings = new[] { new HouseAuthoredFurniture.Binding {
                    furnishing = saved.transform, anchorName = target.name, anchorIndex = 0
                }};
                layout.BindRuntimeAnchors();
                Assert.That(target.transform.position, Is.EqualTo(new Vector3(3, 4, 5)));
                Assert.That(saved.transform.position, Is.EqualTo(new Vector3(3, 4, 5)));
                Assert.That(saved.transform.parent, Is.EqualTo(target.transform));
                Assert.That(savedCollider.enabled, Is.True);
                Assert.That(oldCollider.enabled, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(target);
                Object.DestroyImmediate(root);
            }
        }
    }
}
