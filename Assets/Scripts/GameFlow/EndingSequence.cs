using System.Collections;
using LastSignal.Audio;
using UnityEngine;

namespace LastSignal.GameFlow
{
    /// <summary>
    /// Ending scene: sunrise over Maya's memorial, Alex's last line, THE END, then credits and a
    /// button back to the main menu. Not skippable before the credits.
    /// </summary>
    public class EndingSequence : MonoBehaviour
    {
        [SerializeField] private Transform cameraRig;
        [SerializeField] private Transform cameraStart;
        [SerializeField] private Transform cameraEnd;
        [SerializeField] private float dollySeconds = 24f;
        [SerializeField] private SubtitleLine[] lines = new SubtitleLine[0];
        [SerializeField] private CanvasGroup theEnd;
        [SerializeField] private RectTransform credits;
        [SerializeField] private float creditsSpeed = 70f;
        [SerializeField] private float creditsEndY = 1400f;
        [SerializeField] private GameObject menuButton;

        private void Start()
        {
            Time.timeScale = 1f;
            AudioManager.Instance?.SetAmbience(Ambience.Dawn, 0.5f);
            if (theEnd != null) theEnd.alpha = 0f;
            if (credits != null) credits.gameObject.SetActive(false);
            if (menuButton != null) menuButton.SetActive(false);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            StartCoroutine(Dolly());
            StartCoroutine(Run());
        }

        private IEnumerator Dolly()
        {
            if (cameraRig == null || cameraStart == null || cameraEnd == null) yield break;
            for (float t = 0f; t < dollySeconds; t += Time.deltaTime)
            {
                float k = Mathf.SmoothStep(0f, 1f, t / dollySeconds);
                cameraRig.SetPositionAndRotation(
                    Vector3.Lerp(cameraStart.position, cameraEnd.position, k),
                    Quaternion.Slerp(cameraStart.rotation, cameraEnd.rotation, k));
                yield return null;
            }
        }

        private IEnumerator Run()
        {
            yield return new WaitForSeconds(2f);
            bool done = false;
            if (SubtitleManager.Instance != null) SubtitleManager.Instance.Play(lines, () => done = true);
            else done = true;
            while (!done) yield return null;

            yield return new WaitForSeconds(1.5f);
            yield return FadeGroup(theEnd, 0f, 1f, 2.5f);
            yield return new WaitForSeconds(3.5f);
            yield return FadeGroup(theEnd, 1f, 0f, 1.5f);

            if (credits != null)
            {
                credits.gameObject.SetActive(true);
                Vector2 p = credits.anchoredPosition;
                while (credits.anchoredPosition.y < creditsEndY)
                {
                    p.y += creditsSpeed * Time.deltaTime;
                    credits.anchoredPosition = p;
                    yield return null;
                }
            }
            if (menuButton != null) menuButton.SetActive(true);
        }

        private static IEnumerator FadeGroup(CanvasGroup g, float from, float to, float seconds)
        {
            if (g == null) yield break;
            for (float t = 0f; t < seconds; t += Time.deltaTime)
            {
                g.alpha = Mathf.Lerp(from, to, t / seconds);
                yield return null;
            }
            g.alpha = to;
        }

        public void ReturnToMenu() => GameFlowManager.Instance?.GoToMainMenu();
    }
}
