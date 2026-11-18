using System;
using LastSignal.Audio;
using LastSignal.GameFlow;
using LastSignal.Interaction;
using LastSignal.Player;
using UnityEngine;
using UnityEngine.AI;

namespace LastSignal.Enemy
{
    /// <summary>Per-level tuning for The Echo (Level 2 moderate, Level 3 more alert but fair).</summary>
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
    }

    /// <summary>
    /// The Echo: one NavMesh enemy with a small state machine
    /// Patrol -> Investigate (noise) -> Chase (sight) -> Search (last known) -> Return -> Patrol.
    /// It never sees through walls or into hiding spots, and it warns the player through the
    /// threat audio/vignette before it arrives.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class EchoAIController : MonoBehaviour
    {
        public enum State { Inactive, Patrol, Investigate, Chase, Search, Return, Hunt, Caught }

        [SerializeField] private EchoSettings settings = new EchoSettings();
        [SerializeField] private Transform[] waypoints = new Transform[0];
        [SerializeField] private Transform eye;
        [SerializeField] private LayerMask sightMask = ~0;
        [SerializeField] private bool startActive = true;
        [SerializeField] private AudioSource voice;
        [Tooltip("Distance at which the threat warning starts.")]
        [SerializeField] private float warningDistance = 14f;

        private NavMeshAgent agent;
        private Transform player;
        private PlayerController playerController;
        private FlashlightController flashlight;
        private Transform playerEye;

        private int waypointIndex;
        private float stateTimer;
        private float lostSightTimer;
        private float chaseTimer;
        private float stepTimer;
        private Vector3 lastKnown;
        private Vector3 searchCentre;
        private bool waiting;

        public State Current { get; private set; } = State.Inactive;
        public EchoSettings Settings => settings;
        public bool CanSeePlayerNow { get; private set; }
        public event Action<State> StateChanged;

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            if (eye == null) eye = transform;
        }

        private void OnEnable() => NoiseSystem.NoiseMade += OnNoise;
        private void OnDisable() => NoiseSystem.NoiseMade -= OnNoise;

        [Header("Fair restarts")]
        [Tooltip("After a level start or checkpoint reload, The Echo is moved at least this far from the player.")]
        [SerializeField] private float safeRestartDistance = 15f;
        [Tooltip("Seconds after a (re)start during which The Echo cannot see, hear or catch the player.")]
        [SerializeField] private float restartGraceSeconds = 4f;

        private float graceTimer;
        private string ActiveFlag => "echo:active:" + gameObject.scene.name;
        public bool InGracePeriod => graceTimer > 0f;

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

        /// <summary>True once the (re)start placement has run.</summary>
        public bool Started { get; private set; }

        /// <summary>Never restart right next to the player: jump to the patrol point farthest from them.</summary>
        private void MoveAwayFromPlayer()
        {
            if (player == null || waypoints.Length == 0) return;
            if (Vector3.Distance(transform.position, player.position) >= safeRestartDistance) return;
            int best = -1;
            float bestDist = -1f;
            for (int i = 0; i < waypoints.Length; i++)
            {
                if (waypoints[i] == null) continue;
                float d = Vector3.Distance(waypoints[i].position, player.position);
                if (d > bestDist) { bestDist = d; best = i; }
            }
            if (best < 0) return;
            waypointIndex = best;
            WarpTo(waypoints[best].position);
        }

        private void FindPlayer()
        {
            playerController = FindAnyObjectByType<PlayerController>();
            if (playerController == null) return;
            player = playerController.transform;
            flashlight = playerController.GetComponentInChildren<FlashlightController>();
            Camera cam = playerController.GetComponentInChildren<Camera>();
            playerEye = cam != null ? cam.transform : player;
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
            if (graceTimer > 0f) graceTimer -= Time.deltaTime;
            if (Current == State.Inactive || Current == State.Caught) return;
            if (player == null) { FindPlayer(); if (player == null) return; }
            if (GameFlowManager.Instance != null && GameFlowManager.Instance.IsGameOver) return;

            CanSeePlayerNow = !InGracePeriod && CanSeePlayer();
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
                case State.Chase: UpdateChase(); break;
                case State.Search: UpdateSearch(); break;
                case State.Return: UpdateReturn(); break;
                case State.Hunt: UpdateHunt(); break;
            }

            TryCatch();
            ReportThreat();
            PlaySteps();
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
            waypointIndex = (waypointIndex + 1) % waypoints.Length;
            Go(waypoints[waypointIndex].position);
        }

        private void UpdateInvestigate()
        {
            agent.speed = settings.investigateSpeed;
            if (Arrived()) BeginSearch(transform.position);
        }

        private void UpdateChase()
        {
            agent.speed = settings.chaseSpeed;
            chaseTimer += Time.deltaTime;
            if (!CanSeePlayerNow) lostSightTimer += Time.deltaTime;
            Go(lastKnown);
            if (lostSightTimer > settings.loseSightSeconds || chaseTimer > settings.maxChaseSeconds || HidingSpot.PlayerHidden && lostSightTimer > 0.5f)
                BeginSearch(lastKnown);
        }

        private void UpdateSearch()
        {
            agent.speed = settings.investigateSpeed * 0.8f;
            stateTimer -= Time.deltaTime;
            if (stateTimer <= 0f) { SetState(State.Return); GoToNearestWaypoint(); return; }
            if (!Arrived()) return;
            Vector2 r = UnityEngine.Random.insideUnitCircle * 4f;
            Vector3 p = searchCentre + new Vector3(r.x, 0f, r.y);
            if (NavMesh.SamplePosition(p, out NavMeshHit hit, 2f, NavMesh.AllAreas)) Go(hit.position);
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
            if (Current == State.Inactive || Current == State.Caught || Current == State.Chase || Current == State.Hunt) return;
            if (InGracePeriod) return;
            float effective = radius * settings.hearingMultiplier;
            // Walls muffle sound.
            if (Physics.Linecast(eye.position, position + Vector3.up * 1f, out RaycastHit h, sightMask, QueryTriggerInteraction.Ignore)
                && (player == null || !h.collider.transform.IsChildOf(player)))
                effective *= 0.6f;
            if (Vector3.Distance(transform.position, position) > effective) return;

            searchCentre = position;
            Go(position);
            SetState(State.Investigate);
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
            SetState(State.Search);
            Go(centre);
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
            SetState(State.Caught);
            if (agent.isOnNavMesh) agent.ResetPath();
            AudioManager.Instance?.PlayUI(PlaceholderClips.Scream, 1f);
            GameFlowManager.Instance?.PlayerCaught();
        }

        private void ReportThreat()
        {
            if (player == null || AudioManager.Instance == null) return;
            float d = Vector3.Distance(player.position, transform.position);
            float level = Mathf.Clamp01(1f - d / warningDistance);
            if (Current == State.Chase || Current == State.Hunt) level = Mathf.Max(level, 0.45f);
            AudioManager.Instance.ReportThreat(level);
        }

        private float voiceTimer = 6f;

        /// <summary>Occasional ghostly vocalisation so the player can locate The Echo by ear.</summary>
        private void PlayVoice()
        {
            if (voice == null) return;
            voiceTimer -= Time.deltaTime;
            if (voiceTimer > 0f) return;
            voiceTimer = UnityEngine.Random.Range(9f, 16f);
            AudioClip clip = PlaceholderClips.EchoVoice;
            if (clip != null) voice.PlayOneShot(clip, 0.7f);
        }

        private void PlaySteps()
        {
            PlayVoice();
            if (voice == null || agent.velocity.sqrMagnitude < 0.1f) return;
            stepTimer -= Time.deltaTime;
            if (stepTimer > 0f) return;
            stepTimer = Current == State.Chase || Current == State.Hunt ? 0.35f : 0.65f;
            voice.pitch = 0.6f;
            voice.PlayOneShot(PlaceholderClips.Thud, 0.5f);
            voice.pitch = 1f;
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
