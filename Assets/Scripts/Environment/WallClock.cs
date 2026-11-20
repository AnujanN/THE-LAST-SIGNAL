using UnityEngine;

namespace LastSignal.Environment
{
    /// <summary>
    /// A ticking wall clock: hour, minute and second hands that start at a set time and run in real time,
    /// with a soft mechanical tick every second (made here, so no sound file is needed).
    /// The hands turn about their local Z axis (the clock face looks along -Z).
    /// </summary>
    public class WallClock : MonoBehaviour
    {
        [SerializeField] private Transform hourHand;
        [SerializeField] private Transform minuteHand;
        [SerializeField] private Transform secondHand;
        [SerializeField] private int startHour = 23;
        [SerializeField] private int startMinute = 47;
        [SerializeField] private AudioSource tickSource;
        [SerializeField, Range(0f, 1f)] private float tickVolume = 0.35f;

        private float seconds;
        private int lastTick = -1;
        private AudioClip tick, tock;

        private void Start()
        {
            seconds = startHour * 3600f + startMinute * 60f;
            tick = MakeTick("ClockTick", 3100f);
            tock = MakeTick("ClockTock", 2500f);
        }

        private void Update()
        {
            seconds += Time.deltaTime;
            int whole = Mathf.FloorToInt(seconds);
            if (whole != lastTick)
            {
                lastTick = whole;
                if (tickSource != null) tickSource.PlayOneShot(whole % 2 == 0 ? tick : tock, tickVolume * Save.GameSettings.SfxVolume);
            }
            // The second hand jumps once a second; the others sweep.
            float s = whole % 60;
            float m = seconds / 60f % 60f;
            float h = seconds / 3600f % 12f;
            if (secondHand != null) secondHand.localRotation = Quaternion.Euler(0f, 0f, -s * 6f);
            if (minuteHand != null) minuteHand.localRotation = Quaternion.Euler(0f, 0f, -m * 6f);
            if (hourHand != null) hourHand.localRotation = Quaternion.Euler(0f, 0f, -h * 30f);
        }

        /// <summary>A short wooden click: a burst of noise ringing at one frequency and dying in a few ms.</summary>
        private static AudioClip MakeTick(string name, float ring)
        {
            const int rate = 44100;
            int n = rate / 25;
            var data = new float[n];
            var rng = new System.Random(name.GetHashCode());
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)rate;
                float env = Mathf.Exp(-t * 900f);
                float noise = (float)(rng.NextDouble() * 2.0 - 1.0);
                data[i] = (0.55f * Mathf.Sin(2f * Mathf.PI * ring * t) + 0.45f * noise) * env * 0.8f;
            }
            AudioClip clip = AudioClip.Create(name, n, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
