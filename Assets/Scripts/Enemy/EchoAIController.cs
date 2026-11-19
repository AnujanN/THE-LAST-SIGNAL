using System;
using System.Collections.Generic;
using LastSignal.Audio;
using LastSignal.GameFlow;
using LastSignal.Interaction;
using LastSignal.Player;
using LastSignal.Utilities;
using UnityEngine;
using UnityEngine.AI;

namespace LastSignal.Enemy
{
    /// <summary>Per-level tuning for The Echo. Later levels are more alert but always fair.</summary>
    [Serializable]
    public class EchoSettings
    {
        [Header("Senses")]
        public float sightRange = 11f;
        [Range(30f, 180f)] public float sightAngle = 100f;
        [Tooltip("Sight range multiplier while the player is crouched.")]
        public float crouchSightFactor = 0.55f;
        [Tooltip("Sight range multiplier while the flashlight is on.")]
        public float flashlightSightFactor = 1.35f;
        public float hearingMultiplier = 1f;

        [Header("Speeds (player walks 2.2, sprints 4.0)")]
        public float patrolSpeed = 1.3f;
        public float investigateSpeed = 2.0f;
        public float chaseSpeed = 3.3f;

        [Header("Timing")]
        public float waypointPause = 1.5f;
        [Tooltip("How long it keeps chasing the last known position after losing sight.")]
        public float loseSightSeconds = 2.5f;
        public float searchSeconds = 8f;
        public float maxChaseSeconds = 25f;
        public float catchDistance = 1.2f;
        [Tooltip("Seconds it stands still listening when it reaches a noise.")]
        public float listenSeconds = 2.5f;

        [Header("Intelligence")]
        [Tooltip("Chance (0-1) that a search near a hidden player turns into inspecting their hiding spot.")]
        [Range(0f, 1f)] public float inspectChance = 0.5f;
        [Tooltip("How far it notices torch light leaking out of a hiding place (it must be in front of it and facing it).")]
        public float leakSightRange = 8f;
        [Tooltip("Seconds it stands at a hiding spot; the player must hold their breath meanwhile.")]
        public float inspectSeconds = 3f;
        [Tooltip("Director: if it has been far from the player this long, it gets a vague hint of their area.")]
        public float directorHintSeconds = 45f;
        [Tooltip("Director: radius of that vague hint (never the exact position).")]
        public float directorHintRadius = 7f;
        [Tooltip("Director: after this long close by without a chase, it backs off for a while.")]
        public float directorBackoffSeconds = 30f;
        [Tooltip("Seconds between calls in Maya's voice to lure Alex (0 = never).")]
        public float lureInterval = 55f;
    }

