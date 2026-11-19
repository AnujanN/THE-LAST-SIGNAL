using LastSignal.Enemy;
using UnityEngine;

namespace LastSignal.Environment
{
    /// <summary>
    /// The building is not empty: every so often a mouse darts across the floor ahead, a rat gnaws inside
    /// the wall beside Alex, or a spider drops on its thread in front of his face.
    /// Events only start where they will be seen (in front of the camera, nothing in the way) and never
    /// while The Echo is chasing. Purely atmospheric: nothing here makes noise The Echo can hear.
    /// </summary>
    public class AmbientLife : MonoBehaviour
    {
        [SerializeField] private Critter mouse;
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
            int start = Random.Range(0, 3);
            for (int i = 0; i < 3; i++)
                if (Trigger((start + i) % 3)) return true;
            return false;
        }

        /// <summary>0 mouse, 1 spider, 2 rat gnawing inside a wall.</summary>
        public bool Trigger(int which)
        {
            Camera cam = Camera.main;
            if (cam == null) return false;
            bool ok = which switch
            {
                0 => TryMouse(cam.transform),
                1 => TrySpider(cam.transform),
                _ => TryGnaw(cam.transform),
            };
            if (ok) LastEvent = which switch { 0 => "Mouse", 1 => "Spider", _ => "Gnaw" };
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
            // Half the time it scurries along a table or desk top in view, if there is one.
            if (Random.value < 0.5f && TryTableRun(cam)) return true;
            return TryFloorRun(cam);
        }

        /// <summary>Along the length of a table or desk top ahead (off one end and gone).</summary>
        private bool TryTableRun(Transform cam)
        {
            Vector3 f = Flat(cam.forward);
            for (int attempt = 0; attempt < 6; attempt++)
            {
                Vector3 dir = Quaternion.AngleAxis(Random.Range(-35f, 35f), Vector3.up) * f;
                Vector3 c = cam.position + dir * Random.Range(1.5f, 4.5f);
                if (!Clear(cam.position, c)) continue;
                if (!Physics.Raycast(c, Vector3.down, out RaycastHit top, 1.6f, solid, QueryTriggerInteraction.Ignore) || top.normal.y < 0.9f) continue;
                float height = top.point.y - (cam.position.y - 1.65f);
                if (height < 0.55f || height > 1.25f) continue; // a table or desk, not the floor or a shelf
                Bounds b = top.collider.bounds;
                bool alongX = b.size.x >= b.size.z;
                float length = alongX ? b.size.x : b.size.z;
                if (length < 0.9f || length > 6f) continue;
                Vector3 axis = alongX ? Vector3.right : Vector3.forward;
                Vector3 mid = new Vector3(alongX ? b.center.x : top.point.x, top.point.y, alongX ? top.point.z : b.center.z);
                Vector3 a = mid - axis * (length / 2f - 0.08f), e = mid + axis * (length / 2f - 0.08f);
                if (!Clear(cam.position, a + Vector3.up * 0.1f) && !Clear(cam.position, e + Vector3.up * 0.1f)) continue;
                if (Random.value < 0.5f) (a, e) = (e, a);
                mouse.Run(a, e);
                return true;
            }
            return false;
        }

        private bool TryFloorRun(Transform cam)
        {
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
