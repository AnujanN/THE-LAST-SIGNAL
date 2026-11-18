using UnityEngine;

namespace LastSignal.Environment
{
    /// <summary>
    /// An owl or a pigeon that got inside: perched high in the dark, then it bursts off in a clatter of
    /// wings, over Alex's shoulder and away. The owl hoots first and its eyes catch the light.
    /// </summary>
    public class Bird : MonoBehaviour
    {
        public enum Kind { Pigeon, Owl }

        [SerializeField] private Kind kind;
        [SerializeField] private Transform leftWing;
        [SerializeField] private Transform rightWing;
        [SerializeField] private Transform head;
        [SerializeField] private AudioSource voice;
        [SerializeField] private float flapsPerSecond = 8f;
        [SerializeField] private float flapDegrees = 65f;
        [SerializeField] private float flightSpeed = 6f;

        private Vector3 p0, p1, p2;
        private float perchTimer, t, duration;
        private bool flying;

        public Kind Type => kind;
        public bool Busy => gameObject.activeSelf;

        /// <summary>Perch at <paramref name="perch"/> for <paramref name="delay"/> s, then fly through via to exit.</summary>
        public void Launch(Vector3 perch, Vector3 via, Vector3 exit, float delay)
        {
            p0 = perch;
            p2 = exit;
            p1 = 2f * via - 0.5f * (perch + exit); // quadratic curve that passes through "via" at its middle
            float length = Vector3.Distance(perch, via) + Vector3.Distance(via, exit);
            duration = Mathf.Max(0.6f, length / flightSpeed);
            t = 0f;
            perchTimer = delay;
            flying = false;
            transform.position = perch;
            Vector3 look = via - perch;
            look.y = 0f;
            transform.rotation = Quaternion.LookRotation(look.sqrMagnitude > 1e-4f ? look : Vector3.forward);
            Fold(0f);
            gameObject.SetActive(true);
            if (voice != null && kind == Kind.Owl) voice.PlayOneShot(Audio.PlaceholderClips.Hoot, 1f);
            else if (voice != null && Random.value < 0.5f) voice.PlayOneShot(Audio.PlaceholderClips.Coo, 0.8f);
        }

        private void Fold(float flap)
        {
            if (leftWing != null) leftWing.localRotation = Quaternion.Euler(0f, 0f, flap);
            if (rightWing != null) rightWing.localRotation = Quaternion.Euler(0f, 0f, -flap);
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            if (!flying)
            {
                perchTimer -= dt;
                // Folded wings, small jerky head turns.
                Fold(-70f);
                if (head != null) head.localRotation = Quaternion.Euler(0f, Mathf.Round(Mathf.Sin(Time.time * 1.7f) * 3f) * 20f, 0f);
                if (perchTimer > 0f) return;
                flying = true;
                if (voice != null)
                {
                    voice.pitch = kind == Kind.Owl ? 0.8f : 1.1f;
                    voice.PlayOneShot(Audio.PlaceholderClips.Flutter, 1f);
                    voice.pitch = 1f;
                }
            }

            t += dt / duration;
            if (t >= 1f) { gameObject.SetActive(false); return; }
            float u = 1f - t;
            Vector3 pos = u * u * p0 + 2f * u * t * p1 + t * t * p2;
            Vector3 tangent = 2f * u * (p1 - p0) + 2f * t * (p2 - p1);
            transform.position = pos;
            if (tangent.sqrMagnitude > 1e-4f)
            {
                // Bank into the turn.
                Vector3 flat = new Vector3(tangent.x, 0f, tangent.z);
                float bank = flat.sqrMagnitude > 1e-4f ? Vector3.SignedAngle(transform.forward, flat, Vector3.up) * 2f : 0f;
                transform.rotation = Quaternion.LookRotation(tangent) * Quaternion.Euler(0f, 0f, -Mathf.Clamp(bank, -40f, 40f));
            }
            if (head != null) head.localRotation = Quaternion.identity;
            Fold(Mathf.Sin(Time.time * flapsPerSecond * Mathf.PI * 2f) * flapDegrees);
        }
    }
}