    /// <summary>
    /// The Echo. NavMesh state machine:
    /// Patrol -> Investigate (noise) -> Listen -> Search -> Return -> Patrol, Chase on sight,
    /// Inspect (checks a hiding spot; holding breath keeps the player safe), Hunt (finale).
    /// A small "director" keeps it near the player without cheating, and backs it off after long pressure.
    /// It never sees through walls, and it always warns the player (heartbeat, vignette, flicker, voice).
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class EchoAIController : MonoBehaviour
    {
        public enum State { Inactive, Patrol, Investigate, Listen, Chase, Search, Inspect, Return, Hunt, Caught }

        [SerializeField] private EchoSettings settings = new EchoSettings();
        [SerializeField] private Transform[] waypoints = new Transform[0];
        [SerializeField] private Transform eye;
        [SerializeField] private LayerMask sightMask = ~0;
        [SerializeField] private bool startActive = true;
        [SerializeField] private AudioSource voice;
        [Tooltip("Distance at which the threat warning starts.")]
        [SerializeField] private float warningDistance = 14f;
        [SerializeField] private Animator animator;

        [Header("Fair restarts")]
        [Tooltip("After a level start or checkpoint reload, The Echo is moved at least this far from the player.")]
        [SerializeField] private float safeRestartDistance = 15f;
        [Tooltip("Seconds after a (re)start during which The Echo cannot see, hear or catch the player.")]
        [SerializeField] private float restartGraceSeconds = 4f;

        private NavMeshAgent agent;
        private Transform player;
        private PlayerController playerController;
        private FlashlightController flashlight;
        private PlayerBreath breath;
        private Transform playerEye;

        private int waypointIndex;
        private float stateTimer;
        private float lostSightTimer;
        private float chaseTimer;
        private float stepTimer;
        private float graceTimer;
        private float farTimer;
        private float closeTimer;
        private float backoffTimer;
        private float lureTimer;
        private float surgeTimer;
        private float voiceTimer = 6f;
        private bool inspectedThisSearch;
        private bool resumeHuntAfterInspect;
        private bool warnedAtSpot;
        private float leakCheckTimer;
        private bool waiting;
        private Vector3 lastKnown;
        private Vector3 searchCentre;
        private HidingSpot inspecting;
        private readonly List<Vector3> heardRecently = new List<Vector3>(4);

        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int StateHash = Animator.StringToHash("State");

        public State Current { get; private set; } = State.Inactive;
        public EchoSettings Settings => settings;
        public bool CanSeePlayerNow { get; private set; }
        public bool InGracePeriod => graceTimer > 0f;
        /// <summary>True once the (re)start placement has run.</summary>
        public bool Started { get; private set; }
        /// <summary>True while it is standing at the player's hiding spot.</summary>
        public bool IsInspectingPlayer => Current == State.Inspect && inspecting != null && inspecting == HidingSpot.Current;
        public event Action<State> StateChanged;

        private string ActiveFlag => "echo:active:" + gameObject.scene.name;

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            if (eye == null) eye = transform;
            if (animator == null) animator = GetComponentInChildren<Animator>();
            lureTimer = settings.lureInterval * UnityEngine.Random.Range(0.5f, 1f);
        }

        private void OnEnable() => NoiseSystem.NoiseMade += OnNoise;
        private void OnDisable() => NoiseSystem.NoiseMade -= OnNoise;

        private System.Collections.IEnumerator Start()
        {
            // Wait for the LevelController to place the player at the checkpoint spawn.
            yield return null;
            yield return null;
            FindPlayer();
            MoveAwayFromPlayer();
            graceTimer = restartGraceSeconds;
            // Remembered activation: a one-shot trigger that woke it up must not be needed again after a reload.
            if (startActive || WorldFlags.Has(ActiveFlag)) Activate();
            Started = true;
        }

        private void FindPlayer()
        {
            playerController = FindAnyObjectByType<PlayerController>();
            if (playerController == null) return;
            player = playerController.transform;
            flashlight = playerController.GetComponentInChildren<FlashlightController>();
            breath = playerController.GetComponent<PlayerBreath>();
            Camera cam = playerController.GetComponentInChildren<Camera>();
            playerEye = cam != null ? cam.transform : player;
        }

        /// <summary>Never restart right next to the player: jump to the patrol point farthest from them.</summary>
        private void MoveAwayFromPlayer()
        {
            if (player == null || waypoints.Length == 0) return;
            if (Vector3.Distance(transform.position, player.position) >= safeRestartDistance) return;
            int best = FarthestWaypointFrom(player.position);
            if (best < 0) return;
            waypointIndex = best;
            WarpTo(waypoints[best].position);
        }

        // ------------------------------------------------------------ public control

        public void Activate()
        {
            if (Current != State.Inactive) return;
            gameObject.SetActive(true);
            if (player == null) FindPlayer();
            WorldFlags.Set(ActiveFlag);
            SetState(waypoints.Length > 0 ? State.Patrol : State.Search);
            searchCentre = transform.position;
        }

        public void Deactivate()
        {
            SetState(State.Inactive);
            if (agent.isOnNavMesh) agent.ResetPath();
        }

