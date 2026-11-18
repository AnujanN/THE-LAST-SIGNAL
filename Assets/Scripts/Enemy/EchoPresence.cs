using System.Collections.Generic;
using LastSignal.Environment;
using UnityEngine;

namespace LastSignal.Enemy
{
    /// <summary>
    /// Makes The Echo feel wrong rather than mechanical: sudden head twitches on top of the animation,
    /// eyes that burn brighter when it hunts, smoke that thickens when it is agitated, and lamps that
    /// stutter while it passes. Purely visual; it never changes what the AI can sense.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public class EchoPresence : MonoBehaviour
    {
        [SerializeField] private EchoAIController ai;
        [SerializeField] private Transform head;
        [SerializeField] private Renderer[] eyes = new Renderer[0];
        [SerializeField] private Light eyeGlow;
        [SerializeField] private ParticleSystem smoke;
        [SerializeField] private Color calmEyes = new Color(0.75f, 0.8f, 0.9f);
        [SerializeField] private Color huntingEyes = new Color(1f, 0.25f, 0.2f);

        [Header("Posture (applied on top of the animation)")]
        [Tooltip("Spine and neck bones bent forward by hunchDegrees each.")]
        [SerializeField] private Transform[] spine = new Transform[0];
        [SerializeField] private float hunchDegrees = 11f;
        [SerializeField] private float headRollDegrees = 22f;
        [SerializeField] private float headPitchDegrees = -10f;
        [Tooltip("Upper and lower arm bones, stretched along their length: arms too long for the body.")]
        [SerializeField] private Transform[] upperArms = new Transform[0];
        [SerializeField] private Transform[] lowerArms = new Transform[0];
        [SerializeField] private float upperArmStretch = 1.15f;
        [SerializeField] private float lowerArmStretch = 1.3f;

        [Header("Twitches")]
        [SerializeField] private Vector2 twitchEverySeconds = new Vector2(1.5f, 5f);
        [SerializeField] private float twitchMaxDegrees = 35f;

        [Header("Lamps")]
        [SerializeField] private float disturbRadius = 7f;
        [SerializeField, Range(0f, 1f)] private float disturbMinFactor = 0.25f;

        private static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private MaterialPropertyBlock block;

        private float twitchTimer;
        private float twitchHold;
        private Quaternion twitchOffset = Quaternion.identity;
        private Quaternion twitchCurrent = Quaternion.identity;

        private readonly List<Light> sceneLights = new List<Light>();
        private readonly Dictionary<Light, float> disturbed = new Dictionary<Light, float>();
        private float scanTimer;

        private void Awake()
        {
            if (ai == null) ai = GetComponent<EchoAIController>();
            block = new MaterialPropertyBlock();
            twitchTimer = Random.Range(twitchEverySeconds.x, twitchEverySeconds.y);
        }

        private void Start() => CollectLights();

        private void OnDisable() => RestoreAll();

        private void CollectLights()
        {
            sceneLights.Clear();
            foreach (Light l in FindObjectsByType<Light>(FindObjectsInactive.Exclude))
            {
                if (l.type != LightType.Point && l.type != LightType.Spot) continue;
                if (l.transform.IsChildOf(transform)) continue;
                // The player's flashlight is its own story (it stutters with the threat level).
                if (l.GetComponentInParent<LastSignal.Player.PlayerController>() != null) continue;
                sceneLights.Add(l);
            }
        }

        private void LateUpdate()
        {
            bool agitated = ai != null && (ai.Current == EchoAIController.State.Chase || ai.Current == EchoAIController.State.Hunt
                                           || ai.Current == EchoAIController.State.Inspect || ai.Current == EchoAIController.State.Caught);
            bool active = ai == null || ai.Current != EchoAIController.State.Inactive;
            ApplyPosture();
            UpdateTwitch(agitated);
            UpdateEyes(agitated, active);
            UpdateSmoke(agitated, active);
            UpdateLamps(active);
        }

        /// <summary>Character-relative bends, so they do not depend on each bone's local axes.</summary>
        private void ApplyPosture()
        {
            Vector3 right = transform.right, fwd = transform.forward;
            Quaternion hunch = Quaternion.AngleAxis(hunchDegrees, right);
            foreach (Transform b in spine)
                if (b != null) b.rotation = hunch * b.rotation;
            if (head != null)
                head.rotation = Quaternion.AngleAxis(headRollDegrees, fwd) * Quaternion.AngleAxis(headPitchDegrees, right) * head.rotation;
            foreach (Transform b in upperArms)
                if (b != null) b.localScale = new Vector3(1f, upperArmStretch, 1f);
            foreach (Transform b in lowerArms)
                if (b != null) b.localScale = new Vector3(1f, lowerArmStretch, 1f);
        }

        private void UpdateTwitch(bool agitated)
        {
            if (head == null) return;
            float dt = Time.deltaTime;
            twitchTimer -= dt * (agitated ? 2.5f : 1f);
            if (twitchTimer <= 0f)
            {
                twitchTimer = Random.Range(twitchEverySeconds.x, twitchEverySeconds.y);
                twitchHold = Random.Range(0.15f, 0.6f);
                float a = twitchMaxDegrees;
                twitchOffset = Quaternion.Euler(Random.Range(-a * 0.4f, a * 0.4f), Random.Range(-a, a), Random.Range(-a, a));
            }
            if (twitchHold > 0f) twitchHold -= dt;
            Quaternion target = twitchHold > 0f ? twitchOffset : Quaternion.identity;
            // Snap into the twitch almost instantly, ease out of it slowly: an unnatural, jerky motion.
            float speed = twitchHold > 0f ? 40f : 4f;
            twitchCurrent = Quaternion.Slerp(twitchCurrent, target, 1f - Mathf.Exp(-speed * dt));
            // Offset is defined in the character's frame (yaw/roll/pitch), then applied in world space.
            Quaternion frame = transform.rotation;
            head.rotation = frame * twitchCurrent * Quaternion.Inverse(frame) * head.rotation;
        }

        private void UpdateEyes(bool agitated, bool active)
        {
            Color c = agitated ? huntingEyes : calmEyes;
            float pulse = agitated ? 1.4f + 0.4f * Mathf.Sin(Time.time * 9f) : 0.8f + 0.2f * Mathf.Sin(Time.time * 1.7f);
            if (!active) pulse = 0f;
            foreach (Renderer r in eyes)
            {
                if (r == null) continue;
                r.GetPropertyBlock(block);
                block.SetColor(BaseColorId, c);
                block.SetColor(EmissionId, c * pulse);
                r.SetPropertyBlock(block);
            }
            if (eyeGlow != null)
            {
                eyeGlow.color = c;
                eyeGlow.intensity = 0.6f * pulse;
            }
        }

        private void UpdateSmoke(bool agitated, bool active)
        {
            if (smoke == null) return;
            ParticleSystem.EmissionModule e = smoke.emission;
            e.rateOverTime = !active ? 0f : agitated ? 22f : 9f;
        }

        private void UpdateLamps(bool active)
        {
            scanTimer -= Time.deltaTime;
            if (scanTimer <= 0f)
            {
                scanTimer = 0.25f;
                if (sceneLights.Count == 0) CollectLights();
                Vector3 p = transform.position;
                foreach (Light l in sceneLights)
                {
                    if (l == null) continue;
                    bool near = active && l.isActiveAndEnabled && (l.transform.position - p).sqrMagnitude < disturbRadius * disturbRadius;
                    if (near && !disturbed.ContainsKey(l))
                    {
                        FlickerLight flicker = l.GetComponent<FlickerLight>();
                        if (flicker != null) { flicker.Disturb(0.5f); continue; }
                        disturbed[l] = l.intensity;
                    }
                    else if (near)
                    {
                        FlickerLight flicker = l.GetComponent<FlickerLight>();
                        if (flicker != null) flicker.Disturb(0.5f);
                    }
                    else if (!near && disturbed.TryGetValue(l, out float baseIntensity))
                    {
                        l.intensity = baseIntensity;
                        disturbed.Remove(l);
                    }
                }
            }

            if (disturbed.Count == 0) return;
            float t = Time.time;
            foreach (KeyValuePair<Light, float> kv in disturbed)
            {
                if (kv.Key == null) continue;
                // Mostly dim and stuttering, with brief recoveries (no full-screen strobe).
                float n = Mathf.PerlinNoise(t * 11f, kv.Value * 7.3f + kv.Key.transform.position.x);
                kv.Key.intensity = kv.Value * Mathf.Lerp(disturbMinFactor, 1f, n * n);
            }
        }

        private void RestoreAll()
        {
            foreach (KeyValuePair<Light, float> kv in disturbed)
                if (kv.Key != null) kv.Key.intensity = kv.Value;
            disturbed.Clear();
        }
    }
}
