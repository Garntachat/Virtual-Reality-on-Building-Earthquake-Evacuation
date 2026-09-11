using NUnit.Framework;
using UnityEngine;
using UnityEditor;

namespace ChulaEarthquakeVR.Tests
{
    public sealed class StudentAssetTests
    {
        [Test]
        public void Student_HasSkinTextureAndMatchingAnimationBindings()
        {
            GameObject model = Resources.Load<GameObject>("Student/Student");
            Assert.IsNotNull(model, "FBX did not import.");
            Assert.IsNotNull(Resources.Load<Texture2D>("Student/StudentUniform"));
            var skin = model.GetComponentInChildren<SkinnedMeshRenderer>();
            Assert.IsNotNull(skin, "Student must be a skinned model.");
            Assert.Greater(skin.bones.Length, 0);
            foreach (string name in new[] { "Idle", "Run" })
            {
                int clips = 0;
                foreach (AnimationClip clip in Resources.LoadAll<AnimationClip>("Student/" + name))
                {
                    if (clip.name.StartsWith("__preview__")) continue;
                    clips++;
                    Assert.IsTrue(clip.legacy, "Reimport student assets with StudentAssetImporter.");
                    Assert.Greater(clip.length, 0f);
                    foreach (var binding in AnimationUtility.GetCurveBindings(clip))
                        if (!string.IsNullOrEmpty(binding.path))
                            Assert.IsNotNull(model.transform.Find(binding.path), name + ": " + binding.path);
                }
                Assert.Greater(clips, 0, name + " has no playable clip.");
            }
        }
    }
}
