using LastSignal.Audio;
using LastSignal.Interaction;
using LastSignal.Inventory;
using LastSignal.Player;
using LastSignal.Save;
using UnityEngine;

namespace LastSignal.GameFlow
{
    /// <summary>
    /// One per playable level. Applies the saved state when the level starts (spawn point,
    /// inventory, objective, flashlight rules), sets ambience and plays the intro lines.
    /// </summary>
    public class LevelController : MonoBehaviour
    {
        [Header("Identity")]
        [SerializeField] private int levelNumber = 1;
        [SerializeField] private string levelName = "The Abandoned Station";

        [Header("Start")]
        [SerializeField] private Transform startSpawn;
        [SerializeField] private string startObjectiveId = "start";
        [SerializeField] private string startObjectiveText = "Explore";
        [SerializeField] private SubtitleLine[] introLines = new SubtitleLine[0];

        [Header("Flashlight")]
        [SerializeField] private bool flashlightDrain;
        [SerializeField, Min(0f)] private float drainPerSecond = 0.6f;

        [Header("Atmosphere")]
        [SerializeField] private Ambience ambience = Ambience.IndoorHum;
        [SerializeField, Range(0f, 1f)] private float ambienceVolume = 0.6f;

        private PlayerController player;
        private PlayerLook look;
        private InventoryManager inventory;
        private FlashlightController flashlight;

        public int LevelNumber => levelNumber;
        public string LevelName => levelName;
        public PlayerController Player => player;

        private void Awake()
        {
            player = FindAnyObjectByType<PlayerController>();
            if (player != null)
            {
                look = player.GetComponent<PlayerLook>();
                inventory = player.GetComponent<InventoryManager>();
                flashlight = player.GetComponentInChildren<FlashlightController>();
            }
        }

        private void Start()
        {
            AudioManager.Instance?.SetAmbience(ambience, ambienceVolume);
            GameFlowManager.Instance?.RegisterLevel(this);

            string introFlag = "intro:" + gameObject.scene.name;
            if (introLines.Length > 0 && GameFlowManager.Instance != null && !GameFlowManager.Instance.HasFlag(introFlag))
            {
                GameFlowManager.Instance.SetFlag(introFlag);
                SubtitleManager.Instance?.Play(introLines);
            }
        }

        private void OnDestroy() => GameFlowManager.Instance?.UnregisterLevel(this);

        public void ApplyState(GameState state)
        {
            if (inventory != null) inventory.RestoreFrom(state.items);
            if (flashlight != null)
            {
                flashlight.DrainEnabled = flashlightDrain;
                flashlight.DrainPerSecond = drainPerSecond;
                if (state.flashlightCharge >= 0f) flashlight.SetCharge(state.flashlightCharge);
            }

            ObjectiveManager objectives = ObjectiveManager.Instance;
            if (objectives != null)
            {
                if (string.IsNullOrEmpty(state.objectiveId)) objectives.Set(startObjectiveId, startObjectiveText);
                else objectives.Restore(state.objectiveId, state.objectiveText);
            }

            Transform spawn = startSpawn;
            if (!string.IsNullOrEmpty(state.checkpointId))
            {
                foreach (Checkpoint cp in FindObjectsByType<Checkpoint>(FindObjectsInactive.Exclude))
                    if (cp.Id == state.checkpointId) { spawn = cp.SpawnPoint; break; }
            }
            if (spawn != null && player != null)
            {
                player.Teleport(spawn.position);
                if (look != null) look.SetAngles(spawn.eulerAngles.y, 0f);
            }
        }

        public void CaptureState(GameState state)
        {
            if (inventory != null) state.items = inventory.Snapshot();
            if (flashlight != null) state.flashlightCharge = flashlight.Charge;
            ObjectiveManager objectives = ObjectiveManager.Instance;
            if (objectives != null)
            {
                state.objectiveId = objectives.CurrentId;
                state.objectiveText = objectives.CurrentText;
            }
        }
    }
}
