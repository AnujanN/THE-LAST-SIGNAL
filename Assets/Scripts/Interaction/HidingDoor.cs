using System.Collections;
using LastSignal.Audio;
using UnityEngine;

namespace LastSignal.Interaction
{
    /// <summary>
    /// The door of a locker or wardrobe: it swings open as the player climbs in or out and pulls shut
    /// behind them with a soft knock, so hiding feels physical instead of a teleport.
    /// </summary>
    public class HidingDoor : MonoBehaviour
    {
        [SerializeField] private HidingSpot spot;
        [SerializeField] private Transform hinge;
        [SerializeField] private float openAngle = -100f;

        private Quaternion closed;
        private bool occupied;
        private Coroutine swing;

        private void Awake()
        {
            if (spot == null) spot = GetComponent<HidingSpot>();
            if (hinge != null) closed = hinge.localRotation;
        }

        private void OnEnable() => HidingSpot.HiddenChanged += OnHidden;

        private void OnDisable()
        {
            HidingSpot.HiddenChanged -= OnHidden;
            if (hinge != null) hinge.localRotation = closed;
        }

        private void OnHidden(bool hidden)
        {
            bool mine = hidden ? HidingSpot.Current == spot : occupied;
            if (!mine || hinge == null) return;
            occupied = hidden;
            if (swing != null) StopCoroutine(swing);
            swing = StartCoroutine(Swing());
        }

        private IEnumerator Swing()
        {
            Quaternion open = closed * Quaternion.Euler(0f, openAngle, 0f);
            Quaternion from = hinge.localRotation;
            for (float t = 0f; t < 0.14f; t += Time.deltaTime)
            {
                hinge.localRotation = Quaternion.Slerp(from, open, t / 0.14f);
                yield return null;
            }
            hinge.localRotation = open;
            yield return new WaitForSeconds(0.22f);
            for (float t = 0f; t < 0.3f; t += Time.deltaTime)
            {
                hinge.localRotation = Quaternion.Slerp(open, closed, Mathf.SmoothStep(0f, 1f, t / 0.3f));
                yield return null;
            }
            hinge.localRotation = closed;
            AudioManager.Instance?.PlayAt(PlaceholderClips.Thud, hinge.position, 0.35f);
            swing = null;
        }
    }
}
