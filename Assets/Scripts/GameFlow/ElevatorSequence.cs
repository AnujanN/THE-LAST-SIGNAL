using System.Collections;
using LastSignal.Audio;
using LastSignal.Enemy;
using LastSignal.Player;
using UnityEngine;

namespace LastSignal.GameFlow
{
    /// <summary>
    /// End of Level 2: the elevator doors close while The Echo appears outside, then the
    /// level ends. Started by the elevator button once the generator is running.
    /// </summary>
    public class ElevatorSequence : MonoBehaviour
    {
        [SerializeField] private Transform leftDoor;
        [SerializeField] private Transform rightDoor;
        [Tooltip("How far each door slides to close (local X).")]
        [SerializeField] private float slideDistance = 0.8f;
        [SerializeField] private float closeSeconds = 3f;
        [SerializeField] private EchoAIController echo;
        [SerializeField] private Transform echoAppearPoint;
        [SerializeField] private string nextScene = GameFlowManager.Level3Scene;

        private bool running;

        public void Begin()
        {
            if (running) return;
            running = true;
            StartCoroutine(Run());
        }

        private IEnumerator Run()
        {
            PlayerController player = FindAnyObjectByType<PlayerController>();
            if (player != null) player.MovementLocked = true;
            AudioManager.Instance?.PlayUI(PlaceholderClips.Buzz, 0.8f);
            AudioManager.Instance?.PlayUI(PlaceholderClips.LiftDoors, 0.9f);
            yield return new WaitForSeconds(0.6f);

            if (echo != null && echoAppearPoint != null)
            {
                echo.Deactivate();
                echo.gameObject.SetActive(true);
                echo.WarpTo(echoAppearPoint.position);
                echo.transform.rotation = echoAppearPoint.rotation;
            }

            Vector3 l0 = leftDoor != null ? leftDoor.localPosition : Vector3.zero;
            Vector3 r0 = rightDoor != null ? rightDoor.localPosition : Vector3.zero;
            bool screamed = false;
            for (float t = 0f; t < closeSeconds; t += Time.deltaTime)
            {
                float k = Mathf.SmoothStep(0f, 1f, t / closeSeconds);
                if (leftDoor != null) leftDoor.localPosition = l0 + Vector3.right * slideDistance * k;
                if (rightDoor != null) rightDoor.localPosition = r0 - Vector3.right * slideDistance * k;
                if (!screamed && k > 0.4f)
                {
                    screamed = true;
                    AudioManager.Instance?.PlayUI(PlaceholderClips.EchoWhisper, 1f);
                    SubtitleManager.Instance?.Say("THE ECHO", "Alex... stay with me...", 2.5f);
                }
                AudioManager.Instance?.ReportThreat(1f);
                yield return null;
            }
            AudioManager.Instance?.PlayUI(PlaceholderClips.Thud, 1f);
            yield return new WaitForSeconds(1.5f);
            GameFlowManager.Instance?.CompleteLevel(nextScene);
        }
    }
}
