using UnityEngine;

namespace RealmCommander.Core
{
    public static class TimeScaleManager
    {
        private static float currentScale = 1f;
        private static bool isPaused = false;

        public static float CurrentScale => currentScale;
        public static bool IsPaused => isPaused;

        public static void SetTimeScale(float scale)
        {
            currentScale = scale;
            ApplyTimeScale();
        }

        public static void SetPaused(bool paused)
        {
            isPaused = paused;
            ApplyTimeScale();
        }

        public static void Reset()
        {
            currentScale = 1f;
            isPaused = false;
            Time.timeScale = 1f;
        }

        private static void ApplyTimeScale()
        {
            Time.timeScale = isPaused ? 0f : currentScale;
        }
    }
}
