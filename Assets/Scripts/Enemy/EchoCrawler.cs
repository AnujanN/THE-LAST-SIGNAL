using LastSignal.Player;
using UnityEngine;
using UnityEngine.AI;

namespace LastSignal.Enemy
{
    /// <summary>
    /// Makes The Echo crawl: on all fours, along the walls and upside-down across the ceiling, with the
    /// limbs planting and stepping in a diagonal gait (two-bone IK), elbows splayed like a spider and the
    /// head twisted round to watch Alex. Purely visual: the agent still walks the floor NavMesh and the AI
    /// senses from its own eye point, so crawling never changes what it can see, hear or reach.
    /// It drops to the floor (with a thud) the moment it starts a chase.
    /// </summary>
    [DefaultExecutionOrder(120)]
    public class EchoCrawler : MonoBehaviour
    {
        public enum Surface { Stand, Floor, Ceiling, Wall }

        [SerializeField] private EchoAIController ai;
        [SerializeField] private Animator animator;
        [SerializeField] private Transform hips;
        [SerializeField] private Transform head;
        [Tooltip("Spine bones arched back while crawling (so the chest clears the surface).")]
        [SerializeField] private Transform[] spine = new Transform[0];
        [SerializeField] private Transform[] leftArm = new Transform[3];
        [SerializeField] private Transform[] rightArm = new Transform[3];
        [SerializeField] private Transform[] leftLeg = new Transform[3];
        [SerializeField] private Transform[] rightLeg = new Transform[3];
        [Tooltip("Follows the body while it crawls (smoke).")]
        [SerializeField] private Transform carried;
        [SerializeField] private AudioSource voice;
        [SerializeField] private LayerMask surfaceMask = ~0;

        [Header("When")]
        [SerializeField] private Vector2 decideEverySeconds = new Vector2(4f, 9f);
        [SerializeField, Range(0f, 1f)] private float ceilingChance = 0.4f;
        [SerializeField, Range(0f, 1f)] private float wallChance = 0.25f;
        [SerializeField, Range(0f, 1f)] private float floorChance = 0.15f;

        [Header("Surfaces")]
        [SerializeField] private float minCeiling = 1.9f;
        [SerializeField] private float maxCeiling = 5f;
        [SerializeField] private float wallReach = 1.5f;
        [SerializeField] private float wallHeight = 1.45f;
        [SerializeField] private float clearance = 0.36f;
        [SerializeField] private float climbSpeed = 2.4f;
        [SerializeField] private float dropSpeed = 6f;

        [Header("Gait")]
        [SerializeField] private float stride = 0.5f;
        [SerializeField] private float stepSeconds = 0.2f;
        [SerializeField] private float stepLift = 0.16f;
        [SerializeField] private float watchDistance = 10f;

        private struct Limb
        {
            public Vector3 planted, from, to;
            public float t;
            public bool stepping;
        }

        private NavMeshAgent agent;
        private Transform body;
        private Vector3 bodyLocalPos;
        private Quaternion bodyLocalRot;
        private Vector3 carriedLocalPos;
        private Quaternion headBind;
        private Vector3 headFaceLocal, headUpLocal;
        private readonly Limb[] limbs = new Limb[4];

        private Surface wanted = Surface.Stand, surface = Surface.Stand;
        private float weight;
        private float decideTimer;
        private float lostTimer;
        private float wallSide = 1f;
        private Vector3 planePoint, planeNormal = Vector3.up;
        private bool planeValid, limbsPlanted;
        private float skitterTimer, crackTimer = 1f, gnashTimer = 5f;

        /// <summary>0 standing .. 1 fully crawling.</summary>
        public float Weight => weight;
        public Surface Current => weight > 0.01f ? surface : Surface.Stand;
        public Vector3 SurfaceNormal => planeNormal;

        private void Awake()
        {
            if (ai == null) ai = GetComponent<EchoAIController>();
            agent = GetComponent<NavMeshAgent>();
            if (animator != null) body = animator.transform;
            if (body != null) { bodyLocalPos = body.localPosition; bodyLocalRot = body.localRotation; }
            if (carried != null) carriedLocalPos = carried.localPosition;
            if (head != null && body != null)
            {
                // Which way the face and the top of the head point, in the head bone's own space.
                headFaceLocal = Quaternion.Inverse(head.rotation) * body.forward;
                headUpLocal = Quaternion.Inverse(head.rotation) * body.up;
                headBind = Quaternion.Inverse(Quaternion.LookRotation(headFaceLocal, headUpLocal));
            }
            decideTimer = Random.Range(decideEverySeconds.x * 0.5f, decideEverySeconds.y * 0.5f);
        }

