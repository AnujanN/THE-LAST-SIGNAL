using System;
using System.Collections.Generic;
using LastSignal.Inventory;
using UnityEngine;

namespace LastSignal.Save
{
    /// <summary>
    /// Everything needed to resume play: level, checkpoint, world flags (doors opened,
    /// items taken, puzzles solved), inventory, objective and flashlight charge.
    /// </summary>
    [Serializable]
    public class GameState
    {
        public string scene;
        public string checkpointId;
        public List<string> flags = new List<string>();
        public List<InventoryManager.Entry> items = new List<InventoryManager.Entry>();
        public string objectiveId;
        public string objectiveText;
        public float flashlightCharge = -1f; // -1 = use the level default

        public GameState Clone() => JsonUtility.FromJson<GameState>(JsonUtility.ToJson(this));
    }
}