        /// <summary>Withdraw completely (scripted story moments); StartHunt or Activate brings it back.</summary>
        public void Vanish()
        {
            Deactivate();
            AudioManager.Instance?.PlayUI(PlaceholderClips.Scream, 0.7f);
            gameObject.SetActive(false);
        }

        /// <summary>Finale: always knows roughly where the player is and closes in.</summary>
        public void StartHunt()
        {
            gameObject.SetActive(true);
            if (player == null) FindPlayer();
            SetState(State.Hunt);
        }

        public void WarpTo(Vector3 position)
        {
            if (agent.isOnNavMesh || NavMesh.SamplePosition(position, out _, 2f, NavMesh.AllAreas)) agent.Warp(position);
            else transform.position = position;
        }

        public void ApplySettings(EchoSettings s) => settings = s;

        // ------------------------------------------------------------ update

        private void Update()
        {
            float dt = Time.deltaTime;
            if (graceTimer > 0f) graceTimer -= dt;
            UpdateAnimator();
            if (Current == State.Inactive || Current == State.Caught) return;
            if (player == null) { FindPlayer(); if (player == null) return; }
            if (GameFlowManager.Instance != null && GameFlowManager.Instance.IsGameOver) return;

            CanSeePlayerNow = !InGracePeriod && CanSeePlayer();
            CheckLightLeak(dt);
            if (CanSeePlayerNow)
            {
                lastKnown = player.position;
                lostSightTimer = 0f;
                if (Current != State.Chase && Current != State.Hunt) BeginChase();
            }

            switch (Current)
            {
                case State.Patrol: UpdatePatrol(); break;
                case State.Investigate: UpdateInvestigate(); break;
                case State.Listen: UpdateListen(dt); break;
                case State.Chase: UpdateChase(dt); break;
                case State.Search: UpdateSearch(dt); break;
                case State.Inspect: UpdateInspect(dt); break;
                case State.Return: UpdateReturn(); break;
                case State.Hunt: UpdateHunt(); break;
            }

            UpdateDirector(dt);
            UpdateLure(dt);
            TryCatch();
            ReportThreat();
            PlaySteps(dt);
        }

        private void UpdatePatrol()
        {
            if (waypoints.Length == 0) { SetState(State.Search); return; }
            agent.speed = settings.patrolSpeed;
            if (!Arrived()) return;
            if (!waiting) { waiting = true; stateTimer = settings.waypointPause; }
            stateTimer -= Time.deltaTime;
            if (stateTimer > 0f) return;
            waiting = false;
            // Unpredictable route: usually the next point, sometimes skip ahead or double back.
            int step = UnityEngine.Random.value < 0.7f ? 1 : UnityEngine.Random.Range(2, Mathf.Max(3, waypoints.Length));
            waypointIndex = (waypointIndex + step) % waypoints.Length;
            Go(waypoints[waypointIndex].position);
        }

        private void UpdateInvestigate()
        {
            agent.speed = settings.investigateSpeed;
            if (!Arrived()) return;
            stateTimer = settings.listenSeconds;
            SetState(State.Listen);
        }

        /// <summary>Stands still, slowly turning, listening for the next sound.</summary>
        private void UpdateListen(float dt)
        {
            if (agent.isOnNavMesh) agent.ResetPath();
            transform.Rotate(0f, Mathf.Sin(Time.time * 1.3f) * 40f * dt, 0f);
            stateTimer -= dt;
            if (stateTimer <= 0f) BeginSearch(transform.position);
        }

        private void UpdateChase(float dt)
        {
            // Erratic, unnatural pace: short surges and hesitations around the base chase speed.
            surgeTimer -= dt;
            if (surgeTimer <= 0f) surgeTimer = UnityEngine.Random.Range(0.6f, 1.8f);
            float surge = Mathf.PerlinNoise(Time.time * 0.9f, 7.7f) > 0.6f ? 1.15f : 0.9f;
            agent.speed = settings.chaseSpeed * surge;
            chaseTimer += dt;
            if (!CanSeePlayerNow) lostSightTimer += dt;
            Go(lastKnown);
            if (lostSightTimer > settings.loseSightSeconds || chaseTimer > settings.maxChaseSeconds || HidingSpot.PlayerHidden && lostSightTimer > 0.5f)
                BeginSearch(lastKnown);
        }

