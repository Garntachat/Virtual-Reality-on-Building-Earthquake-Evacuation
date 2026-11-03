using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace ChulaEarthquakeVR.Tests
{
    public sealed class TutorialCondoResponseRootTests
    {
        private static Transform FindRuntimeResponseRoot(Transform leaf)
        {
            MethodInfo method = typeof(EarthquakeSceneResponseInstaller).GetMethod(
                "FindResponseRoot", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(method);
            return (Transform)method.Invoke(null, new object[] { leaf });
        }

        [Test]
        public void TutorialEditableFurnitureGroup_DoesNotShakeAsOneLargeObject()
        {
            GameObject group = new GameObject("Tutorial Editable Furniture");
            try
            {
                GameObject sofa = new GameObject("TeamFurniture_Sofa");
                sofa.transform.SetParent(group.transform);
                GameObject surface = GameObject.CreatePrimitive(PrimitiveType.Cube);
                surface.name = "AuthoredSurface";
                surface.transform.SetParent(sofa.transform);
                Assert.AreSame(sofa.transform, FindRuntimeResponseRoot(surface.transform),
                    "The sofa itself, not the saved furniture container, must receive the quake response.");
            }
            finally { Object.DestroyImmediate(group); }
        }

        [Test]
        public void CondoGameplayGroup_DoesNotMoveAsOneObject()
        {
            GameObject group = new GameObject("Condo Gameplay");
            try
            {
                GameObject table = new GameObject("Condo Sturdy Table");
                table.transform.SetParent(group.transform);
                GameObject top = GameObject.CreatePrimitive(PrimitiveType.Cube);
                top.name = "Table Top";
                top.transform.SetParent(table.transform);
                Assert.AreSame(table.transform, FindRuntimeResponseRoot(top.transform));
            }
            finally { Object.DestroyImmediate(group); }
        }

        [Test]
        public void AuthoredFurnitureResponse_IsPreferredAndKeepsItsStrength()
        {
            GameObject group = new GameObject("Tutorial Editable Props");
            try
            {
                GameObject model = new GameObject("Kenney_books");
                model.transform.SetParent(group.transform);
                DecorativeQuakeResponse authored = model.AddComponent<DecorativeQuakeResponse>();
                authored.Configure(null, DecorativeQuakeMode.Loose, 0.42f);
                GameObject mesh = GameObject.CreatePrimitive(PrimitiveType.Cube);
                mesh.name = "AuthoredSurface";
                mesh.transform.SetParent(model.transform);

                Assert.AreSame(model.transform, FindRuntimeResponseRoot(mesh.transform));
                Assert.That(authored.ShakeStrength, Is.EqualTo(0.42f).Within(0.001f));
                Assert.AreEqual(1, model.GetComponents<DecorativeQuakeResponse>().Length);
            }
            finally { Object.DestroyImmediate(group); }
        }

        [Test]
        public void ExistingVisualSway_DoesNotGetSecondShaker()
        {
            GameObject fixture = new GameObject("HangingLamp_1");
            try
            {
                fixture.AddComponent<VisualQuakeSway>();
                GameObject mesh = GameObject.CreatePrimitive(PrimitiveType.Cube);
                mesh.name = "Lamp Mesh";
                mesh.transform.SetParent(fixture.transform);
                Assert.IsNull(FindRuntimeResponseRoot(mesh.transform));
            }
            finally { Object.DestroyImmediate(fixture); }
        }
    }
}
