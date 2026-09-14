using System;
using NUnit.Framework;
using UnityEngine;

namespace ChulaEarthquakeVR.Tests
{
    public sealed class TeamFurnitureAssetTests
    {
        [TestCase("Bed")]
        [TestCase("Bed_Pillow")]
        [TestCase("DiningChair")]
        [TestCase("DiningTable")]
        [TestCase("Fridge")]
        [TestCase("Sofa")]
        [TestCase("Sofa_Pillows")]
        [TestCase("Vase")]
        [TestCase("Wandrobe")]
        public void TeamFurniture_IsLoadableAndHasVisibleGeometry(string modelName)
        {
            GameObject model = Resources.Load<GameObject>("PlengFurniture/" + modelName);
            Assert.IsNotNull(model, modelName + " must be included in Resources.");
            int visibleMeshes = 0;
            foreach (Renderer renderer in model.GetComponentsInChildren<Renderer>(true))
                if (renderer.name.IndexOf("Collision", StringComparison.OrdinalIgnoreCase) < 0) visibleMeshes++;
            Assert.Greater(visibleMeshes, 0, modelName + " has no non-collision renderer.");
        }
    }
}