        private void UpdateSearch(float dt)
        {
            agent.speed = settings.investigateSpeed * 0.8f;
            stateTimer -= dt;

            // Suspicious of hiding spots near where it lost the player.
            if (!inspectedThisSearch && HidingSpot.PlayerHidden)
            {
                HidingSpot spot = HidingSpot.Current;
                if (spot != null && Vector3.Distance(spot.InspectPosition, searchCentre) < 7f)
                {
                    inspectedThisSearch = true;
                    if (UnityEngine.Random.value < settings.inspectChance) { BeginInspect(spot); return; }
                }
            }

            if (stateTimer <= 0f) { SetState(State.Return); GoToNearestWaypoint(); return; }
            if (!Arrived()) return;
            // Re-check places it heard noise recently, otherwise wander around the search centre.
            if (heardRecently.Count > 0 && UnityEngine.Random.value < 0.4f)
            {
                Vector3 p = heardRecently[UnityEngine.Random.Range(0, heardRecently.Count)];
                if (NavMesh.SamplePosition(p, out NavMeshHit near, 2f, NavMesh.AllAreas)) { Go(near.position); return; }
            }
            Vector2 r = UnityEngine.Random.insideUnitCircle * 4f;
            if (NavMesh.SamplePosition(searchCentre + new Vector3(r.x, 0f, r.y), out NavMeshHit hit, 2f, NavMesh.AllAreas)) Go(hit.position);
        }

        private void BeginInspect(HidingSpot spot)
        {
            resumeHuntAfterInspect = Current == State.Hunt;
            inspecting = spot;
            stateTimer = settings.inspectSeconds;
            SetState(State.Inspect);
            Go(spot.InspectPosition);
            // The breath warning comes when it is right outside (see UpdateInspect), so a held breath lasts.
            HudMessage.Show(spot.LightLeaking
                ? "It saw your light! Turn the torch off!"
                : "Something is coming to check your hiding place...");
            warnedAtSpot = false;
        }

        /// <summary>
        /// A torch left on inside a hiding place shines out of its front. If The Echo is in front of it, facing
        /// it, within range and with a clear view, it comes straight over to look (whatever its mood).
        /// </summary>
        private void CheckLightLeak(float dt)
        {
            if (!HidingSpot.PlayerHidden || InGracePeriod) return;
            if (Current == State.Inspect || Current == State.Caught || Current == State.Inactive) return;
            leakCheckTimer -= dt;
            if (leakCheckTimer > 0f) return;
            leakCheckTimer = 0.25f;
            HidingSpot spot = HidingSpot.Current;
            if (spot != null && spot.LightLeaking && SeesLightLeak(spot)) BeginInspect(spot);
        }

        /// <summary>Can it see the light leaking from this spot right now?</summary>
        public bool SeesLightLeak(HidingSpot spot)
        {
            if (spot == null || !spot.LightLeaking) return false;
            Vector3 p = spot.LeakPoint;
            Vector3 to = p - eye.position;
            float dist = to.magnitude;
            if (dist > settings.leakSightRange || dist < 1e-3f) return false;
            // The light comes out of the front only.
            if (Vector3.Dot(spot.Front, -to) <= 0f) return false;
            if (Vector3.Angle(eye.forward, to) > settings.sightAngle * 0.5f && dist > 2f) return false;
            RaycastHit[] hits = Physics.RaycastAll(eye.position, to / dist, dist, sightMask, QueryTriggerInteraction.Ignore);
            foreach (RaycastHit h in hits)
            {
                Transform t = h.collider.transform;
                if (t.IsChildOf(transform) || t.IsChildOf(spot.transform) || (player != null && t.IsChildOf(player))) continue;
                return false;
            }
            return true;
        }

