using System.Collections.Generic;
using UnityEngine;

namespace LastSignal.Audio
{
    /// <summary>
    /// Real recorded sounds, loaded by name from Assets/Audio/Resources/Sounds/.
    /// A sound can have variations (footstep, footstep_1, footstep_2 ...); Pick returns a random one
    /// so repeated sounds don't feel mechanical. Returns null when no file exists, so callers can fall
    /// back to a generated placeholder.
    /// </summary>
    public static class SoundBank
    {
        private const string Folder = "Sounds/";
        private const int MaxVariations = 9;
        private static readonly Dictionary<string, AudioClip[]> cache = new Dictionary<string, AudioClip[]>();
        private static int lastIndex = -1;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => cache.Clear();

        /// <summary>Random variation of a sound, avoiding an immediate repeat; null if none exist.</summary>
        public static AudioClip Pick(string name)
        {
            AudioClip[] set = Load(name);
            if (set.Length == 0) return null;
            if (set.Length == 1) return set[0];
            int i = Random.Range(0, set.Length);
            if (i == lastIndex) i = (i + 1) % set.Length;
            lastIndex = i;
            return set[i];
        }

        public static bool Has(string name) => Load(name).Length > 0;

        private static AudioClip[] Load(string name)
        {
            if (cache.TryGetValue(name, out AudioClip[] set) && (set.Length == 0 || set[0] != null)) return set;
            var list = new List<AudioClip>();
            AudioClip first = Resources.Load<AudioClip>(Folder + name);
            if (first != null) list.Add(first);
            for (int v = 1; v <= MaxVariations; v++)
            {
                AudioClip c = Resources.Load<AudioClip>(Folder + name + "_" + v);
                if (c != null) list.Add(c);
            }
            set = list.ToArray();
            cache[name] = set;
            return set;
        }
    }
}
