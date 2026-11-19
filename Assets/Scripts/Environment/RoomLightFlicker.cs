using System.Collections.Generic;
using UnityEngine;

namespace LastSignal.Environment
{
    /// <summary>
    /// Bad wiring: every so often one of the hall or room lights near Alex stutters for a few seconds
    /// (dips, brief blackouts, a buzz) and then settles again. Lights that already flicker on their own,
    /// the flashlight and The Echo's eyes are left alone. Local and short: no full-screen strobing.
    /// </summary>
    public class RoomLightFlicker : MonoBehaviour
    {
        [SerializeField] private Vector2 everySeconds = new Vector2(12f, 28f);
        [SerializeField] private Vector2 flickerSeconds = new Vector2(1.5f, 4.5f);
        [SerializeField] private float nearDistance = 16f;
        [SerializeField, Range(0f, 1f)] private float lowest = 0.05f;

        private readonly List<Light> candidates = new List<Light>();
        private Light current;
        private Transform echo;
        private float baseIntensity, timer, flickerLeft, stepTimer, level = 1f;

        /// <summary>The light flickering right now (tests, debugging).</summary>
        public Light Current => current;

        private void Start()
        {
            foreach (Light l in GetComponentsInChildren<Light>(true))
            {
                if (l.type != LightType.Point && l.type != LightType.Spot) continue;
                if (l.GetComponent<FlickerLight>() != null || l.GetComponent<DistantFlicker>() != null) continue;
                if (l.color.g < l.color.r * 0.4f) continue; // red emergency lamps keep burning
                candidates.Add(l);
            }
            timer = Random.Range(everySeconds.x * 0.5f, everySeconds.y * 0.5f);
            Enemy.EchoAIController e = FindFirstObjectByType<Enemy.EchoAIController>();
            if (e != null) echo = e.transform;
        }

        private void OnDisable() => Stop();

        /// <summary>Start a flicker on a lit light near the camera now (scares, tests).</summary>
        public bool FlickerNow()
        {
            Camera cam = Camera.main;
            if (cam == null || current != null) return false;
            Light pick = null;
            float best = nearDistance * nearDistance;
            int seen = 0;
            foreach (Light l in candidates)
            {
                if (l == null || !l.isActiveAndEnabled || l.intensity <= 0.01f) continue;
                float d = (l.transform.position - cam.transform.position).sqrMagnitude;
                if (d > best) continue;
                // The Echo already makes the lamps near it stutter; leave those to it.
                if (echo != null && (l.transform.position - echo.position).sqrMagnitude < 81f) continue;
                // Random pick among the near ones (reservoir sampling, no allocations).
                seen++;
                if (Random.Range(0, seen) == 0) pick = l;
            }
            if (pick == null) return false;
            current = pick;
            baseIntensity = pick.intensity;
            flickerLeft = Random.Range(flickerSeconds.x, flickerSeconds.y);
            stepTimer = 0f;
            Audio.AudioManager.Instance?.PlayAt(Audio.PlaceholderClips.TubeFlick, pick.transform.position, 0.5f);
            return true;
        }

        private void Stop()
        {
            if (current != null) current.intensity = baseIntensity;
            current = null;
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            if (current == null)
            {
                timer -= dt;
                if (timer > 0f) return;
                timer = Random.Range(everySeconds.x, everySeconds.y);
                FlickerNow();
                return;
            }
            if (!current.isActiveAndEnabled) { Stop(); return; } // switched off by the story or the power
            flickerLeft -= dt;
            if (flickerLeft <= 0f) { Stop(); return; }
            stepTimer -= dt;
            if (stepTimer <= 0f)
            {
                // Mostly dips and short blackouts, now and then a full-strength flash back on.
                float r = Random.value;
                level = r < 0.3f ? lowest : r < 0.75f ? Random.Range(0.25f, 0.7f) : 1f;
                stepTimer = level <= lowest ? Random.Range(0.06f, 0.25f) : Random.Range(0.05f, 0.18f);
                if (level >= 1f && Random.value < 0.4f)
                    Audio.AudioManager.Instance?.PlayAt(Audio.PlaceholderClips.TubeFlick, current.transform.position, 0.35f);
            }
            current.intensity = baseIntensity * level;
        }
    }
}
