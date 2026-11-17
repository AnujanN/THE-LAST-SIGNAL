using LastSignal.Utilities;
using UnityEngine;

namespace LastSignal.GameFlow
{
    /// <summary>
    /// Trigger volume that saves progress when the player walks in.
    /// The spawn point is where the player reappears after being caught.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class Checkpoint : MonoBehaviour
    {
        [SerializeField] private string id = "checkpoint";
        [Tooltip("Where the player respawns. Defaults to this transform.")]
        [SerializeField] private Transform spawnPoint;

        public string Id => id;
        public Transform SpawnPoint => spawnPoint != null ? spawnPoint : transform;

        private void Reset() => GetComponent<Collider>().isTrigger = true;

        private void OnTriggerEnter(Collider other)
        {
            if (!PlayerUtil.IsPlayer(other)) return;
            GameFlowManager flow = GameFlowManager.Instance;
            if (flow == null || flow.Live.checkpointId == id) return;
            flow.ReachCheckpoint(id);
        }
    }
}
