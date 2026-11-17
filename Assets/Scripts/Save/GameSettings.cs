using System;
using UnityEngine;

namespace LastSignal.Save
{
    /// <summary>
    /// Player settings stored in PlayerPrefs: graphics preset, look sensitivity,
    /// volumes and subtitles. Call Apply() after changing values.
    /// </summary>
    public static class GameSettings
    {
        private const string KQuality = "ls.quality";
        private const string KSensitivity = "ls.sensitivity";
        private const string KMaster = "ls.vol.master";
        private const string KAmbience = "ls.vol.ambience";
        private const string KSfx = "ls.vol.sfx";
        private const string KSubtitles = "ls.subtitles";

        public static event Action Changed;

        /// <summary>Display names. "High" maps to the PC quality level and is not offered on phones.</summary>
        public static string[] QualityOptions =>
            Application.isMobilePlatform ? new[] { "Low", "Medium" } : new[] { "Low", "Medium", "High" };

        public static int QualityOption
        {
            get
            {
                if (PlayerPrefs.HasKey(KQuality)) return Mathf.Clamp(PlayerPrefs.GetInt(KQuality), 0, QualityOptions.Length - 1);
                string current = QualitySettings.names[QualitySettings.GetQualityLevel()];
                return current == "Low" ? 0 : current == "PC" && !Application.isMobilePlatform ? 2 : 1;
            }
            set => PlayerPrefs.SetInt(KQuality, Mathf.Clamp(value, 0, QualityOptions.Length - 1));
        }

        public static float LookSensitivity
        {
            get => PlayerPrefs.GetFloat(KSensitivity, 1f);
            set => PlayerPrefs.SetFloat(KSensitivity, Mathf.Clamp(value, 0.25f, 2.5f));
        }

        public static float MasterVolume
        {
            get => PlayerPrefs.GetFloat(KMaster, 1f);
            set => PlayerPrefs.SetFloat(KMaster, Mathf.Clamp01(value));
        }

        public static float AmbienceVolume
        {
            get => PlayerPrefs.GetFloat(KAmbience, 0.8f);
            set => PlayerPrefs.SetFloat(KAmbience, Mathf.Clamp01(value));
        }

        public static float SfxVolume
        {
            get => PlayerPrefs.GetFloat(KSfx, 1f);
            set => PlayerPrefs.SetFloat(KSfx, Mathf.Clamp01(value));
        }

        /// <summary>Optional ambient lines. Story-critical lines are always subtitled.</summary>
        public static bool Subtitles
        {
            get => PlayerPrefs.GetInt(KSubtitles, 1) == 1;
            set => PlayerPrefs.SetInt(KSubtitles, value ? 1 : 0);
        }

        public static void Apply()
        {
            if (PlayerPrefs.HasKey(KQuality))
            {
                string wanted = QualityOptions[QualityOption];
                if (wanted == "High") wanted = "PC";
                int index = Array.IndexOf(QualitySettings.names, wanted);
                if (index >= 0 && index != QualitySettings.GetQualityLevel()) QualitySettings.SetQualityLevel(index, true);
            }
            AudioListener.volume = MasterVolume;
            PlayerPrefs.Save();
            Changed?.Invoke();
        }
    }
}
