using UnityEngine;

namespace LastSignal.Environment
{
    /// <summary>
    /// A spider on a wall or the ceiling: still for a while, then a quick scuttle to a new spot near its
    /// web. A dropping spider instead lowers itself on a thread right in front of Alex's face, hangs
    /// there turning slowly, then climbs back up.
    /// </summary>
    public class Spider : MonoBehaviour
    {
        [SerializeField] private Transform body;
        [SerializeField] private Transform[] legs = new Transform[0];
        [SerializeField] private Transform thread;
        [SerializeField] private AudioSource voice;
        [SerializeField] private bool dropper;
        [SerializeField] private float wanderRadius = 0.6f;
        [SerializeField] private float scuttleSpeed = 0.35f;

        private enum Mode { Still, Scuttle, Descend, Hang, Climb }

        private Mode mode = Mode.Still;
        private Vector3 home, normal, target, anchor;
        private float timer, bottom;
        private Quaternion[] legRest;

        public bool Busy => gameObject.activeSelf;

        private void Awake()
        {
            legRest = new Quaternion[legs.Length];
            for (int i = 0; i < legs.Length; i++) legRest[i] = legs[i] != null ? legs[i].localRotation : Quaternion.identity;
            if (body == null) body = transform;
            home = body.position;
            normal = body.up;
            timer = Random.Range(1f, 6f);
            if (thread != null) thread.gameObject.SetActive(false);
        }

        /// <summary>Drop from <paramref name="ceilingPoint"/> down to <paramref name="bottomY"/> on a thread.</summary>
        public void Drop(Vector3 ceilingPoint, float bottomY)
        {
            anchor = ceilingPoint;
            bottom = Mathf.Min(bottomY, ceilingPoint.y - 0.3f);
            gameObject.SetActive(true);
            body.position = anchor;
            body.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            mode = Mode.Descend;
            if (thread != null) thread.gameObject.SetActive(true);
            if (voice != null) voice.PlayOneShot(Audio.PlaceholderClips.Skitter, 0.4f);
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            bool moving = false;
            switch (mode)
            {
                case Mode.Still:
                    timer -= dt;
                    if (timer <= 0f && !dropper)
                    {
                        Vector3 tangentA = Vector3.Cross(normal, Mathf.Abs(normal.y) > 0.9f ? Vector3.right : Vector3.up).normalized;
                        Vector3 tangentB = Vector3.Cross(normal, tangentA);
                        Vector2 r = Random.insideUnitCircle * wanderRadius;
                        target = home + tangentA * r.x + tangentB * r.y;
                        mode = Mode.Scuttle;
                        timer = 3f;
                    }
                    break;
                case Mode.Scuttle:
                    Vector3 to = target - body.position;
                    timer -= dt;
                    if (to.magnitude < 0.02f || timer <= 0f) { mode = Mode.Still; timer = Random.Range(2f, 9f); break; }
                    // Stop-start: spiders move in bursts.
                    if (Mathf.PerlinNoise(Time.time * 3f, home.x) < 0.35f) break;
                    body.position += to.normalized * Mathf.Min(to.magnitude, scuttleSpeed * dt);
                    body.rotation = Quaternion.Slerp(body.rotation, Quaternion.LookRotation(to, normal), 1f - Mathf.Exp(-12f * dt));
                    moving = true;
                    break;
                case Mode.Descend:
                    body.position = Vector3.MoveTowards(body.position, new Vector3(anchor.x, bottom, anchor.z), 0.9f * dt);
                    moving = true;
                    if (body.position.y <= bottom + 0.001f) { mode = Mode.Hang; timer = Random.Range(2f, 3.5f); }
                    break;
                case Mode.Hang:
                    timer -= dt;
                    body.Rotate(Vector3.up, 25f * dt, Space.World);
                    body.position = new Vector3(anchor.x, bottom + Mathf.Sin(Time.time * 2f) * 0.015f, anchor.z);
                    if (timer <= 0f) mode = Mode.Climb;
                    break;
                case Mode.Climb:
                    body.position = Vector3.MoveTowards(body.position, anchor, 0.45f * dt);
                    moving = true;
                    if ((body.position - anchor).sqrMagnitude < 1e-4f)
                    {
                        mode = Mode.Still;
                        if (thread != null) thread.gameObject.SetActive(false);
                        gameObject.SetActive(false);
                    }
                    break;
            }

            if (thread != null && thread.gameObject.activeSelf)
            {
                Vector3 a = anchor, b = body.position;
                thread.position = (a + b) / 2f;
                thread.rotation = Quaternion.identity;
                thread.localScale = new Vector3(0.004f, Mathf.Max(0.001f, (a.y - b.y) / 2f), 0.004f);
            }

            // Legs ripple while it moves.
            for (int i = 0; i < legs.Length; i++)
            {
                if (legs[i] == null) continue;
                float swing = moving ? Mathf.Sin(Time.time * 28f + i * 1.7f) * 22f : 0f;
                legs[i].localRotation = legRest[i] * Quaternion.Euler(0f, swing, 0f);
            }
        }
    }
}
