using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace ChulaEarthquakeVR.Tests
{
    public sealed class MainMenuTests
    {
        [Test]
        public void MenuIsFirstAndBothDestinationsAreEnabled()
        {
            var scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).ToArray();
            Assert.GreaterOrEqual(scenes.Length, 3);
            Assert.AreEqual(MainMenuController.MenuScene, Path.GetFileNameWithoutExtension(scenes[0].path));
            foreach (string name in new[] { MainMenuController.MenuScene, MainMenuController.TutorialScene, MainMenuController.HouseScene })
            {
                var matches = scenes.Where(scene => Path.GetFileNameWithoutExtension(scene.path) == name).ToArray();
                Assert.AreEqual(1, matches.Length, "Exactly one enabled scene required: " + name);
                Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<SceneAsset>(matches[0].path));
            }
        }

        [Test]
        public void MenuSceneContainsControllerAndNoGameplayComponents()
        {
            string scene = File.ReadAllText("Assets/CEVR/Generated/Scenes/CEVR_MainMenu.unity");
            string guid = AssetDatabase.AssetPathToGUID("Assets/CEVR/Runtime/UI/MainMenuController.cs");
            StringAssert.Contains(guid, scene);
            StringAssert.DoesNotContain("GameFlowController", scene);
            StringAssert.DoesNotContain("GroundMotionPlayer", scene);
        }

        [Test]
        public void AppearanceChoicesAreDistinctAndSelectionIsClamped()
        {
            const string key = "CEVR.StudentOutfit.v1";
            bool existed = PlayerPrefs.HasKey(key);
            int previous = PlayerPrefs.GetInt(key, 0);
            try
            {
                Assert.AreEqual(3, StudentAppearance.Names.Length);
                Assert.AreNotEqual(StudentAppearance.ShirtColor(0), StudentAppearance.ShirtColor(1));
                Assert.AreNotEqual(StudentAppearance.ShirtColor(1), StudentAppearance.ShirtColor(2));
                StudentAppearance.Select(-100);
                Assert.AreEqual(0, StudentAppearance.Selected);
                StudentAppearance.Select(100);
                Assert.AreEqual(2, StudentAppearance.Selected);
            }
            finally
            {
                if (existed) PlayerPrefs.SetInt(key, previous); else PlayerPrefs.DeleteKey(key);
                PlayerPrefs.Save();
            }
        }
    }
}
