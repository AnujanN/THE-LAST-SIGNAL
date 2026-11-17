using LastSignal.Interaction;
using LastSignal.Utilities;
using UnityEngine;

namespace LastSignal.GameFlow
{
    /// <summary>Trigger at the end of a level: saves progress and loads the next scene.</summary>
    [RequireComponent(typeof(Collider))]
    public class LevelExit : MonoBehaviour
    {
        [SerializeField] private string nextScene = GameFlowManager.Level2Scene;
        [Tooltip("Exit only works once this flag is set (optional).")]
        [SerializeField] private string requiredFlag;
        [SerializeField] private string notReadyMessage = "Not yet.";

        private bool used;

        private void Reset() => GetComponent<Collider>().isTrigger = true;

        private void OnTriggerEnter(Collider other)
        {
            if (used || !PlayerUtil.IsPlayer(other)) return;
            GameFlowManager flow = GameFlowManager.Instance;
            if (flow == null) return;
            if (!string.IsNullOrEmpty(requiredFlag) && !flow.HasFlag(requiredFlag))
            {
                other.GetComponentInParent<PlayerInteractor>()?.ShowMessage(notReadyMessage);
                return;
            }
            used = true;
            flow.CompleteLevel(nextScene);
        }
    }
}
