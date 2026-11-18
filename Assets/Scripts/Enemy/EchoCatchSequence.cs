using System;
using System.Collections;
using LastSignal.Audio;
using LastSignal.Interaction;
using LastSignal.Player;
using UnityEngine;
using UnityEngine.AI;

namespace LastSignal.Enemy
{
    /// <summary>
    /// What happens when The Echo gets you. Not a punch: it is suddenly right in front of you, towering,
    /// your view is wrenched up to its face, its long hands close around your head, and it shrieks while
    /// the picture tears into static and goes black. Then the game-over screen.
    /// </summary>
    [DefaultExecutionOrder(150)]
    public class EchoCatchSequence : MonoBehaviour
    {
        [SerializeField] private Transform head;
        [Tooltip("Left arm: upper arm, forearm, hand.")]
        [SerializeField] private Transform[] leftArm = new Transform[3];
        [Tooltip("Right arm: upper arm, forearm, hand.")]
        [SerializeField] private Transform[] rightArm = new Transform[3];
        [SerializeField] private AudioSource voice;
        [SerializeField, Min(0.3f)] private float duration = 1.4f;
        [Tooltip("How far in front of the player The Echo appears.")]
        [SerializeField] private float faceDistance = 0.7f;

        /// <summary>Raised when a catch starts (the HUD tears the screen with static); argument = duration.</summary>
        public static event Action<float> Started;

        private Transform cam;
        private float progress = -1f;

        public bool Playing => progress >= 0f;

        public void Play(Action onDone)
        {
            if (Playing) return;
            StartCoroutine(Run(onDone));
        }

        private IEnumerator Run(Action onDone)
        {
            PlayerController player = FindAnyObjectByType<PlayerController>();
            if (player == null) { onDone?.Invoke(); yield break; }
            HidingSpot.Current?.Leave();
            PlayerLook look = player.GetComponent<PlayerLook>();
            PlayerInputRouter router = player.GetComponent<PlayerInputRouter>();
            Camera c = player.GetComponentInChildren<Camera>();
            cam = c != null ? c.transform : player.transform;
            player.MovementLocked = true;
            if (router != null) router.GameplayBlocked = true;

            // Appear right in front of the player, on the side it came from.
            NavMeshAgent agent = GetComponent<NavMeshAgent>();
            if (agent != null) agent.enabled = false;
            Vector3 dir = transform.position - player.transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.04f) dir = player.transform.forward;
            dir.Normalize();
            transform.position = player.transform.position + dir * faceDistance;
            transform.rotation = Quaternion.LookRotation(-dir);

            Started?.Invoke(duration);
            AudioManager.Instance?.PlayUI(PlaceholderClips.Scream, 1f);
            AudioManager.Instance?.PlayUI(PlaceholderClips.StaticBurst, 0.8f);
            if (voice != null && PlaceholderClips.EchoVoice != null)
            {
                voice.pitch = 0.7f;
                voice.PlayOneShot(PlaceholderClips.EchoVoice, 1f);
            }
            CameraMotion.Shake(0.05f, duration);

            float startYaw = look != null ? look.Yaw : 0f, startPitch = look != null ? look.Pitch : 0f;
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                progress = t / duration;
                if (look != null && head != null)
                {
                    // The view is wrenched up to its face within a fraction of a second.
                    Vector3 to = head.position - cam.position;
                    float yaw = Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg;
                    float pitch = -Mathf.Atan2(to.y, new Vector2(to.x, to.z).magnitude) * Mathf.Rad2Deg;
                    float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / 0.18f));
                    look.SetAngles(Mathf.LerpAngle(startYaw, yaw, k), Mathf.Lerp(startPitch, pitch, k));
                }
                yield return null;
            }
            progress = -1f;
            if (voice != null) voice.pitch = 1f;
            onDone?.Invoke();
        }

        private void LateUpdate()
        {
            if (!Playing || cam == null) return;
            // Violent, inhuman head jitter.
            if (head != null)
                head.rotation = Quaternion.Euler(UnityEngine.Random.Range(-14f, 14f), UnityEngine.Random.Range(-18f, 18f),
                    UnityEngine.Random.Range(-20f, 20f)) * head.rotation;
            // Long hands close in on both sides of the player's head.
            float grab = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(progress / 0.3f));
            Vector3 right = cam.right, up = cam.up, fwd = cam.forward;
            Vector3 leftTarget = cam.position - right * Mathf.Lerp(0.45f, 0.2f, grab) + fwd * 0.18f + up * 0.05f;
            Vector3 rightTarget = cam.position + right * Mathf.Lerp(0.45f, 0.2f, grab) + fwd * 0.18f + up * 0.05f;
            Reach(leftArm, leftTarget, -transform.right);
            Reach(rightArm, rightTarget, transform.right);
        }

        private void Reach(Transform[] arm, Vector3 target, Vector3 outward)
        {
            if (arm == null || arm.Length < 3 || arm[0] == null || arm[1] == null || arm[2] == null) return;
            Vector3 hint = arm[1].position + outward * 0.4f + Vector3.up * 0.2f;
            PlayerBody.SolveTwoBone(arm[0], arm[1], arm[2], target, hint, 1f);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Started = null;
    }
}
