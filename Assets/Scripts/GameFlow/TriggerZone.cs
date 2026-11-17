using LastSignal.Utilities;
using UnityEngine;
using UnityEngine.Events;

namespace LastSignal.GameFlow
{
    /// <summary>
    /// Fires an event when the player enters (scripted scares, radio messages, objective updates).
    /// One-shot zones remember they fired, so they do not repeat after a checkpoint reload.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class TriggerZone : MonoBehaviour
    {
        [SerializeField] private string id = "zone";
        [SerializeField] private bool once = true;
        [Tooltip("Only fire after this flag is set (optional).")]
        [SerializeField] private string requiredFlag;
        [SerializeField] private UnityEvent onEnter = new UnityEvent();

        private string FiredFlag => "zone:" + id;

        private void Reset() => GetComponent<Collider>().isTrigger = true;

        private void OnTriggerEnter(Collider other)
        {
            if (!PlayerUtil.IsPlayer(other)) return;
            GameFlowManager flow = GameFlowManager.Instance;
            if (flow != null)
            {
                if (!string.IsNullOrEmpty(requiredFlag) && !flow.HasFlag(requiredFlag)) return;
                if (once && flow.HasFlag(FiredFlag)) return;
                if (once) flow.SetFlag(FiredFlag);
            }
            onEnter.Invoke();
        }
    }
}
