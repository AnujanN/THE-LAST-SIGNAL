using UnityEngine;

namespace LastSignal.Environment
{
    /// <summary>
    /// The figure upstairs at Northpoint: it stands at the first window looking down at Alex, then turns and walks
    /// along the room behind the facade, past the second window, and out of sight. A dark silhouette against the dim
    /// light at the back of the room; nobody can tell who it is. ArrivalCutscene drives it.
    /// </summary>
    public class WindowWatcher : MonoBehaviour
    {
        [SerializeField] private Transform figure;
        [SerializeField] private Animator animator;
        [Tooltip("Where it stands watching (at the first window).")]
        [SerializeField] private Transform watchPoint;
        [Tooltip("Where it walks to (past the second window, out of sight); it disappears there.")]
        [SerializeField] private Transform exitPoint;
        [SerializeField] private float walkSpeed = 1.05f;
        [SerializeField] private float turnSeconds = 0.6f;

        private static readonly int WalkingId = Animator.StringToHash("Walking");
        private bool walking;
        private float turn;
        private Quaternion turnFrom, turnTo;

        /// <summary>Head height of the figure, for the camera to follow.</summary>
        public Vector3 Head => figure != null ? figure.position + Vector3.up * 1.45f : transform.position;
        public bool Gone => figure == null || !figure.gameObject.activeSelf;
        public float X => figure != null ? figure.position.x : 0f;

        private void Awake() => Hide();

        /// <summary>Standing at the first window, facing out at Alex.</summary>
        public void Watch()
        {
            if (figure == null || watchPoint == null) return;
            figure.gameObject.SetActive(true);
            // The model faces its -Z: look out of the window (towards -Z of the world).
            figure.SetPositionAndRotation(watchPoint.position, Quaternion.LookRotation(Vector3.forward));
            walking = false;
            if (animator != null) animator.SetBool(WalkingId, false);
        }

        /// <summary>Turns away and walks off past the second window.</summary>
        public void WalkAway()
        {
            if (figure == null || exitPoint == null) return;
            Vector3 dir = exitPoint.position - figure.position;
            dir.y = 0f;
            turnFrom = figure.rotation;
            turnTo = Quaternion.LookRotation(-dir.normalized);
            turn = 0f;
            walking = true;
            if (animator != null) animator.SetBool(WalkingId, true);
        }

        public void Hide()
        {
            walking = false;
            if (figure != null) figure.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (!walking || figure == null) return;
            turn += Time.deltaTime / turnSeconds;
            figure.rotation = Quaternion.Slerp(turnFrom, turnTo, Mathf.SmoothStep(0f, 1f, turn));
            if (turn < 0.5f) return; // turn before stepping off
            figure.position = Vector3.MoveTowards(figure.position, exitPoint.position, walkSpeed * Time.deltaTime);
            if ((figure.position - exitPoint.position).sqrMagnitude < 0.0004f) Hide();
        }
    }
}
