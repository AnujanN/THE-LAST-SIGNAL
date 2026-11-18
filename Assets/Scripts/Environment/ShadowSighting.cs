using LastSignal.Audio;
using LastSignal.GameFlow;
using LastSignal.Player;
using UnityEngine;

namespace LastSignal.Environment
{
    /// <summary>
    /// A brief, harmless figure (The Echo's shadow, or Maya's ghost). Appears when Show() is called and
    /// vanishes when the player gets close, a moment after they look straight at it, or after a few
    /// seconds. Happens once.
    /// </summary>
    public class ShadowSighting : MonoBehaviour
    {
        [SerializeField] private string id = "shadow";
        [SerializeField] private GameObject figure;
        [SerializeField] private float vanishDistance = 6f;
        [SerializeField] private float maxSeconds = 4f;
        [Tooltip("Vanish shortly after the player looks directly at it (a glimpse).")]
        [SerializeField] private bool vanishWhenSeen;
        [SerializeField] private float seenAngle = 14f;
        [SerializeField] private float seenDelay = 0.35f;
        [SerializeField] private bool facePlayer = true;
        [Tooltip("Optional whisper when it appears: SPEAKER|text|seconds.")]
        [SerializeField] private string whisper = "";
        [Tooltip("Hide it again at the end of a recording etc. (UnityEvent: Hide).")]
        [SerializeField] private bool silentVanish;

        private Transform viewer;
        private float seenTimer = -1f;

        private Transform player;
        private float timer;
        private bool showing;

        private void Awake()
        {
            if (figure != null) figure.SetActive(false);
        }

        public void Show()
        {
            if (showing || WorldFlags.Has("seen:" + id) || figure == null) return;
            WorldFlags.Set("seen:" + id);
            PlayerController p = FindAnyObjectByType<PlayerController>();
            player = p != null ? p.transform : null;
            Camera cam = p != null ? p.GetComponentInChildren<Camera>() : null;
            viewer = cam != null ? cam.transform : player;
            figure.SetActive(true);
            showing = true;
            timer = maxSeconds;
            seenTimer = -1f;
            if (!silentVanish) AudioManager.Instance?.PlayAt(PlaceholderClips.Thud, figure.transform.position, 0.6f);
            if (!string.IsNullOrEmpty(whisper)) SubtitleManager.Instance?.SayPacked(whisper);
        }

        /// <summary>Make it disappear now (e.g. when a recording ends).</summary>
        public void Hide()
        {
            if (!showing) return;
            Vanish();
        }

        private void Vanish()
        {
            figure.SetActive(false);
            showing = false;
            if (!silentVanish) AudioManager.Instance?.PlayUI(PlaceholderClips.StaticBurst, 0.3f);
        }

        private void Update()
        {
            if (!showing) return;
            timer -= Time.deltaTime;
            if (facePlayer && player != null)
            {
                Vector3 to = player.position - figure.transform.position;
                to.y = 0f;
                if (to.sqrMagnitude > 0.01f) figure.transform.rotation = Quaternion.LookRotation(to);
            }
            if (vanishWhenSeen && viewer != null && seenTimer < 0f)
            {
                Vector3 dir = figure.transform.position + Vector3.up * 1.2f - viewer.position;
                if (Vector3.Angle(viewer.forward, dir) < seenAngle) seenTimer = seenDelay;
            }
            if (seenTimer > 0f) seenTimer -= Time.deltaTime;
            bool close = player != null && Vector3.Distance(player.position, figure.transform.position) < vanishDistance;
            if (timer <= 0f || close || (seenTimer >= 0f && seenTimer <= 0f)) Vanish();
        }
    }
}