        private void OnDisable()
        {
            weight = 0f;
            surface = wanted = Surface.Stand;
            if (body != null) { body.localPosition = bodyLocalPos; body.localRotation = bodyLocalRot; }
            if (carried != null) carried.localPosition = carriedLocalPos;
        }

        /// <summary>Send it onto a surface now (scares, tests). Stand drops it back to its feet.</summary>
        public void Crawl(Surface s)
        {
            wanted = s;
            decideTimer = Random.Range(decideEverySeconds.x, decideEverySeconds.y);
        }

        private bool Allowed()
        {
            if (ai == null) return true;
            switch (ai.Current)
            {
                case EchoAIController.State.Patrol:
                case EchoAIController.State.Investigate:
                case EchoAIController.State.Search:
                case EchoAIController.State.Return:
                case EchoAIController.State.Listen:
                    return true;
                default:
                    return false;
            }
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f || body == null) return;

            bool allowed = Allowed();
            if (!allowed && wanted != Surface.Stand)
            {
                // Chase, catch, inspect: down on its feet, fast.
                if (weight > 0.5f && surface != Surface.Floor && voice != null && ai != null && ai.Current == EchoAIController.State.Chase)
                {
                    voice.PlayOneShot(Audio.PlaceholderClips.Thud, 1f);
                    CameraMotion.Shake(0.25f, 0.25f);
                }
                wanted = Surface.Stand;
            }

            decideTimer -= dt;
            if (decideTimer <= 0f)
            {
                decideTimer = Random.Range(decideEverySeconds.x, decideEverySeconds.y);
                if (allowed)
                {
                    float r = Random.value;
                    wanted = r < ceilingChance ? Surface.Ceiling
                        : r < ceilingChance + wallChance ? Surface.Wall
                        : r < ceilingChance + wallChance + floorChance ? Surface.Floor
                        : Surface.Stand;
                    if (wanted == Surface.Wall) wallSide = Random.value < 0.5f ? -1f : 1f;
                }
            }

            // Find the surface under (above, beside) the agent.
            Surface target = wanted;
            Vector3 point = planePoint, normal = planeNormal;
            bool found = target != Surface.Stand && Probe(target, out point, out normal);
            if (target != Surface.Stand && !found)
            {
                // Doorways and open rooms: hold on to the last surface briefly, then fall back to the floor.
                lostTimer += dt;
                if (lostTimer > 0.5f) { target = Surface.Floor; found = Probe(Surface.Floor, out point, out normal); }
                else { point = planePoint; normal = planeNormal; found = planeValid; target = surface; }
            }
            else lostTimer = 0f;

            if (target == Surface.Stand || !found)
            {
                weight = Mathf.MoveTowards(weight, 0f, dt * (allowed ? climbSpeed : dropSpeed));
                if (weight <= 0f) { surface = Surface.Stand; planeValid = false; limbsPlanted = false; }
                return;
            }

            if (!planeValid || weight <= 0f) { planePoint = point; planeNormal = normal; planeValid = true; }
            else
            {
                float k = 1f - Mathf.Exp(-8f * dt);
                planePoint = Vector3.Lerp(planePoint, point, k);
                planeNormal = Vector3.Slerp(planeNormal, normal, k).normalized;
            }
            surface = target;
            weight = Mathf.MoveTowards(weight, 1f, dt * climbSpeed);

            if (voice == null) return;
            bool moving = agent != null && agent.velocity.sqrMagnitude > 0.2f;
            // Scratching while it moves on a wall or the ceiling.
            if (surface != Surface.Floor && moving)
            {
                skitterTimer -= dt;
                if (skitterTimer <= 0f)
                {
                    skitterTimer = Random.Range(0.3f, 0.7f);
                    voice.pitch = Random.Range(1.3f, 1.8f);
                    voice.PlayOneShot(Audio.PlaceholderClips.Skitter, 0.25f);
                    voice.pitch = 1f;
                }
            }
            // Joints cracking as it bends into the crawl and moves.
            crackTimer -= dt * (moving ? 1f : 0.4f);
            if (crackTimer <= 0f && weight > 0.5f)
            {
                crackTimer = Random.Range(1.4f, 3.2f);
                voice.pitch = Random.Range(0.85f, 1.1f);
                voice.PlayOneShot(Audio.PlaceholderClips.BoneCrack, 0.8f);
                voice.pitch = 1f;
            }
            // Teeth grinding while it hangs above and watches.
            gnashTimer -= dt;
            if (gnashTimer <= 0f)
            {
                gnashTimer = Random.Range(5f, 10f);
                Camera cam = Camera.main;
                AudioClip gnash = Audio.PlaceholderClips.Gnash;
                if (surface != Surface.Floor && gnash != null && cam != null
                    && (cam.transform.position - transform.position).sqrMagnitude < watchDistance * watchDistance)
                    voice.PlayOneShot(gnash, 0.7f);
            }
        }

