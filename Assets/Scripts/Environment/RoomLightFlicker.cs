using System.Collections.Generic;
using UnityEngine;

namespace LastSignal.Environment
{
    /// <summary>
    /// Bad wiring everywhere: every hall and room light has its own random timer, and when it comes round
    /// (and Alex is near enough to see it) that light stutters for a few seconds: dips, brief blackouts,
    /// starter clicks, then it settles. At most two at a time. Lights that already flicker on their own,
    /// red emergency lamps, the flashlight and lamps The Echo is disturbing are left alone. Local and short:
    /// no full-screen strobing.
    /// </summary>
    public class RoomLightFlicker : MonoBehaviour
    {
        [Tooltip("Each light flickers about this often (seconds between its turns).")]
        [SerializeField] private Vector2 everySeconds = new Vector2(14f, 40f);
        [SerializeField] private Vector2 flickerSeconds = new Vector2(1.5f, 4.5f);
        [SerializeField] private float nearDistance = 22f;
        [SerializeField] private int maxAtOnce = 2;
        [SerializeField, Range(0f, 1f)] private float lowest = 0.05f;

        private class Lamp
        {
            public Light light;
            public float baseIntensity, next, left, step, level = 1f;
            public bool active;
        }

        private readonly List<Lamp> lamps = new List<Lamp>();
        private Transform echo;
        private int activeCount;

        /// <summary>How many lights take part, and how many are flickering right now (tests, debugging).</summary>
        public int LightCount => lamps.Count;
        public int Flickering => activeCount;

        private void Start()
        {
            foreach (Light l in GetComponentsInChildren<Light>(true))
            {
                if (l.type != LightType.Point && l.type != LightType.Spot) continue;
                if (l.GetComponent<FlickerLight>() != null || l.GetComponent<DistantFlicker>() != null) continue;
                if (l.color.g < l.color.r * 0.4f) continue; // red emergency lamps keep burning
                lamps.Add(new Lamp { light = l, next = Random.Range(3f, everySeconds.y) });
            }
            Enemy.EchoAIController e = FindFirstObjectByType<Enemy.EchoAIController>();
            if (e != null) echo = e.transform;
        }

        private void OnDisable()
        {
            foreach (Lamp lamp in lamps) Stop(lamp);
        }

        /// <summary>Start a flicker on the nearest lit light now (scares, tests). False if none qualifies.</summary>
        public bool FlickerNow()
        {
            Camera cam = Camera.main;
            if (cam == null) return false;
            Lamp best = null;
            float bestD = nearDistance * nearDistance;
            foreach (Lamp lamp in lamps)
            {
                if (lamp.active || !Usable(lamp)) continue;
                float d = (lamp.light.transform.position - cam.transform.position).sqrMagnitude;
                if (d < bestD) { bestD = d; best = lamp; }
            }
            if (best == null) return false;
            Begin(best);
            return true;
        }

        private bool Usable(Lamp lamp)
        {
            Light l = lamp.light;
            if (l == null || !l.isActiveAndEnabled || l.intensity <= 0.01f) return false;
            // The Echo already makes the lamps near it stutter; leave those to it.
            return echo == null || (l.transform.position - echo.position).sqrMagnitude >= 81f;
        }

        private void Begin(Lamp lamp)
        {
            lamp.active = true;
            lamp.baseIntensity = lamp.light.intensity;
            lamp.left = Random.Range(flickerSeconds.x, flickerSeconds.y);
            lamp.step = 0f;
            activeCount++;
            Audio.AudioManager.Instance?.PlayAt(Audio.PlaceholderClips.TubeFlick, lamp.light.transform.position, 0.5f);
        }

        private void Stop(Lamp lamp)
        {
            if (!lamp.active) return;
            if (lamp.light != null) lamp.light.intensity = lamp.baseIntensity;
            lamp.active = false;
            lamp.next = Random.Range(everySeconds.x, everySeconds.y);
            activeCount--;
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            Camera cam = Camera.main;
            Vector3 eye = cam != null ? cam.transform.position : transform.position;
            float near = nearDistance * nearDistance;

            foreach (Lamp lamp in lamps)
            {
                if (lamp.active)
                {
                    if (lamp.light == null || !lamp.light.isActiveAndEnabled) { Stop(lamp); continue; } // switched off by the story
                    lamp.left -= dt;
                    if (lamp.left <= 0f) { Stop(lamp); continue; }
                    lamp.step -= dt;
                    if (lamp.step <= 0f)
                    {
                        // Mostly dips and short blackouts, now and then a full-strength flash back on.
                        float r = Random.value;
                        lamp.level = r < 0.3f ? lowest : r < 0.75f ? Random.Range(0.25f, 0.7f) : 1f;
                        lamp.step = lamp.level <= lowest ? Random.Range(0.06f, 0.25f) : Random.Range(0.05f, 0.18f);
                        if (lamp.level >= 1f && Random.value < 0.4f)
                            Audio.AudioManager.Instance?.PlayAt(Audio.PlaceholderClips.TubeFlick, lamp.light.transform.position, 0.35f);
                    }
                    lamp.light.intensity = lamp.baseIntensity * lamp.level;
                    continue;
                }

                lamp.next -= dt;
                if (lamp.next > 0f) continue;
                // Its turn: flicker if Alex can see it and not too many are going already; otherwise try again soon.
                bool seen = (lamp.light != null) && (lamp.light.transform.position - eye).sqrMagnitude < near;
                if (seen && activeCount < maxAtOnce && Usable(lamp)) Begin(lamp);
                else lamp.next = Random.Range(3f, 8f);
            }
        }
    }
}
