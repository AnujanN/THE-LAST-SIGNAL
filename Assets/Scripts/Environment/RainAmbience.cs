using LastSignal.Audio;
using LastSignal.Interaction;
using LastSignal.Player;
using UnityEngine;

namespace LastSignal.Environment
{
    /// <summary>
    /// Makes the storm loud out in the yard and in the open doorway, then fade (and go muffled) little by little the
    /// further Alex walks into the building from the entrance; quieter still once the front door is shut, and only a
    /// faint hiss down in the basement. It sets AudioManager's "outdoors" amount every frame.
    /// </summary>
    public class RainAmbience : MonoBehaviour
    {
        [Tooltip("The middle of the entrance doorway.")]
        [SerializeField] private Transform entrance;
        [Tooltip("The front door (shut = the storm is cut down more).")]
        [SerializeField] private Door frontDoor;
        [Tooltip("Points on this side of the entrance plane (along the entrance's -Z) are outdoors.")]
        [SerializeField] private float fadeDistance = 16f;
        [SerializeField, Range(0f, 1f)] private float deepInside = 0.12f;
        [SerializeField, Range(0f, 1f)] private float doorShutFactor = 0.55f;
        [Tooltip("Below this height (the basement) the storm is almost gone.")]
        [SerializeField] private float basementBelow = -0.6f;

        private Transform listener;
        private float current = 1f;

        private void Start()
        {
            PlayerController p = FindAnyObjectByType<PlayerController>();
            if (p != null) listener = p.transform;
        }

        private void OnDestroy() => AudioManager.Instance?.SetOutdoors(1f);

        private void Update()
        {
            if (listener == null || entrance == null || AudioManager.Instance == null) return;
            Vector3 local = entrance.InverseTransformPoint(listener.position);
            float target;
            if (local.z <= 0f) target = 1f; // out in the yard
            else
            {
                float d = Vector3.Distance(listener.position, entrance.position);
                target = Mathf.Lerp(1f, deepInside, Mathf.SmoothStep(0f, 1f, d / fadeDistance));
                if (frontDoor != null && !frontDoor.IsOpen) target *= Mathf.Lerp(1f, doorShutFactor, Mathf.Clamp01((d - 0.8f) / 2f));
                if (listener.position.y < basementBelow) target = Mathf.Min(target, 0.04f);
            }
            current = Mathf.MoveTowards(current, target, Time.deltaTime * 0.8f);
            AudioManager.Instance.SetOutdoors(current);
        }
    }
}
