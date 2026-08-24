namespace ChulaEarthquakeVR
{
    public static class TutorialRunRules
    {
        public static bool CanStartEarthquake(
            float phaseElapsed,
            float normalActivitySeconds,
            bool requireTasks,
            bool tasksComplete,
            float watchdogSeconds)
        {
            if (phaseElapsed < normalActivitySeconds) return false;
            if (!requireTasks || tasksComplete) return true;
            return phaseElapsed >= watchdogSeconds;
        }

        public static bool IsSuccessfulEvacuation(
            GameplayPhase phase,
            bool atAssemblyPoint,
            float health)
        {
            return phase == GameplayPhase.PostQuakeEvacuation && atAssemblyPoint && health > 0f;
        }

        public static bool IsFailure(float health, float evacuationElapsed, float timeoutSeconds)
        {
            return health <= 0f || evacuationElapsed >= timeoutSeconds;
        }
    }
}
