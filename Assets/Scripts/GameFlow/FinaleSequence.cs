using System.Collections;
using LastSignal.Audio;
using LastSignal.Enemy;
using LastSignal.Environment;
using LastSignal.Interaction;
using UnityEngine;

namespace LastSignal.GameFlow
{
    /// <summary>
    /// Level 3 finale: shutting down the transmitter makes The Echo scream, kills the lights,
    /// opens the escape route and starts a short hunt. A checkpoint is taken right after the
    /// shutdown so a catch only replays the escape.
    /// </summary>
    public class FinaleSequence : MonoBehaviour
    {
        public const string ShutdownFlag = "finale:shutdown";

        [SerializeField] private PowerGroup towerPower;
        [SerializeField] private GameObject emergencyLights;
        [SerializeField] private Door escapeDoor;
        [SerializeField] private EchoAIController echo;
        [SerializeField] private Transform echoSpawn;
        [SerializeField] private float huntDelay = 3f;
        [SerializeField] private string escapeObjectiveId = "l3_escape";
        [SerializeField] private string escapeObjectiveText = "Escape the tower";

        private void Start()
        {
            if (emergencyLights != null) emergencyLights.SetActive(false);
            // Reloaded at the finale checkpoint: restore the after-shutdown state.
            if (WorldFlagsHas(ShutdownFlag)) StartCoroutine(ApplyAftermath(replay: false));
        }

        /// <summary>Called by the shutdown lever once the final recording has played.</summary>
        public void ShutDownTransmitter()
        {
            if (WorldFlagsHas(ShutdownFlag)) return;
            WorldFlags.Set(ShutdownFlag);
            StartCoroutine(ApplyAftermath(replay: true));
        }

        private IEnumerator ApplyAftermath(bool replay)
        {
            if (towerPower != null) towerPower.PowerOff();
            if (emergencyLights != null) emergencyLights.SetActive(true);
            if (escapeDoor != null) escapeDoor.Open();

            if (replay)
            {
                AudioManager.Instance?.PlayUI(PlaceholderClips.Scream, 1f);
                SubtitleManager.Instance?.Play(new[]
                {
                    new SubtitleLine("THE ECHO", "NO! Alex, don't leave me here!", 2.5f),
                    new SubtitleLine("ALEX", "You're not her. You never were.", 2.5f),
                });
                ObjectiveManager.Instance?.Set(escapeObjectiveId, escapeObjectiveText);
                GameFlowManager.Instance?.ReachCheckpoint("finale");
            }

            yield return new WaitForSeconds(huntDelay);
            if (echo != null)
            {
                if (echoSpawn != null) echo.WarpTo(echoSpawn.position);
                echo.StartHunt();
            }
        }

        private static bool WorldFlagsHas(string flag) => WorldFlags.Has(flag);
    }
}