        /// <summary>
        /// Stands at a hiding spot. A hidden player is only found if they are not holding their breath once it is
        /// right outside, or if they gasp. Leaving the spot during inspection means being seen normally.
        /// </summary>
        private void UpdateInspect(float dt)
        {
            agent.speed = settings.investigateSpeed;
            if (inspecting == null) { BeginSearch(transform.position); return; }
            bool atSpot = Vector3.Distance(transform.position, inspecting.InspectPosition) < 1.2f;
            if (atSpot)
            {
                if (!warnedAtSpot) { warnedAtSpot = true; HudMessage.Show("It's right outside. Hold your breath!"); }
                if (agent.isOnNavMesh) agent.ResetPath();
                Vector3 look = inspecting.transform.position - transform.position;
                look.y = 0f;
                if (look.sqrMagnitude > 0.01f)
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(look), dt * 3f);
                stateTimer -= dt;

                bool playerInside = inspecting == HidingSpot.Current;
                bool breathing = breath == null || !breath.Holding;
                // Right outside the door, light through the gaps gives the player away even if they hold their breath.
                bool lit = playerInside && inspecting.LightLeaking;
                if (playerInside && (breathing && stateTimer < settings.inspectSeconds - 1f || breath != null && breath.RecentlyGasped
                                     || lit && stateTimer < settings.inspectSeconds - 0.6f))
                {
                    Discover();
                    return;
                }
            }
            if (stateTimer <= 0f)
            {
                inspecting = null;
                if (resumeHuntAfterInspect) { resumeHuntAfterInspect = false; SetState(State.Hunt); return; }
                BeginSearch(transform.position);
                stateTimer = settings.searchSeconds * 0.5f;
                inspectedThisSearch = true;
            }
        }

        /// <summary>Scripted discovery at a hiding spot (the player was breathing or gasped).</summary>
        private void Discover()
        {
            HidingSpot spot = HidingSpot.Current;
            if (spot != null) spot.Leave();
            Catch();
        }

        private void UpdateReturn()
        {
            agent.speed = settings.patrolSpeed;
            if (Arrived()) SetState(State.Patrol);
        }

        private void UpdateHunt()
        {
            agent.speed = settings.chaseSpeed;
            // Knows where the player is unless they are hidden; then it prowls nearby instead of
            // camping the hiding spot, so the player always gets a window to move on.
            if (!HidingSpot.PlayerHidden)
            {
                lastKnown = player.position;
                Go(lastKnown);
                return;
            }
            if (!Arrived()) return;
            Vector2 r = UnityEngine.Random.insideUnitCircle * 6f;
            if (NavMesh.SamplePosition(lastKnown + new Vector3(r.x, 0f, r.y), out NavMeshHit hit, 2f, NavMesh.AllAreas))
                Go(hit.position);
        }

        // ------------------------------------------------------------ director + lure

        /// <summary>
        /// Keeps tension rising and falling: a vague hint after a long time apart (never the exact position),
        /// a back-off after a long time close without a chase.
        /// </summary>
        private void UpdateDirector(float dt)
        {
            if (Current == State.Hunt || Current == State.Chase || Current == State.Inspect) { farTimer = closeTimer = 0f; return; }
            float d = Vector3.Distance(transform.position, player.position);
            if (backoffTimer > 0f) { backoffTimer -= dt; return; }

            if (d > 20f) farTimer += dt; else farTimer = 0f;
            if (d < 10f) closeTimer += dt; else closeTimer = 0f;

            if (farTimer > settings.directorHintSeconds && !HidingSpot.PlayerHidden)
            {
                farTimer = 0f;
                Vector2 r = UnityEngine.Random.insideUnitCircle * settings.directorHintRadius;
                Vector3 guess = player.position + new Vector3(r.x, 0f, r.y);
                if (NavMesh.SamplePosition(guess, out NavMeshHit hit, 3f, NavMesh.AllAreas))
                {
                    searchCentre = hit.position;
                    Go(hit.position);
                    SetState(State.Investigate);
                }
            }
            else if (closeTimer > settings.directorBackoffSeconds && waypoints.Length > 0)
            {
                closeTimer = 0f;
                backoffTimer = 20f;
                int far = FarthestWaypointFrom(player.position);
                if (far >= 0) { waypointIndex = far; Go(waypoints[far].position); SetState(State.Return); }
            }
        }

        /// <summary>Calls out in Maya's voice from where it stands, to draw Alex towards it.</summary>
        private void UpdateLure(float dt)
        {
            if (settings.lureInterval <= 0f || Current == State.Chase || Current == State.Hunt || Current == State.Inspect) return;
            lureTimer -= dt;
            if (lureTimer > 0f) return;
            lureTimer = settings.lureInterval * UnityEngine.Random.Range(0.8f, 1.3f);
            if (Vector3.Distance(transform.position, player.position) > 25f) return;
            AudioClip clip = PlaceholderClips.EchoVoice;
            if (voice != null && clip != null) voice.PlayOneShot(clip, 1f);
            string[] lines = { "Alex? I'm over here...", "Alex... it's so dark. Come find me.", "Why did you leave me, Alex?", "I'm still here. Follow my voice." };
            SubtitleManager.Instance?.Say("MAYA?", lines[UnityEngine.Random.Range(0, lines.Length)], 3f, critical: false);
        }

        // ------------------------------------------------------------ senses

        public bool CanSeePlayer()
        {
            if (player == null || HidingSpot.PlayerHidden) return false;
            Vector3 from = eye.position;
            Vector3 to = playerEye.position;
            Vector3 dir = to - from;
            float dist = dir.magnitude;

            float range = settings.sightRange;
            if (playerController != null && playerController.IsCrouching) range *= settings.crouchSightFactor;
            if (flashlight != null && flashlight.IsOn) range *= settings.flashlightSightFactor;
            if (dist > range) return false;
            if (Vector3.Angle(eye.forward, dir) > settings.sightAngle * 0.5f && dist > 1.5f) return false;

            // Line of sight: nothing solid between the eyes (ignore our own and the player's colliders).
            RaycastHit[] hits = Physics.RaycastAll(from, dir / dist, dist, sightMask, QueryTriggerInteraction.Ignore);
            foreach (RaycastHit h in hits)
            {
                Transform t = h.collider.transform;
                if (t.IsChildOf(transform) || t.IsChildOf(player)) continue;
                return false;
            }
            return true;
        }

        private void OnNoise(Vector3 position, float radius)
        {
            if (Current == State.Inactive || Current == State.Caught || Current == State.Chase || Current == State.Hunt || Current == State.Inspect) return;
            if (InGracePeriod) return;
            float effective = radius * settings.hearingMultiplier;
            // Walls muffle sound.
            if (Physics.Linecast(eye.position, position + Vector3.up * 1f, out RaycastHit h, sightMask, QueryTriggerInteraction.Ignore)
                && (player == null || !h.collider.transform.IsChildOf(player)))
                effective *= 0.6f;
            if (Vector3.Distance(transform.position, position) > effective) return;

            Remember(position);
            searchCentre = position;
            Go(position);
            SetState(State.Investigate);
        }

        private void Remember(Vector3 p)
        {
            if (heardRecently.Count >= 4) heardRecently.RemoveAt(0);
            heardRecently.Add(p);
        }

        // ------------------------------------------------------------ helpers

        private void BeginChase()
        {
            chaseTimer = 0f;
            lostSightTimer = 0f;
            SetState(State.Chase);
            if (voice != null) voice.PlayOneShot(PlaceholderClips.Scream, 0.6f);
        }

        private void BeginSearch(Vector3 centre)
        {
            searchCentre = centre;
            stateTimer = settings.searchSeconds;
            inspectedThisSearch = false;
            SetState(State.Search);
            Go(centre);
        }

        private int FarthestWaypointFrom(Vector3 from)
        {
            int best = -1;
            float bestDist = -1f;
            for (int i = 0; i < waypoints.Length; i++)
            {
                if (waypoints[i] == null) continue;
                float d = Vector3.Distance(waypoints[i].position, from);
                if (d > bestDist) { bestDist = d; best = i; }
            }
            return best;
        }

        private void GoToNearestWaypoint()
        {
            if (waypoints.Length == 0) { SetState(State.Search); return; }
            float best = float.MaxValue;
            for (int i = 0; i < waypoints.Length; i++)
            {
                float d = (waypoints[i].position - transform.position).sqrMagnitude;
                if (d < best) { best = d; waypointIndex = i; }
            }
            Go(waypoints[waypointIndex].position);
        }

        private void TryCatch()
        {
            if (player == null || HidingSpot.PlayerHidden || InGracePeriod) return;
            if (Current != State.Chase && Current != State.Hunt) return;
            Vector3 flat = player.position - transform.position;
            flat.y = 0f;
            if (flat.magnitude > settings.catchDistance) return;
            Catch();
        }

        private void Catch()
        {
            SetState(State.Caught);
            if (agent.isOnNavMesh) agent.ResetPath();
            // The catch is a short, horrible moment in your face (EchoCatchSequence), then game over.
            EchoCatchSequence sequence = GetComponent<EchoCatchSequence>();
            if (sequence != null && isActiveAndEnabled)
            {
                sequence.Play(() => GameFlowManager.Instance?.PlayerCaught());
                return;
            }
            AudioManager.Instance?.PlayUI(PlaceholderClips.Scream, 1f);
            GameFlowManager.Instance?.PlayerCaught();
        }

        private void ReportThreat()
        {
            if (player == null || AudioManager.Instance == null) return;
            float d = Vector3.Distance(player.position, transform.position);
            float level = Mathf.Clamp01(1f - d / warningDistance);
            if (Current == State.Chase || Current == State.Hunt || Current == State.Inspect) level = Mathf.Max(level, 0.5f);
            AudioManager.Instance.ReportThreat(level);
        }

        private void PlaySteps(float dt)
        {
            // Occasional ghostly vocalisation so the player can locate The Echo by ear.
            voiceTimer -= dt;
            if (voiceTimer <= 0f && voice != null)
            {
                voiceTimer = UnityEngine.Random.Range(9f, 16f);
                AudioClip clip = PlaceholderClips.EchoVoice;
                if (clip != null) voice.PlayOneShot(clip, 0.7f);
            }
            if (voice == null || agent.velocity.sqrMagnitude < 0.1f) return;
            stepTimer -= dt;
            if (stepTimer > 0f) return;
            stepTimer = Current == State.Chase || Current == State.Hunt ? 0.35f : 0.65f;
            voice.pitch = 0.6f;
            voice.PlayOneShot(PlaceholderClips.Thud, 0.5f);
            voice.pitch = 1f;
        }

        private void UpdateAnimator()
        {
            if (animator == null || !animator.isActiveAndEnabled) return;
            animator.SetFloat(SpeedHash, agent != null && agent.isOnNavMesh ? agent.velocity.magnitude : 0f);
            animator.SetInteger(StateHash, (int)Current);
        }

        private void Go(Vector3 destination)
        {
            if (agent.isOnNavMesh) agent.SetDestination(destination);
        }

        private bool Arrived()
        {
            if (!agent.isOnNavMesh) return true;
            if (agent.pathPending) return false;
            return agent.remainingDistance <= agent.stoppingDistance + 0.3f;
        }

        private void SetState(State s)
        {
            if (Current == s) return;
            Current = s;
            waiting = false;
            if (s == State.Search && stateTimer <= 0f) stateTimer = settings.searchSeconds;
            if (s == State.Patrol && waypoints.Length > 0) Go(waypoints[waypointIndex].position);
            StateChanged?.Invoke(s);
        }
    }
}