        private bool Probe(Surface s, out Vector3 point, out Vector3 normal)
        {
            Vector3 p = transform.position;
            point = p;
            normal = Vector3.up;
            switch (s)
            {
                case Surface.Floor:
                    // The agent stands on the floor NavMesh already.
                    return true;
                case Surface.Ceiling:
                    if (Cast(p + Vector3.up * 1f, Vector3.up, maxCeiling - 1f, out RaycastHit ch) && ch.point.y - p.y >= minCeiling)
                    { point = ch.point; normal = ch.normal; return true; }
                    return false;
                case Surface.Wall:
                    Vector3 side = Vector3.Cross(Vector3.up, Flat(transform.forward)) * wallSide;
                    Vector3 from = p + Vector3.up * wallHeight;
                    for (int attempt = 0; attempt < 2; attempt++)
                    {
                        if (Cast(from, side, wallReach, out RaycastHit wh) && Mathf.Abs(wh.normal.y) < 0.3f)
                        { point = wh.point; normal = wh.normal; return true; }
                        side = -side;
                    }
                    return false;
            }
            return false;
        }

        private readonly RaycastHit[] hitBuffer = new RaycastHit[8];

        /// <summary>Nearest solid hit that is neither The Echo itself nor the player.</summary>
        private bool Cast(Vector3 from, Vector3 dir, float distance, out RaycastHit best)
        {
            best = default;
            int count = Physics.RaycastNonAlloc(from, dir, hitBuffer, distance, surfaceMask, QueryTriggerInteraction.Ignore);
            float nearest = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                RaycastHit h = hitBuffer[i];
                if (h.distance >= nearest || h.collider.transform.IsChildOf(transform)) continue;
                if (h.collider.GetComponentInParent<PlayerController>() != null) continue;
                nearest = h.distance;
                best = h;
            }
            return nearest < float.MaxValue;
        }

        private static Vector3 Flat(Vector3 v)
        {
            v.y = 0f;
            return v.sqrMagnitude > 1e-6f ? v.normalized : Vector3.forward;
        }

        private void LateUpdate()
        {
            if (body == null || Time.deltaTime <= 0f) return;
            if (weight <= 0f)
            {
                body.localPosition = bodyLocalPos;
                body.localRotation = bodyLocalRot;
                if (carried != null) carried.localPosition = carriedLocalPos;
                return;
            }
            float w = Mathf.SmoothStep(0f, 1f, weight);
            Vector3 n = planeNormal;

            // Heading along the surface: the way the agent is moving.
            Vector3 move = agent != null && agent.velocity.sqrMagnitude > 0.04f ? agent.velocity : transform.forward;
            Vector3 f = Vector3.ProjectOnPlane(move, n);
            if (f.sqrMagnitude < 1e-4f) f = Vector3.ProjectOnPlane(transform.forward, n);
            if (f.sqrMagnitude < 1e-4f) f = Vector3.ProjectOnPlane(transform.up, n);
            f.Normalize();
            Vector3 side = Vector3.Cross(n, f);

            // Body lies along the surface, belly to it, head first.
            Quaternion stand = transform.rotation * bodyLocalRot;
            Quaternion crawl = Quaternion.LookRotation(f, n) * Quaternion.Euler(80f, 0f, 0f);
            Vector3 standPos = transform.TransformPoint(bodyLocalPos);
            body.rotation = crawl;
            body.position = standPos;
            Vector3 anchor = planePoint + Vector3.ProjectOnPlane(transform.position - planePoint, n);
            if (surface == Surface.Wall) anchor.y = planePoint.y; // up the wall, not at the agent's feet
            Vector3 hipsGoal = anchor + n * clearance;
            Vector3 crawlPos = standPos + (hipsGoal - (hips != null ? hips.position : standPos));
            body.rotation = Quaternion.Slerp(stand, crawl, w);
            body.position = Vector3.Lerp(standPos, crawlPos, w);
            if (carried != null && hips != null) carried.position = Vector3.Lerp(transform.TransformPoint(carriedLocalPos), hips.position, w);

            // Arch the back so the chest lifts off the surface.
            foreach (Transform b in spine)
                if (b != null) b.rotation = Quaternion.AngleAxis(-16f * w, body.right) * b.rotation;

            UpdateLimbs(n, f, side, w);
            UpdateHead(n, f, w);
        }

        private void UpdateLimbs(Vector3 n, Vector3 f, Vector3 side, float w)
        {
            if (hips == null) return;
            float dt = Time.deltaTime;
            Vector3 hp = hips.position - n * Vector3.Dot(hips.position - planePoint, n);
            float scale = body.lossyScale.y;
            Vector3[] home =
            {
                hp + f * (0.6f * scale) - side * (0.36f * scale), // left hand
                hp + f * (0.6f * scale) + side * (0.36f * scale), // right hand
                hp - f * (0.3f * scale) - side * (0.34f * scale),  // left foot
                hp - f * (0.3f * scale) + side * (0.34f * scale),  // right foot
            };
            if (!limbsPlanted)
            {
                for (int i = 0; i < 4; i++) { limbs[i].planted = home[i]; limbs[i].stepping = false; }
                limbsPlanted = true;
            }

            // Diagonal pairs step together: left hand + right foot, then right hand + left foot.
            bool pairAStepping = limbs[0].stepping || limbs[3].stepping;
            bool pairBStepping = limbs[1].stepping || limbs[2].stepping;
            float reach = stride * scale;
            for (int i = 0; i < 4; i++)
            {
                ref Limb l = ref limbs[i];
                // Keep planted points on the (moving, smoothed) surface.
                l.planted -= n * Vector3.Dot(l.planted - planePoint, n);
                if (l.stepping)
                {
                    l.t += dt / stepSeconds;
                    if (l.t >= 1f) { l.stepping = false; l.planted = l.to; }
                }
                else
                {
                    bool inA = i == 0 || i == 3;
                    bool otherPairBusy = inA ? pairBStepping : pairAStepping;
                    if (!otherPairBusy && (l.planted - home[i]).sqrMagnitude > reach * reach)
                    {
                        l.stepping = true;
                        l.t = 0f;
                        l.from = l.planted;
                        Vector3 lead = Vector3.ProjectOnPlane(home[i] - l.planted, n).normalized * reach * 0.45f;
                        l.to = home[i] + lead;
                        if (inA) pairAStepping = true; else pairBStepping = true;
                    }
                }
            }

            for (int i = 0; i < 4; i++)
            {
                Limb l = limbs[i];
                Vector3 target = l.stepping
                    ? Vector3.Lerp(l.from, l.to - n * Vector3.Dot(l.to - planePoint, n), l.t) + n * (Mathf.Sin(l.t * Mathf.PI) * stepLift * scale)
                    : l.planted;
                Transform[] chain = i == 0 ? leftArm : i == 1 ? rightArm : i == 2 ? leftLeg : rightLeg;
                if (chain == null || chain.Length < 3 || chain[0] == null || chain[1] == null || chain[2] == null) continue;
                float outward = i == 0 || i == 2 ? -1f : 1f;
                // Elbows splay out and up like a spider's; knees bend down to the surface.
                Vector3 hint = i < 2
                    ? chain[1].position + side * (outward * 0.45f) + n * 0.45f
                    : chain[1].position + side * (outward * 0.2f) + f * 0.35f - n * 0.1f;
                PlayerBody.SolveTwoBone(chain[0], chain[1], chain[2], target, hint, w);
            }
        }

        private void UpdateHead(Vector3 n, Vector3 f, float w)
        {
            if (head == null) return;
            // Looks ahead along the surface; on a wall or the ceiling it twists round to stare at Alex.
            Vector3 face = f;
            Camera cam = Camera.main;
            if (cam != null && surface != Surface.Floor)
            {
                Vector3 toCam = cam.transform.position - head.position;
                if (toCam.sqrMagnitude < watchDistance * watchDistance) face = Vector3.Slerp(f, toCam.normalized, 0.8f);
            }
            Quaternion want = Quaternion.LookRotation(face, n) * headBind;
            head.rotation = Quaternion.Slerp(head.rotation, want, w * 0.85f);
        }
    }
}
