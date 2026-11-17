using LastSignal.Audio;
using LastSignal.Interaction;
using UnityEngine;

namespace LastSignal.GameFlow
{
    /// <summary>
    /// Scene-side bridge to the persistent managers so level content (pickups, puzzles,
    /// trigger zones) can drive story beats from UnityEvents with a single string argument.
    /// </summary>
    public class StoryEvents : MonoBehaviour
    {
        /// <summary>"objective_id|Objective text"</summary>
        public void SetObjective(string packed) => ObjectiveManager.Instance?.SetPacked(packed);

        public void SetFlag(string flag) => GameFlowManager.Instance?.SetFlag(flag);

        /// <summary>"Speaker|Line|seconds"</summary>
        public void Say(string packed) => SubtitleManager.Instance?.SayPacked(packed);

        public void ShowMessage(string text) => FindAnyObjectByType<PlayerInteractor>()?.ShowMessage(text);

        public void Checkpoint(string id) => GameFlowManager.Instance?.ReachCheckpoint(id);

        public void PlayStatic() => AudioManager.Instance?.PlayUI(PlaceholderClips.StaticBurst, 0.5f);

        public void PlayThud() => AudioManager.Instance?.PlayUI(PlaceholderClips.Thud, 0.8f);

        public void CompleteLevel(string nextScene) => GameFlowManager.Instance?.CompleteLevel(nextScene);
    }
}
