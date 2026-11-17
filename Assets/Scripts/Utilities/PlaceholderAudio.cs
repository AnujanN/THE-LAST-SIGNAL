using UnityEngine;

namespace LastSignal.Utilities
{
    /// <summary>
    /// Tiny generated sounds used until real audio assets exist. Created once, then cached.
    /// </summary>
    public static class PlaceholderAudio
    {
        private static AudioClip click;
        private static AudioClip pickup;

        public static AudioClip Click => click != null ? click : (click = Tone("Placeholder_Click", 1800f, 0.03f, 60f));
        public static AudioClip Pickup => pickup != null ? pickup : (pickup = Sweep("Placeholder_Pickup", 500f, 900f, 0.12f));

        private static AudioClip Tone(string name, float freq, float seconds, float decay)
        {
            const int rate = 22050;
            int n = Mathf.CeilToInt(rate * seconds);
            float[] data = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)rate;
                data[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * Mathf.Exp(-decay * t) * 0.5f;
            }
            return Make(name, data, rate);
        }

        private static AudioClip Sweep(string name, float from, float to, float seconds)
        {
            const int rate = 22050;
            int n = Mathf.CeilToInt(rate * seconds);
            float[] data = new float[n];
            float phase = 0f;
            for (int i = 0; i < n; i++)
            {
                float k = i / (float)n;
                phase += 2f * Mathf.PI * Mathf.Lerp(from, to, k) / rate;
                data[i] = Mathf.Sin(phase) * (1f - k) * 0.4f;
            }
            return Make(name, data, rate);
        }

        private static AudioClip Make(string name, float[] data, int rate)
        {
            AudioClip clip = AudioClip.Create(name, data.Length, 1, rate, false);
            clip.SetData(data, 0);
            clip.hideFlags = HideFlags.DontUnloadUnusedAsset;
            return clip;
        }
    }
}
