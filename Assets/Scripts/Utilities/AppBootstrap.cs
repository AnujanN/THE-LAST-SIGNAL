using UnityEngine;

namespace LastSignal.Utilities
{
    /// <summary>
    /// App-wide settings applied before the first scene loads.
    /// </summary>
    public static class AppBootstrap
    {
        /// <summary>Brief target: stable 30 FPS on the baseline phone.</summary>
        public const int MobileTargetFrameRate = 30;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Init()
        {
            if (Application.isMobilePlatform)
            {
                QualitySettings.vSyncCount = 0;
                Application.targetFrameRate = MobileTargetFrameRate;
                Screen.sleepTimeout = SleepTimeout.NeverSleep;
            }
        }
    }
}
