using NUnit.Framework;

namespace ChulaEarthquakeVR.Tests
{
    public sealed class TutorialRunRulesTests
    {
        [Test]
        public void Earthquake_DoesNotStartBeforeMinimumTime()
        {
            Assert.IsFalse(TutorialRunRules.CanStartEarthquake(29.99f, 30f, false, false, 120f));
        }

        [Test]
        public void Earthquake_StartsAtThirtySeconds_WhenTasksNotRequired()
        {
            Assert.IsTrue(TutorialRunRules.CanStartEarthquake(30f, 30f, false, false, 120f));
        }

        [Test]
        public void Watchdog_PreventsTutorialFromBlockingForever()
        {
            Assert.IsFalse(TutorialRunRules.CanStartEarthquake(60f, 30f, true, false, 120f));
            Assert.IsTrue(TutorialRunRules.CanStartEarthquake(120f, 30f, true, false, 120f));
        }

        [TestCase(GameplayPhase.Earthquake, true, 100f, false)]
        [TestCase(GameplayPhase.PostQuakeEvacuation, false, 100f, false)]
        [TestCase(GameplayPhase.PostQuakeEvacuation, true, 0f, false)]
        [TestCase(GameplayPhase.PostQuakeEvacuation, true, 1f, true)]
        public void EvacuationSuccess_RequiresCorrectPhaseZoneAndHealth(
            GameplayPhase phase, bool atAssembly, float health, bool expected)
        {
            Assert.AreEqual(expected, TutorialRunRules.IsSuccessfulEvacuation(phase, atAssembly, health));
        }

        [Test]
        public void Failure_UsesHealthOrTimeout()
        {
            Assert.IsTrue(TutorialRunRules.IsFailure(0f, 1f, 60f));
            Assert.IsTrue(TutorialRunRules.IsFailure(100f, 60f, 60f));
            Assert.IsFalse(TutorialRunRules.IsFailure(1f, 59.99f, 60f));
        }
    }
}
