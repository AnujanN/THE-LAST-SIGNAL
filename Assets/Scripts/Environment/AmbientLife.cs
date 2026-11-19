using LastSignal.Enemy;
using UnityEngine;

namespace LastSignal.Environment
{
    /// <summary>
    /// The building is not empty: every so often a mouse darts across the floor ahead, a pigeon or an owl
    /// bursts from the dark over Alex's shoulder, or a spider drops on its thread in front of his face.
    /// Events only start where they will be seen (in front of the camera, nothing in the way) and never
    /// while The Echo is chasing. Purely atmospheric: nothing here makes noise The Echo can hear.
    /// </summary>
    public class AmbientLife : MonoBehaviour
    {
        [SerializeField] private Critter mouse;
        [SerializeField] private Bird pigeon;
        [SerializeField] private Bird owl;
        [SerializeField] private Spider dropSpider;
        [SerializeField] private Vector2 everySeconds = new Vector2(18f, 40f);
        [SerializeField] private float firstDelay = 14f;
        [SerializeField] private LayerMask solid = ~0;

        private float timer;
        private EchoAIController echo;

        /// <summary>Last event started (tests, debugging).</summary>
        public string LastEvent { get; private set; } = "";

        private void Start()
        {
            timer = firstDelay;
            echo = FindFirstObjectByType<EchoAIController>();
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            timer -= dt;
            if (timer > 0f) return;
            timer = Random.Range(everySeconds.x, everySeconds.y);
            if (echo != null && (echo.Current == EchoAIController.State.Chase || echo.Current == EchoAIController.State.Hunt
                                 || echo.Current == EchoAIController.State.Caught)) return;
            TriggerAny();
        }

        /// <summary>Try the events in a random order until one fits where Alex is looking.</summary>
        public bool TriggerAny()
        {
            int start = Random.Range(0, 5);
            for (int i = 0; i < 5; i++)
                if (Trigger((start + i) % 5)) return true;
            return false;
        }

        /// <summary>0 mouse, 1 pigeon, 2 owl, 3 spider, 4 rat gnawing inside a wall.</summary>
        public bool Trigger(int which)
        {
            Camera cam = Camera.main;
            if (cam == null) return false;
            bool ok = which switch
            {
                0 => TryMouse(cam.transform),
                1 => TryBird(pigeon, cam.transform, 0.25f),
                2 => TryBird(owl, cam.transform, 1.4f),
                3 => TrySpider(cam.transform),
                _ => TryGnaw(cam.transform),
            };
            if (ok) LastEvent = which switch { 0 => "Mouse", 1 => "Pigeon", 2 => "Owl", 3 => "Spider", _ => "Gnaw" };
            return ok;
        }

        private static Vector3 Flat(Vector3 v)
        {
            v.y = 0f;
            return v.sqrMagnitude > 1e-6f ? v.normalized : Vector3.forward;
        }

        private bool Clear(Vector3 a, Vector3 b) => !Physics.Linecast(a, b, solid, QueryTriggerInteraction.Ignore);

        private bool TryMouse(Transform cam)
        {
            if (mouse == null || mouse.Busy) return false;
            Vector3 f = Flat(cam.forward), r = Vector3.Cross(Vector3.up, f);
            Vector3 c = cam.position + f * Random.Range(2.5f, 5f);
            if (!Clear(cam.position, c)) return false;
            if (!Physics.Raycast(c, Vector3.down, out RaycastHit floor, 3f, solid, QueryTriggerInteraction.Ignore) || floor.normal.y < 0.9f
                || floor.point.y > cam.position.y - 1.2f) return false; // a floor, not a table top
            Vector3 low = floor.point + Vector3.up * 0.08f;
            if (!Clear(cam.position, low)) return false;
            float left = Physics.Raycast(low, -r, out RaycastHit hl, 3.5f, solid, QueryTriggerInteraction.Ignore) ? hl.distance - 0.12f : 3.5f;
            float right = Physics.Raycast(low, r, out RaycastHit hr, 3.5f, solid, QueryTriggerInteraction.Ignore) ? hr.distance - 0.12f : 3.5f;
            if (left + right < 1.3f) return false;
            Vector3 a = floor.point - r * left, b = floor.point + r * right;
            if (Random.value < 0.5f) (a, b) = (b, a);
            mouse.Run(a, b);
            return true;
        }

