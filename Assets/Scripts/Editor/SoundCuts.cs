using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace LastSignal.EditorTools
{
    /// <summary>
    /// Cuts the BigSoundBank recordings (Joseph Sardin, CC0) in Assets/Audio/Source/BigSoundBank into the
    /// game's sounds in Assets/Audio/Resources/Sounds: single footsteps and squeaks found by their onsets,
    /// the loudest stretch of a long take, seamless loops for breathing and the fluorescent hum.
    /// The source files are not in the build (nothing references them); only the cuts ship.
    /// </summary>
    public static class SoundCuts
    {
        private const string Src = "Assets/Audio/Source/BigSoundBank/";
        private const string Dst = "Assets/Audio/Resources/Sounds/";

        [MenuItem("The Last Signal/Audio/Cut BigSoundBank Sounds")]
        public static void CutAll()
        {
            // Sets these recordings replace completely (old Kenney / generated variations removed).
            foreach (string key in new[] { "footstep", "door", "splash", "paper", "paperdown", "rain", "thunder", "whisper", "choke",
                                           "tapestart", "taperun", "tapestop", "carrain", "cardoor", "tune", "crackle",
                                           "engine", "engineoff", "wiper", "clock", "transmit" }) RemoveSet(key);

            // Intro: rain drumming on the car roof, the radio hunting through the band, the car door.
            Loudest("1293", "carrain", 30f, rate: 24000, loop: true, level: 0.06f);
            First("1526", "cardoor", 3.5f);
            First("2618", "tune", 7f);
            First("2617", "tune_1", 5f);
            First("0312", "crackle", 4f);
            // The drive: a steady stretch of a car interior at 80 km/h (loop), the engine idling and switching off,
            // single wiper strokes from inside the car; the flat: a quartz wall clock ticking (loop).
            Segment("3098", "engine", 15f, 18f, loop: true, rate: 24000, level: 0.06f);
            Segment("0557", "engineoff", 30.4f, 3.5f);
            Onsets("3112", "wiper", 3, 1.3f, minGap: 1.0f);
            Segment("1069", "clock", 2f, 12f, loop: true, rate: 24000);

            // Storm outside Level 1: a real outdoor downpour (a steady stretch between the thunder rolls of
            // "Rain and Thunder #2", looped), and thunder on its own that plays now and then: two single claps
            // and a roll from the same storm recording, plus the two older distant rumbles.
            Segment("0740", "rain", 64f, 20f, loop: true, rate: 24000, level: 0.07f);
            First("3181", "thunder", 16f);
            First("3184", "thunder_1", 14f);
            Segment("0740", "thunder_2", 4f, 14f);
            First("3115", "thunder_3", 14f);
            First("3180", "thunder_4", 12f);
            // The title's red dot: one burst of a real radio-paging transmitter (Eurosignal).
            Segment("3571", "transmit", 10f, 0.42f);

            // Cassette deck: PLAY key down, the tape running under the voices (loop), the key popping up.
            First("0565", "tapestart", 1f);
            Segment("0565", "taperun", 3f, 5f, loop: true, rate: 24000, level: 0.05f);
            LastPeak("0570", "tapestop", 0.8f);

            // The Echo: wordless whispers instead of snarls, and a strangled gasp when it catches you.
            Windows("3255", "whisper", 2, 3.5f, 0, level: 0.08f);
            Windows("3256", "whisper", 2, 3.5f, 2, level: 0.08f);
            Loudest("1639", "choke", 2.2f);
            Loudest("1640", "choke_1", 2.2f);

            // Mice and rats.
            Onsets("0459", "squeak", 4, 0.6f, minGap: 0.5f);
            Loudest("0185", "gnaw", 6f, rate: 24000, level: 0.12f);

            // Alex: frightened breathing (seamless loop) and the gasp when breath runs out.
            Loudest("1103", "breathing", 6f, rate: 24000, loop: true);
            Loudest("1103", "gasp", 0.9f);

            // Footsteps on concrete, one step per file.
            Onsets("0514", "footstep", 6, 0.3f, minGap: 0.25f);
            // Footsteps on creaking boards for the "someone upstairs" scare.
            Onsets("3440", "creakstep", 5, 0.7f, minGap: 0.4f);

            // Creaking doors (also lockers and wardrobes).
            // Cut to 1.5 s: the time a door takes to swing open (Door.openSeconds).
            First("0302", "door", 1.5f);
            First("3205", "door_1", 1.5f);
            First("3206", "door_2", 1.5f);
            First("3207", "door_3", 1.5f);
            First("0306", "door_4", 1.5f);

            // Wading through water and wet muck (flooded rooms and puddles).
            Onsets("0690", "splash", 6, 0.55f, minGap: 0.45f);
            Onsets("0495", "splash", 2, 0.55f, minGap: 0.45f, first: 6);

            // Paper: picked up and unfolded, then put back down.
            Onsets("0785", "paper", 3, 0.8f, minGap: 1f);
            First("1225", "paper_3", 1f);
            First("0164", "paperdown", 0.8f);
            First("2212", "paperdown_1", 0.9f);
            First("0362", "paperdown_2", 0.9f);

            // Dying fluorescent tubes: flicks while sputtering, hum while lit.
            First("2143", "tube", 1.2f);
            First("2144", "tube_1", 1.2f);
            First("2145", "tube_2", 1.2f);
            First("2146", "tube_3", 1.5f);
            Loudest("2118", "neon", 5f, rate: 24000, loop: true);

            // Ending: birds waking at sunrise (40 s, faded in and out).
            Loudest("0222", "dawn", 40f, rate: 24000, fade: 2.5f);

            AssetDatabase.Refresh();
            Debug.Log("[SoundCuts] BigSoundBank sounds cut into " + Dst);
        }

        // ------------------------------------------------------------------ cuts

        private static void Loudest(string id, string name, float seconds, int rate = 0, bool loop = false, float fade = 0.03f, float level = 0f)
        {
            float[] d = Load(id, out int hz);
            int len = Mathf.Min(d.Length, Mathf.RoundToInt(seconds * hz));
            int best = 0;
            double sum = 0, top = -1;
            for (int i = 0; i < d.Length; i++)
            {
                sum += d[i] * d[i];
                if (i >= len) sum -= d[i - len] * d[i - len];
                if (i >= len - 1 && sum > top) { top = sum; best = i - len + 1; }
            }
            Write(name, Slice(d, best, len, loop ? 0f : fade, loop ? 0f : Mathf.Max(fade, 0.08f), loop, hz, level), hz, rate);
        }

        private static void First(string id, string name, float maxSeconds)
        {
            float[] d = Load(id, out int hz);
            int start = Mathf.Max(0, OnsetTimes(d, hz, 0.1f, 0.1f)[0] - hz / 100);
            Write(name, Slice(d, start, Mathf.Min(d.Length - start, Mathf.RoundToInt(maxSeconds * hz)), 0.005f, 0.25f, false, hz), hz, 0);
        }

        private static void Onsets(string id, string key, int count, float seconds, float minGap, int first = 0)
        {
            float[] d = Load(id, out int hz);
            List<int> on = PeakTimes(d, hz, 0.3f, minGap);
            // Spread the picks across the take rather than the first few.
            int made = first;
            for (int k = 0; k < count && on.Count > 0; k++)
            {
                int start = Mathf.Max(0, on[Mathf.Min(on.Count - 1, k * on.Count / count)] - hz / 25);
                int len = Mathf.Min(d.Length - start, Mathf.RoundToInt(seconds * hz));
                Write(made == 0 ? key : key + "_" + made, Slice(d, start, len, 0.003f, seconds * 0.35f, false, hz), hz, 0);
                made++;
            }
        }

        /// <summary>A fixed stretch of a take (seconds in, length).</summary>
        private static void Segment(string id, string name, float start, float seconds, bool loop = false, int rate = 0, float level = 0f)
        {
            float[] d = Load(id, out int hz);
            int s0 = Mathf.Clamp(Mathf.RoundToInt(start * hz), 0, d.Length - 1);
            int len = Mathf.Min(d.Length - s0, Mathf.RoundToInt(seconds * hz));
            Write(name, Slice(d, s0, len, loop ? 0f : 0.03f, loop ? 0f : 0.1f, loop, hz, level), hz, rate);
        }

        /// <summary>The last strong transient of a take (a key popping up at the end).</summary>
        private static void LastPeak(string id, string name, float seconds)
        {
            float[] d = Load(id, out int hz);
            List<int> peaks = PeakTimes(d, hz, 0.35f, 0.3f);
            int start = Mathf.Max(0, peaks[peaks.Count - 1] - hz / 25);
            Write(name, Slice(d, start, Mathf.Min(d.Length - start, Mathf.RoundToInt(seconds * hz)), 0.003f, 0.2f, false, hz), hz, 0);
        }

        /// <summary>The loudest non-overlapping stretches of a take, named key, key_1 ... from <paramref name="first"/>.</summary>
        private static void Windows(string id, string key, int count, float seconds, int first, float level = 0f)
        {
            float[] d = Load(id, out int hz);
            int len = Mathf.Min(d.Length, Mathf.RoundToInt(seconds * hz)), hop = hz / 10;
            var windows = new List<(int start, double energy)>();
            for (int st = 0; st + len <= d.Length; st += hop)
            {
                double e = 0;
                for (int i = st; i < st + len; i += 4) e += d[i] * d[i];
                windows.Add((st, e));
            }
            windows.Sort((a, b) => b.energy.CompareTo(a.energy));
            var used = new List<int>();
            foreach (var w in windows)
            {
                if (used.Count >= count) break;
                if (used.Exists(u => Mathf.Abs(u - w.start) < len)) continue;
                used.Add(w.start);
                int n = first + used.Count - 1;
                Write(n == 0 ? key : key + "_" + n, Slice(d, w.start, len, 0.15f, 0.4f, false, hz, level), hz, 0);
            }
        }

        // ------------------------------------------------------------------ helpers

        private static float[] Load(string id, out int hz)
        {
            string path = Src + id + ".ogg";
            var imp = (AudioImporter)AssetImporter.GetAtPath(path);
            AudioImporterSampleSettings s = imp.defaultSampleSettings;
            if (!imp.forceToMono || s.loadType != AudioClipLoadType.DecompressOnLoad || s.compressionFormat != AudioCompressionFormat.PCM)
            {
                imp.forceToMono = true;
                s.loadType = AudioClipLoadType.DecompressOnLoad;
                s.compressionFormat = AudioCompressionFormat.PCM;
                s.preloadAudioData = true;
                imp.defaultSampleSettings = s;
                imp.SaveAndReimport();
            }
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            clip.LoadAudioData();
            var data = new float[clip.samples];
            clip.GetData(data, 0);
            hz = clip.frequency;
            return data;
        }

        /// <summary>The strongest transients (envelope peaks), at least minGap apart, in time order.</summary>
        private static List<int> PeakTimes(float[] d, int hz, float threshold, float minGapSeconds)
        {
            int hop = Mathf.Max(1, hz / 200), win = hop * 2;
            var env = new List<float>();
            for (int s = 0; s + win < d.Length; s += hop)
            {
                double e = 0;
                for (int i = s; i < s + win; i++) e += d[i] * d[i];
                env.Add(Mathf.Sqrt((float)(e / win)));
            }
            float peak = 0f;
            foreach (float e in env) peak = Mathf.Max(peak, e);
            var candidates = new List<int>();
            for (int k = 1; k < env.Count - 1; k++)
                if (env[k] >= env[k - 1] && env[k] >= env[k + 1] && env[k] > peak * threshold) candidates.Add(k);
            candidates.Sort((a, b) => env[b].CompareTo(env[a]));
            int gap = Mathf.RoundToInt(minGapSeconds * hz);
            var picked = new List<int>();
            foreach (int k in candidates)
                if (!picked.Exists(p => Mathf.Abs(p - k * hop) < gap)) picked.Add(k * hop);
            picked.Sort();
            if (picked.Count == 0) picked.Add(0);
            return picked;
        }

        /// <summary>Sample indices where the sound starts (envelope rises past a share of its peak).</summary>
        private static List<int> OnsetTimes(float[] d, int hz, float threshold, float minGapSeconds)
        {
            int hop = Mathf.Max(1, hz / 200), win = hop * 2;
            var env = new List<float>();
            for (int s = 0; s + win < d.Length; s += hop)
            {
                double e = 0;
                for (int i = s; i < s + win; i++) e += d[i] * d[i];
                env.Add(Mathf.Sqrt((float)(e / win)));
            }
            float peak = 0f;
            foreach (float e in env) peak = Mathf.Max(peak, e);
            var on = new List<int>();
            bool armed = true;
            int last = -int.MaxValue / 2, gap = Mathf.RoundToInt(minGapSeconds * hz);
            for (int k = 0; k < env.Count; k++)
            {
                if (armed && env[k] > peak * threshold && k * hop - last >= gap) { on.Add(k * hop); last = k * hop; armed = false; }
                else if (env[k] < peak * threshold * 0.5f) armed = true;
            }
            if (on.Count == 0) on.Add(0);
            return on;
        }

        /// <param name="level">If above 0: scale to this average loudness (RMS) with a soft limiter, for takes
        /// whose few loud clicks would otherwise leave the rest too quiet.</param>
        private static float[] Slice(float[] d, int start, int len, float fadeIn, float fadeOut, bool loop, int hz, float level = 0f)
        {
            len = Mathf.Max(1, Mathf.Min(len, d.Length - start));
            var o = new float[len];
            System.Array.Copy(d, start, o, 0, len);
            if (loop)
            {
                // Crossfade the tail into the head so the loop has no seam.
                int x = Mathf.Min(len / 4, hz / 3);
                for (int i = 0; i < x; i++)
                {
                    float t = i / (float)x;
                    o[i] = o[i] * t + o[len - x + i] * (1f - t);
                }
                System.Array.Resize(ref o, len - x);
                len = o.Length;
            }
            else
            {
                int fi = Mathf.Min(len / 2, Mathf.RoundToInt(fadeIn * hz)), fo = Mathf.Min(len / 2, Mathf.RoundToInt(fadeOut * hz));
                for (int i = 0; i < fi; i++) o[i] *= i / (float)fi;
                for (int i = 0; i < fo; i++) o[len - 1 - i] *= i / (float)fo;
            }
            if (level > 0f)
            {
                double e = 0;
                foreach (float v in o) e += v * v;
                float rms = Mathf.Sqrt((float)(e / len));
                if (rms > 1e-5f)
                    for (int i = 0; i < len; i++) o[i] = 0.92f * (float)System.Math.Tanh(o[i] * level / rms / 0.92f);
                return o;
            }
            float peak = 0f;
            foreach (float v in o) peak = Mathf.Max(peak, Mathf.Abs(v));
            if (peak > 1e-4f) for (int i = 0; i < len; i++) o[i] *= 0.89f / peak;
            return o;
        }

        private static void RemoveSet(string key)
        {
            foreach (string f in Directory.GetFiles(Dst))
            {
                string n = Path.GetFileNameWithoutExtension(f);
                if (f.EndsWith(".meta")) continue;
                if (n == key || (n.StartsWith(key + "_") && int.TryParse(n.Substring(key.Length + 1), out _)))
                    AssetDatabase.DeleteAsset(f.Replace('\\', '/'));
            }
        }

        private static void Write(string name, float[] data, int hz, int rate)
        {
            if (rate > 0 && rate < hz)
            {
                // Linear resample for long ambience (smaller files; plenty for the phone speaker).
                int n = Mathf.RoundToInt(data.Length * (rate / (float)hz));
                var r = new float[n];
                for (int i = 0; i < n; i++)
                {
                    float p = i * (hz / (float)rate);
                    int a = Mathf.Min((int)p, data.Length - 1), b = Mathf.Min(a + 1, data.Length - 1);
                    r[i] = Mathf.Lerp(data[a], data[b], p - a);
                }
                data = r;
                hz = rate;
            }
            foreach (string ext in new[] { ".ogg", ".wav" })
                if (File.Exists(Dst + name + ext)) AssetDatabase.DeleteAsset(Dst + name + ext);
            using (var w = new BinaryWriter(File.Create(Dst + name + ".wav")))
            {
                int bytes = data.Length * 2;
                w.Write(System.Text.Encoding.ASCII.GetBytes("RIFF")); w.Write(36 + bytes);
                w.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt ")); w.Write(16); w.Write((short)1); w.Write((short)1);
                w.Write(hz); w.Write(hz * 2); w.Write((short)2); w.Write((short)16);
                w.Write(System.Text.Encoding.ASCII.GetBytes("data")); w.Write(bytes);
                foreach (float v in data) w.Write((short)Mathf.Clamp(Mathf.RoundToInt(v * 32767f), -32768, 32767));
            }
        }
    }
}
