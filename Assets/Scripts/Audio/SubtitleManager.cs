using System;
using System.Collections;
using System.Collections.Generic;
using LastSignal.Save;
using UnityEngine;

namespace LastSignal.Audio
{
    [Serializable]
    public struct SubtitleLine
    {
        public string speaker;
        [TextArea(1, 3)] public string text;
        [Min(0.5f)] public float seconds;

        public SubtitleLine(string speaker, string text, float seconds)
        {
            this.speaker = speaker;
            this.text = text;
            this.seconds = seconds;
        }
    }

    /// <summary>
    /// Queues and times subtitle lines. Story-critical lines always show; optional lines
    /// follow the Subtitles setting. Radio/recording speakers get a burst of static.
    /// </summary>
    public class SubtitleManager : MonoBehaviour
    {
        private struct Request
        {
            public SubtitleLine[] lines;
            public Action onComplete;
            public bool critical;
        }

        public static SubtitleManager Instance { get; private set; }

        private readonly Queue<Request> queue = new Queue<Request>();
        private Coroutine runner;

        public bool IsPlaying => runner != null;
        public event Action<SubtitleLine> LineStarted;
        public event Action LineEnded;

        private void Awake() => Instance = this;

        public void Play(SubtitleLine[] lines, Action onComplete = null, bool critical = true)
        {
            if (lines == null || lines.Length == 0) { onComplete?.Invoke(); return; }
            queue.Enqueue(new Request { lines = lines, onComplete = onComplete, critical = critical });
            if (runner == null) runner = StartCoroutine(Run());
        }

        public void Say(string speaker, string text, float seconds = 3f, bool critical = true) =>
            Play(new[] { new SubtitleLine(speaker, text, seconds) }, null, critical);

        /// <summary>"Speaker|Text|seconds" form for UnityEvents with one string argument.</summary>
        public void SayPacked(string packed)
        {
            if (string.IsNullOrEmpty(packed)) return;
            string[] p = packed.Split('|');
            float seconds = p.Length > 2 && float.TryParse(p[2], System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out float s) ? s : 3f;
            Say(p.Length > 1 ? p[0] : "", p.Length > 1 ? p[1] : p[0], seconds);
        }

        public void StopAll()
        {
            queue.Clear();
            if (runner != null) StopCoroutine(runner);
            runner = null;
            LineEnded?.Invoke();
        }

        private IEnumerator Run()
        {
            while (queue.Count > 0)
            {
                Request r = queue.Dequeue();
                bool show = r.critical || GameSettings.Subtitles;
                foreach (SubtitleLine line in r.lines)
                {
                    if (IsRadioSpeaker(line.speaker)) AudioManager.Instance?.PlayUI(PlaceholderClips.StaticBurst, 0.35f);
                    if (show) LineStarted?.Invoke(line);
                    yield return new WaitForSeconds(Mathf.Max(0.5f, line.seconds));
                    if (show) LineEnded?.Invoke();
                    yield return new WaitForSeconds(0.15f);
                }
                r.onComplete?.Invoke();
            }
            runner = null;
        }

        public static bool IsRadioSpeaker(string speaker)
        {
            if (string.IsNullOrEmpty(speaker)) return false;
            string s = speaker.ToUpperInvariant();
            return s.Contains("RADIO") || s.Contains("TAPE") || s.Contains("ECHO") || s.Contains("RECORDING");
        }
    }
}
