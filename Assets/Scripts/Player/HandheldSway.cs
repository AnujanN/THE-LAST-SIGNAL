using UnityEngine;

namespace LastSignal.Player
{
    /// <summary>
    /// A hand-held light lags slightly behind fast turns and settles back, so the beam feels carried
    /// rather than bolted to the camera.
    /// </summary>
    [DefaultExecutionOrder(70)]
    public class HandheldSway : MonoBehaviour
    {
        [Tooltip("Higher = stiffer (follows the view faster).")]
        [SerializeField] private float sharpness = 14f;
        [Tooltip("Never lag further than this from the view direction.")]
        [SerializeField] private float maxLagDegrees = 12f;

        private Quaternion baseLocal;
        private Quaternion current;
        private bool initialised;

        private void Awake() => baseLocal = transform.localRotation;

        private void OnEnable() => initialised = false;

        private void LateUpdate()
        {
            Transform parent = transform.parent;
            if (parent == null) return;
            Quaternion target = parent.rotation * baseLocal;
            if (!initialised) { current = target; initialised = true; }
            current = Quaternion.Slerp(current, target, 1f - Mathf.Exp(-sharpness * Time.deltaTime));
            if (Quaternion.Angle(current, target) > maxLagDegrees)
                current = Quaternion.RotateTowards(target, current, maxLagDegrees);
            transform.rotation = current;
        }
    }
}
