using UnityEngine;

namespace LastSignal.Audio
{
    /// <summary>
    /// Generated placeholder sounds (no audio files needed). Each clip is built once on first use.
    /// Replace with licensed or original recordings later; nothing story-critical relies on them.
    /// </summary>
    public static class PlaceholderClips
    {
        private const int Rate = 22050;
        private static AudioClip rain, hum, drips, interference, dawn, drone, heartbeat;
        private static AudioClip staticBurst, spark, thud, scream, footstep, splash, beep, buzz, doorCreak;

        public static AudioClip Rain => rain ??= Make("Rain", 4f, RainSample);
        public static AudioClip Hum => hum ??= Make("Hum", 4f, HumSample);
        public static AudioClip Drips => drips ??= Make("Drips", 6f, DripSample);
        public static AudioClip Interference => interference ??= Make("Interference", 4f, InterferenceSample);
        public static AudioClip Dawn => dawn ??= Make("Dawn", 6f, DawnSample);
        public static AudioClip ThreatDrone => drone ??= Make("ThreatDrone", 4f, DroneSample);
        public static AudioClip Heartbeat => heartbeat ??= Make("Heartbeat", 1f, HeartSample);

        public static AudioClip StaticBurst => staticBurst ??= Make("Static", 0.6f, (t, n) => Noise(n) * 0.5f * Env(t, 0.6f));
        public static AudioClip Spark => spark ??= Make("Spark", 0.35f, (t, n) => Noise(n) * Mathf.Exp(-t * 14f) * (Mathf.Sin(t * 900f) > 0f ? 0.8f : 0.3f));
        public static AudioClip Thud => thud ??= Make("Thud", 0.4f, (t, n) => Mathf.Sin(2f * Mathf.PI * 70f * t) * Mathf.Exp(-t * 10f) * 0.9f);
        public static AudioClip Scream => scream ??= Make("Scream", 1.8f, ScreamSample);
        public static AudioClip Footstep => footstep ??= Make("Footstep", 0.14f, (t, n) => (Noise(n) * 0.5f + Mathf.Sin(2f * Mathf.PI * 110f * t)) * Mathf.Exp(-t * 40f) * 0.5f);
        public static AudioClip Splash => splash ??= Make("Splash", 0.4f, (t, n) => Noise(n) * Mathf.Exp(-t * 8f) * 0.5f);
        public static AudioClip Beep => beep ??= Make("Beep", 0.15f, (t, n) => Mathf.Sin(2f * Mathf.PI * 880f * t) * Env(t, 0.15f) * 0.4f);
        public static AudioClip Buzz => buzz ??= Make("Buzz", 0.5f, (t, n) => Mathf.Sign(Mathf.Sin(2f * Mathf.PI * 120f * t)) * Env(t, 0.5f) * 0.25f);
        public static AudioClip DoorCreak => doorCreak ??= Make("Door", 0.9f, (t, n) => Mathf.Sin(2f * Mathf.PI * (180f + 90f * Mathf.Sin(t * 9f)) * t) * Env(t, 0.9f) * 0.3f);

        private delegate float Sampler(float t, int n);

        private static AudioClip Make(string name, float seconds, Sampler sampler)
        {
            int count = Mathf.CeilToInt(seconds * Rate);
            float[] data = new float[count];
            Random.State saved = Random.state;
            Random.InitState(name.GetHashCode());
            for (int i = 0; i < count; i++) data[i] = Mathf.Clamp(sampler(i / (float)Rate, i), -1f, 1f);
            Random.state = saved;
            AudioClip clip = AudioClip.Create("Placeholder_" + name, count, 1, Rate, false);
            clip.SetData(data, 0);
            // Held only by static fields, so stop scene loads from unloading it.
            clip.hideFlags = HideFlags.DontUnloadUnusedAsset;
            return clip;
        }

        private static float Noise(int n) => Random.value * 2f - 1f;
        private static float Env(float t, float length) => Mathf.Clamp01(t * 40f) * Mathf.Clamp01((length - t) * 20f);

        // Simple one-pole low-pass state for noise beds.
        private static float lp1, lp2;

        private static float RainSample(float t, int n)
        {
            lp1 += (Noise(n) - lp1) * 0.35f;
            lp2 += (Noise(n) - lp2) * 0.02f; // wind rumble
            float gust = 0.6f + 0.4f * Mathf.Sin(2f * Mathf.PI * 0.25f * t);
            return lp1 * 0.35f + lp2 * 1.2f * gust;
        }

        private static float HumSample(float t, int n)
        {
            float s = Mathf.Sin(2f * Mathf.PI * 50f * t) * 0.18f + Mathf.Sin(2f * Mathf.PI * 100f * t) * 0.08f;
            lp1 += (Noise(n) - lp1) * 0.05f;
            return s + lp1 * 0.15f;
        }

        private static float DripSample(float t, int n)
        {
            lp1 += (Noise(n) - lp1) * 0.03f;
            float s = lp1 * 0.4f;
            float[] drops = { 0.4f, 1.7f, 2.3f, 3.9f, 5.1f };
            foreach (float d in drops)
            {
                float dt = t - d;
                if (dt >= 0f && dt < 0.25f) s += Mathf.Sin(2f * Mathf.PI * (1400f - 2000f * dt) * dt) * Mathf.Exp(-dt * 25f) * 0.35f;
            }
            return s;
        }

        private static float InterferenceSample(float t, int n)
        {
            lp1 += (Noise(n) - lp1) * 0.6f;
            float bursts = Mathf.PerlinNoise(t * 3f, 0.5f);
            return lp1 * 0.25f * bursts + Mathf.Sin(2f * Mathf.PI * 60f * t) * 0.06f;
        }

        private static float DawnSample(float t, int n)
        {
            lp2 += (Noise(n) - lp2) * 0.01f;
            float chirp = 0f;
            float[] birds = { 0.8f, 2.6f, 4.4f };
            foreach (float b in birds)
            {
                float dt = t - b;
                if (dt >= 0f && dt < 0.18f) chirp += Mathf.Sin(2f * Mathf.PI * (2600f + 1500f * dt * 5f) * dt) * Env(dt, 0.18f) * 0.12f;
            }
            return lp2 * 0.6f + chirp;
        }

        private static float DroneSample(float t, int n)
        {
            return (Mathf.Sin(2f * Mathf.PI * 41f * t) + Mathf.Sin(2f * Mathf.PI * 43.5f * t) * 0.8f
                  + Mathf.Sin(2f * Mathf.PI * 82f * t) * 0.3f) * 0.25f;
        }

        private static float HeartSample(float t, int n)
        {
            float a = t < 0.12f ? Mathf.Sin(2f * Mathf.PI * 55f * t) * Mathf.Exp(-t * 30f) : 0f;
            float t2 = t - 0.25f;
            float b = t2 > 0f && t2 < 0.12f ? Mathf.Sin(2f * Mathf.PI * 50f * t2) * Mathf.Exp(-t2 * 30f) * 0.7f : 0f;
            return (a + b) * 0.9f;
        }

        private static float ScreamSample(float t, int n)
        {
            float f = 300f + 500f * Mathf.Sin(t * 2.2f);
            return (Mathf.Sin(2f * Mathf.PI * f * t) * 0.4f + Noise(n) * 0.35f) * Env(t, 1.8f);
        }
    }
}
