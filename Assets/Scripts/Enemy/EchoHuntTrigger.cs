using System.Collections;
using UnityEngine;

namespace LastSignal.Enemy
{
    /// <summary>
    /// Scripted moment: after a short delay The Echo appears at a set point and hunts the player
    /// (it always knows roughly where they are unless they hide). Used for escape runs.
    /// </summary>
    public class EchoHuntTrigger : MonoBehaviour
    {
        [SerializeField] private EchoAIController echo;
        [Tooltip("Where The Echo appears. Keep it well away from the player so the run is fair.")]
        [SerializeField] private Transform spawn;
        [SerializeField, Min(0f)] private float delay = 2.5f;

        private bool started;

        public void Begin()
        {
            if (started || echo == null) return;
            started = true;
            StartCoroutine(Run());
        }

        private IEnumerator Run()
        {
            yield return new WaitForSeconds(delay);
            if (echo == null) yield break;
            echo.gameObject.SetActive(true);
            echo.Deactivate();
            if (spawn != null) echo.WarpTo(spawn.position);
            echo.StartHunt();
        }
    }
}
