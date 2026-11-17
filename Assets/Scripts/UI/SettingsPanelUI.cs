using LastSignal.Save;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LastSignal.UI
{
    /// <summary>
    /// Settings page shared by the main menu and the pause menu: graphics preset, look
    /// sensitivity, volumes and optional subtitles. Values are saved immediately.
    /// </summary>
    public class SettingsPanelUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text qualityLabel;
        [SerializeField] private Slider sensitivity;
        [SerializeField] private Slider masterVolume;
        [SerializeField] private Slider ambienceVolume;
        [SerializeField] private Slider sfxVolume;
        [SerializeField] private TMP_Text subtitlesLabel;

        private bool refreshing;

        private void OnEnable() => Refresh();

        private void Awake()
        {
            Hook(sensitivity, 0.25f, 2.5f, v => GameSettings.LookSensitivity = v);
            Hook(masterVolume, 0f, 1f, v => GameSettings.MasterVolume = v);
            Hook(ambienceVolume, 0f, 1f, v => GameSettings.AmbienceVolume = v);
            Hook(sfxVolume, 0f, 1f, v => GameSettings.SfxVolume = v);
        }

        private void Hook(Slider s, float min, float max, System.Action<float> set)
        {
            if (s == null) return;
            s.minValue = min;
            s.maxValue = max;
            s.onValueChanged.AddListener(v =>
            {
                if (refreshing) return;
                set(v);
                GameSettings.Apply();
            });
        }

        public void CycleQuality()
        {
            int count = GameSettings.QualityOptions.Length;
            GameSettings.QualityOption = (GameSettings.QualityOption + 1) % count;
            GameSettings.Apply();
            Refresh();
        }

        public void ToggleSubtitles()
        {
            GameSettings.Subtitles = !GameSettings.Subtitles;
            GameSettings.Apply();
            Refresh();
        }

        private void Refresh()
        {
            refreshing = true;
            if (qualityLabel != null) qualityLabel.text = "GRAPHICS: " + GameSettings.QualityOptions[GameSettings.QualityOption].ToUpperInvariant();
            if (subtitlesLabel != null) subtitlesLabel.text = "AMBIENT SUBTITLES: " + (GameSettings.Subtitles ? "ON" : "OFF");
            if (sensitivity != null) sensitivity.value = GameSettings.LookSensitivity;
            if (masterVolume != null) masterVolume.value = GameSettings.MasterVolume;
            if (ambienceVolume != null) ambienceVolume.value = GameSettings.AmbienceVolume;
            if (sfxVolume != null) sfxVolume.value = GameSettings.SfxVolume;
            refreshing = false;
        }
    }
}
