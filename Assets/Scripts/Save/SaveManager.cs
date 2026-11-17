using System;
using System.IO;
using UnityEngine;

namespace LastSignal.Save
{
    /// <summary>Local JSON save file for checkpoint progress. No cloud or network.</summary>
    public static class SaveManager
    {
        private const string FileName = "lastsignal_save.json";

        public static string PathOnDisk => System.IO.Path.Combine(Application.persistentDataPath, FileName);

        public static bool HasSave
        {
            get
            {
                try { return File.Exists(PathOnDisk); }
                catch (Exception) { return false; }
            }
        }

        public static void Save(GameState state)
        {
            if (state == null) return;
            try { File.WriteAllText(PathOnDisk, JsonUtility.ToJson(state)); }
            catch (Exception e) { Debug.LogWarning("[SaveManager] Could not save: " + e.Message); }
        }

        public static bool TryLoad(out GameState state)
        {
            state = null;
            try
            {
                if (!File.Exists(PathOnDisk)) return false;
                state = JsonUtility.FromJson<GameState>(File.ReadAllText(PathOnDisk));
                return state != null && !string.IsNullOrEmpty(state.scene);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[SaveManager] Could not load: " + e.Message);
                return false;
            }
        }

        public static void Delete()
        {
            try { if (File.Exists(PathOnDisk)) File.Delete(PathOnDisk); }
            catch (Exception e) { Debug.LogWarning("[SaveManager] Could not delete: " + e.Message); }
        }
    }
}
