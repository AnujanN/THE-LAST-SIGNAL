using System.Collections;
using LastSignal.Audio;
using LastSignal.Enemy;
using LastSignal.GameFlow;
using LastSignal.Interaction;
using UnityEngine;

namespace LastSignal.Environment
{
    /// <summary>
    /// A one-shot scripted scare, triggered by a zone, a pickup or a puzzle: lamps bursting, something
    /// falling, a door slamming, footsteps crossing the floor above, a whisper. Anything loud also makes
    /// noise The Echo can hear, so a scare can turn into a real threat. Plays once per playthrough.
    /// </summary>
    public class ScareEvent : MonoBehaviour
    {
        [SerializeField] private string id = "scare";
        [SerializeField, Min(0f)] private float delay;

        [Header("Lamps that burst (go dark for good)")]
        [SerializeField] private Light[] burstLights = new Light[0];

        [Header("Objects that fall")]
        [SerializeField] private Rigidbody[] dropObjects = new Rigidbody[0];
        [SerializeField] private Vector3 dropImpulse = new Vector3(0f, 0f, 1.2f);

        [Header("Doors that slam shut")]
        [SerializeField] private Door[] slamDoors = new Door[0];

        [Header("Footsteps crossing (e.g. the floor above)")]
        [SerializeField] private Transform[] footstepPath = new Transform[0];
        [SerializeField] private float footstepInterval = 0.45f;
        [SerializeField, Range(0f, 1f)] private float footstepVolume = 0.8f;

        [Header("Sound and voice")]
        [Tooltip("Optional whisper/subtitle, packed as SPEAKER|text|seconds.")]
        [SerializeField] private string subtitle = "";
        [SerializeField] private bool playStatic;
        [Tooltip("Noise radius at this object (0 = silent to The Echo).")]
        [SerializeField] private float noiseRadius;

        private string Flag => "scare:" + id;

        public void Play()
        {
            if (WorldFlags.Has(Flag)) return;
            WorldFlags.Set(Flag);
            StartCoroutine(Run());
        }

        private IEnumerator Run()
        {
            if (delay > 0f) yield return new WaitForSeconds(delay);

            foreach (Light l in burstLights)
            {
                if (l == null || !l.enabled) continue;
                AudioManager.Instance?.PlayAt(PlaceholderClips.Spark, l.transform.position, 1f);
                l.intensity *= 2.5f;
                yield return new WaitForSeconds(0.06f);
                l.enabled = false;
            }

            foreach (Rigidbody rb in dropObjects)
            {
                if (rb == null) continue;
                rb.isKinematic = false;
                rb.AddForce(rb.transform.TransformDirection(dropImpulse), ForceMode.Impulse);
                StartCoroutine(ImpactSound(rb));
            }

            foreach (Door d in slamDoors)
                if (d != null) d.Slam();

            if (playStatic) AudioManager.Instance?.PlayUI(PlaceholderClips.StaticBurst, 0.45f);
            if (!string.IsNullOrEmpty(subtitle)) SubtitleManager.Instance?.SayPacked(subtitle);
            if (noiseRadius > 0f) NoiseSystem.Emit(transform.position, noiseRadius);

            foreach (Transform step in footstepPath)
            {
                if (step == null) continue;
                AudioManager.Instance?.PlayAt(PlaceholderClips.Footstep, step.position, footstepVolume);
                yield return new WaitForSeconds(footstepInterval * Random.Range(0.85f, 1.2f));
            }
        }

        private static IEnumerator ImpactSound(Rigidbody rb)
        {
            // Wait for the object to hit something, then a heavy thud where it landed.
            float end = Time.time + 3f;
            yield return new WaitForSeconds(0.15f);
            while (rb != null && Time.time < end && rb.linearVelocity.y < -0.3f) yield return null;
            if (rb != null) AudioManager.Instance?.PlayAt(PlaceholderClips.Thud, rb.position, 1f);
        }
    }
}
