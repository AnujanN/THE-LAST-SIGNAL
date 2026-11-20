using System.IO;
using UnityEditor;
using UnityEngine;

namespace LastSignal.EditorTools
{
    /// <summary>
    /// Mobile-friendly import settings for the game's sound files under Assets/Audio/Resources/Sounds:
    /// long ambience loops stream compressed from disk; short effects are mono and compressed in memory.
    /// </summary>
    public class AudioImportRules : AssetPostprocessor
    {
        private static readonly string[] LongSounds = { "rain", "carrain", "engine", "clock", "drips", "dawn", "wind", "interference", "hum", "generator", "threat" };

        private void OnPreprocessAudio()
        {
            if (!assetPath.Replace('\\', '/').StartsWith("Assets/Audio/Resources/Sounds/")) return;
            string name = Path.GetFileNameWithoutExtension(assetPath).ToLowerInvariant();
            bool isLong = System.Array.Exists(LongSounds, n => name == n || name.StartsWith(n + "_"));

            var importer = (AudioImporter)assetImporter;
            AudioImporterSampleSettings s = importer.defaultSampleSettings;
            s.compressionFormat = AudioCompressionFormat.Vorbis;
            s.quality = isLong ? 0.45f : 0.6f;
            s.loadType = isLong ? AudioClipLoadType.Streaming : AudioClipLoadType.CompressedInMemory;
            s.sampleRateSetting = AudioSampleRateSetting.OptimizeSampleRate;
            s.preloadAudioData = !isLong;
            importer.defaultSampleSettings = s;
            importer.forceToMono = !isLong;
            importer.loadInBackground = isLong;
        }
    }
}
