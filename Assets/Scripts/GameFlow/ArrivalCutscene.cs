using System.Collections;
using LastSignal.Audio;
using LastSignal.Environment;
using LastSignal.Player;
using UnityEngine;

namespace LastSignal.GameFlow
{
    /// <summary>
    /// Level 1's first moments, in one unbroken shot through Alex's eyes (no cuts, no control): he looks at the
    /// station, then his view slowly turns and narrows onto the lit upstairs window, where someone is standing,
    /// watching him. It turns and walks away past the second window and out of sight; his eyes follow it.
    /// "Who is that?" ... "Maya? Is that you?" Then his view widens back to normal and the player has control.
    /// Plays once per run (not after a checkpoint retry). Runs after the camera's own sway so it has the last word.
    /// </summary>
    [DefaultExecutionOrder(200)]
    public class ArrivalCutscene : MonoBehaviour
    {
        /// <summary>Tests that walk Level 1 turn this off (each test's set-up sets it again).</summary>
        public static bool SkipForTests;

        // The editor keeps static values between Play sessions here (no domain reload), so a test run used to
        // leave the shot switched off for the next normal play. Reset it every time play starts.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => SkipForTests = false;

        [SerializeField] private WindowWatcher watcher;
        [Tooltip("The middle of the first window (where the figure stands).")]
        [SerializeField] private Transform firstWindow;
        [SerializeField] private float zoomFov = 22f;
        [SerializeField] private SubtitleLine arrival = new SubtitleLine("ALEX", "Ten years... I thought I'd never see this place again.", 4f);
        [SerializeField] private SubtitleLine whoIsThat = new SubtitleLine("ALEX", "Who is that?", 2.5f);
        [SerializeField] private SubtitleLine isThatYou = new SubtitleLine("ALEX", "Maya...? Is that you?", 3.5f);

        private const string SeenFlag = "seen:l1_arrival";
        private Transform eye;
        private Camera cam;
        private Quaternion view;
        private float fov;
        private bool driving;

        /// <summary>True while the player has no control.</summary>
        public bool Playing { get; private set; }

        private IEnumerator Start()
        {
            yield return null; // let the LevelController place the player first
            GameFlowManager flow = GameFlowManager.Instance;
            if (SkipForTests || (flow != null && flow.HasFlag(SeenFlag))) { if (watcher != null) watcher.Hide(); yield break; }
            flow?.SetFlag(SeenFlag);
            yield return Play();
        }

        /// <summary>The shot's view wins over the camera's own sway and roll.</summary>
        private void LateUpdate()
        {
            if (!driving || eye == null) return;
            eye.rotation = view;
            cam.fieldOfView = fov;
        }

        private IEnumerator Play()
        {
            PlayerController player = FindAnyObjectByType<PlayerController>();
            PlayerLook look = player != null ? player.GetComponent<PlayerLook>() : null;
            cam = player != null ? player.GetComponentInChildren<Camera>() : null;
            if (player == null || cam == null || watcher == null || firstWindow == null) yield break;

            Playing = true;
            player.MovementLocked = true;
            ShowHud(false); // a clean shot: no objective, crosshair or touch buttons
            if (look != null) look.enabled = false;
            eye = cam.transform;
            Quaternion home = eye.rotation;
            float homeFov = cam.fieldOfView;
            view = home;
            fov = homeFov;
            driving = true;

            // He stands by the car looking at the station; someone is already up there.
            watcher.Watch();
            SubtitleManager.Instance?.Play(new[] { arrival });
            yield return Wait(2.8f);

            // His gaze drifts up to the window and narrows on it.
            Quaternion from = view;
            for (float k = 0f; k < 1f; k += Time.deltaTime / 2.8f)
            {
                float e = Mathf.SmoothStep(0f, 1f, k);
                view = Quaternion.Slerp(from, Toward(watcher.Head), e);
                fov = Mathf.Lerp(homeFov, zoomFov, e);
                yield return null;
            }
            yield return Stare(() => watcher.Head, 1.2f);
            SubtitleManager.Instance?.Play(new[] { whoIsThat });
            yield return Stare(() => watcher.Head, 1.8f);

            // It turns away and walks past the second window and out of sight; his eyes follow it.
            watcher.WalkAway();
            bool asked = false;
            float t = 0f;
            Vector3 target = watcher.Head;
            while (!watcher.Gone && t < 10f)
            {
                t += Time.deltaTime;
                target = Vector3.Lerp(target, watcher.Head, 1f - Mathf.Exp(-4f * Time.deltaTime));
                view = Quaternion.Slerp(view, Toward(target), 1f - Mathf.Exp(-5f * Time.deltaTime));
                if (!asked && t > 1.4f) { asked = true; SubtitleManager.Instance?.Play(new[] { isThatYou }); }
                yield return null;
            }
            yield return Stare(() => target, 1.5f);

            // Back to his own view.
            from = view;
            float fovFrom = fov;
            for (float k = 0f; k < 1f; k += Time.deltaTime / 2.6f)
            {
                float e = Mathf.SmoothStep(0f, 1f, k);
                view = Quaternion.Slerp(from, home, e);
                fov = Mathf.Lerp(fovFrom, homeFov, e);
                yield return null;
            }
            view = home;
            fov = homeFov;
            yield return null;
            driving = false;
            cam.fieldOfView = homeFov;
            if (look != null) look.enabled = true;
            player.MovementLocked = false;
            ShowHud(true);
            Playing = false;
        }

        private static readonly string[] HudParts = { "Objective", "Crosshair", "MobileControls", "Battery" };
        private readonly System.Collections.Generic.List<GameObject> hidden = new System.Collections.Generic.List<GameObject>();

        /// <summary>Hides the HUD parts that were showing, then brings back exactly those.</summary>
        private void ShowHud(bool on)
        {
            if (on)
            {
                foreach (GameObject g in hidden) if (g != null) g.SetActive(true);
                hidden.Clear();
                return;
            }
            foreach (UI.StoryHUD hud in FindObjectsByType<UI.StoryHUD>(FindObjectsInactive.Include))
                foreach (Transform t in hud.GetComponentsInChildren<Transform>(true))
                    if (System.Array.IndexOf(HudParts, t.name) >= 0 && t.gameObject.activeSelf) { t.gameObject.SetActive(false); hidden.Add(t.gameObject); }
        }

        private Quaternion Toward(Vector3 point) => Quaternion.LookRotation(point - eye.position);

        private static IEnumerator Wait(float seconds)
        {
            for (float t = 0f; t < seconds; t += Time.deltaTime) yield return null;
        }

        /// <summary>Staring at something, with the faint drift of held breath.</summary>
        private IEnumerator Stare(System.Func<Vector3> target, float seconds)
        {
            for (float t = 0f; t < seconds; t += Time.deltaTime)
            {
                Quaternion want = Toward(target()) * Quaternion.Euler(Mathf.Sin(Time.time * 0.8f) * 0.15f, Mathf.Sin(Time.time * 0.55f) * 0.2f, 0f);
                view = Quaternion.Slerp(view, want, 1f - Mathf.Exp(-4f * Time.deltaTime));
                yield return null;
            }
        }
    }
}