        private bool TryBird(Bird bird, Transform cam, float delay)
        {
            if (bird == null || bird.Busy) return false;
            Vector3 f = Flat(cam.forward), r = Vector3.Cross(Vector3.up, f);
            Vector3 dir = Quaternion.AngleAxis(Random.Range(-25f, 25f), Vector3.up) * f;
            Vector3 c = cam.position + dir * Random.Range(3f, 6f);
            if (!Clear(cam.position, c)) return false;
            float ceiling = Physics.Raycast(c, Vector3.up, out RaycastHit hc, 4f, solid, QueryTriggerInteraction.Ignore) ? hc.point.y : cam.position.y + 1.6f;
            Vector3 perch = new Vector3(c.x, Mathf.Min(ceiling - 0.35f, cam.position.y + 1.3f), c.z);
            if (perch.y < cam.position.y + 0.3f || !Clear(cam.position, perch)) return false;

            // Over Alex's shoulder, then away behind him.
            float side = Random.value < 0.5f ? -1f : 1f;
            float overhead = Physics.Raycast(cam.position, Vector3.up, out RaycastHit ho, 3f, solid, QueryTriggerInteraction.Ignore) ? ho.distance - 0.3f : 0.6f;
            Vector3 via = cam.position + r * (side * 0.7f) + Vector3.up * Mathf.Clamp(overhead, 0.2f, 0.6f);
            Vector3 back = cam.position + Vector3.up * 0.35f;
            float behind = Physics.Raycast(back, -f, out RaycastHit hb, 7f, solid, QueryTriggerInteraction.Ignore) ? hb.distance - 0.6f : 7f;
            Vector3 exit;
            if (behind >= 1.5f) exit = back - f * behind + r * (side * 0.4f);
            else
            {
                // Nowhere to go behind: veer off sideways instead.
                float away = Physics.Raycast(back, r * side, out RaycastHit hs, 6f, solid, QueryTriggerInteraction.Ignore) ? hs.distance - 0.6f : 6f;
                if (away < 1.5f) return false;
                exit = back + r * (side * away);
            }
            if (!Clear(perch, via) || !Clear(via, exit)) return false;
            bird.Launch(perch, via, exit, delay);
            return true;
        }

        /// <summary>Something gnawing inside the nearest wall to one side.</summary>
        private bool TryGnaw(Transform cam)
        {
            AudioClip gnaw = Audio.PlaceholderClips.Gnaw;
            if (gnaw == null || Audio.AudioManager.Instance == null) return false;
            Vector3 side = Vector3.Cross(Vector3.up, Flat(cam.forward)) * (Random.value < 0.5f ? -1f : 1f);
            Vector3 low = cam.position - Vector3.up * 1.2f;
            if (!Physics.Raycast(low, side, out RaycastHit wall, 3f, solid, QueryTriggerInteraction.Ignore) || Mathf.Abs(wall.normal.y) > 0.3f) return false;
            Audio.AudioManager.Instance.PlayAt(gnaw, wall.point - wall.normal * 0.1f, 0.8f);
            return true;
        }

        private bool TrySpider(Transform cam)
        {
            if (dropSpider == null || dropSpider.Busy) return false;
            Vector3 f = Flat(cam.forward), r = Vector3.Cross(Vector3.up, f);
            Vector3 c = cam.position + f * Random.Range(1.4f, 2.2f) + r * Random.Range(-0.4f, 0.4f);
            if (!Clear(cam.position, c)) return false;
            if (!Physics.Raycast(c, Vector3.up, out RaycastHit hc, 3f, solid, QueryTriggerInteraction.Ignore)) return false;
            dropSpider.Drop(hc.point, cam.position.y - Random.Range(0f, 0.2f));
            return true;
        }
    }
}
