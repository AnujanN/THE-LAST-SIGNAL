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
            foreach (string key in new[] { "footstep", "door" }) RemoveSet(key);

            // Birds.
            Loudest("1763", "owl", 4f);
            Loudest("1764", "owl_1", 4f);
            Loudest("0840", "flutter", 2f, level: 0.12f);
            Loudest("0476", "flutter_1", 2.2f, level: 0.12f);
            Loudest("2891", "flutter_2", 2f, level: 0.12f);
            Loudest("2892", "flutter_3", 2f, level: 0.12f);
            Coos("0841", "pigeon", 2, 2.2f);

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
            First("0302", "door", 2.5f);
            First("3205", "door_1", 2.5f);
            First("3206", "door_2", 2.5f);
            First("3207", "door_3", 2.5f);
            First("0306", "door_4", 3f);

            // Dying fluorescent tubes: flicks while sputtering, hum while lit.
            First("2143", "tube", 1.2f);
            First("2144", "tube_1", 1.2f);
            First("2145", "tube_2", 1.2f);
            First("2146", "tube_3", 1.5f);
            Loudest("2118", "neon", 5f, rate: 24000, loop: true);

            // The Echo crawling: joints cracking, teeth grinding.
            First("1408", "bonecrack", 1.2f);
            First("1409", "bonecrack_1", 1.2f);
            Onsets("0605", "gnash", 2, 1.4f, minGap: 1.5f);

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

        private static void Onsets(string id, string key, int count, float seconds, float minGap)
        {
            float[] d = Load(id, out int hz);
            List<int> on = PeakTimes(d, hz, 0.3f, minGap);
            // Spread the picks across the take rather than the first few.
            int made = 0;
            for (int k = 0; k < count && on.Count > 0; k++)
            {
                int start = Mathf.Max(0, on[Mathf.Min(on.Count - 1, k * on.Count / count)] - hz / 25);
                int len = Mathf.Min(d.Length - start, Mathf.RoundToInt(seconds * hz));
                Write(made == 0 ? key : key + "_" + made, Slice(d, start, len, 0.003f, seconds * 0.35f, false, hz), hz, 0);
                made++;
            }
        }

        /// <summary>Pigeon coos: loud but tonal stretches (low zero-crossing rate), not wing flaps.</summary>
        private static void Coos(string id, string key, int count, float seconds)
        {
            float[] d = Load(id, out int hz);
            int len = Mathf.RoundToInt(seconds * hz), hop = hz / 10;
            var windows = new List<(int start, float rms, float zcr)>();
            float peakRms = 0f;
            for (int s = 0; s + len < d.Length; s += hop)
            {
                double e = 0; int z = 0;
                for (int i = s; i < s + len; i++) { e += d[i] * d[i]; if (i > s && (d[i] >= 0f) != (d[i - 1] >= 0f)) z++; }
                float rms = Mathf.Sqrt((float)(e / len));
                peakRms = Mathf.Max(peakRms, rms);
                windows.Add((s, rms, z / (float)len));
            }
            windows.RemoveAll(w => w.rms < peakRms * 0.6f);
            windows.Sort((a, b) => a.zcr.CompareTo(b.zcr));
            var used = new List<int>();
            foreach (var w in windows)
            {
                if (used.Count >= count) break;
                if (used.Exists(u => Mathf.Abs(u - w.start) < len)) continue;
                used.Add(w.start);
                Write(used.Count == 1 ? key : key + "_" + (used.Count - 1), Slice(d, w.start, len, 0.05f, 0.3f, false, hz, 0.12f), hz, 0);
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
