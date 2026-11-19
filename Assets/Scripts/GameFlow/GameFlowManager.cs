using System;
using System.Collections;
using LastSignal.Audio;
using LastSignal.Save;
using LastSignal.Utilities;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LastSignal.GameFlow
{
    /// <summary>
    /// Persistent game controller (created automatically before the first scene):
    /// scene order, fades, checkpoints, retry, save/continue and world flags.
    /// </summary>
    public class GameFlowManager : MonoBehaviour
    {
        public const string MainMenuScene = "MainMenu";
        public const string Level1Scene = "Level01_Station";
        public const string Level2Scene = "Level02_Basement";
        public const string Level3Scene = "Level03_Quarters";
        public const string Level4Scene = "Level04_BurnedWing";
        public const string Level5Scene = "Level05_Tower";
        /// <summary>Before the five-level update the tower was "Level03_Tower"; old saves are mapped to Level 5.</summary>
        public const string LegacyTowerScene = "Level03_Tower";
        public const string EndingScene = "Ending";

        public static GameFlowManager Instance { get; private set; }

        private GameState checkpointState;
        private CanvasGroup fade;
        private float fadeSeconds = 0.6f;

        /// <summary>Live state of the current run. Restored from the checkpoint on retry.</summary>
        public GameState Live { get; private set; } = new GameState();
        public LevelController CurrentLevel { get; private set; }
        public bool IsLoading { get; private set; }
        public bool IsGameOver { get; private set; }

        public event Action<bool> GameOverChanged;
        public event Action<string> CheckpointReached;
        public event Action<string> FlagSet;
        public event Action<LevelController> LevelStarted;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            if (Instance != null) return;
            var go = new GameObject("[GameFlow]");
            DontDestroyOnLoad(go);
            Instance = go.AddComponent<GameFlowManager>();
            go.AddComponent<ObjectiveManager>();
            go.AddComponent<SubtitleManager>();
            go.AddComponent<AudioManager>();
            GameSettings.Apply();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            BuildFadeOverlay();
        }

        // ------------------------------------------------------------ flags

        public bool HasFlag(string flag) => !string.IsNullOrEmpty(flag) && Live.flags.Contains(flag);

        public void SetFlag(string flag)
        {
            if (string.IsNullOrEmpty(flag) || Live.flags.Contains(flag)) return;
            Live.flags.Add(flag);
            FlagSet?.Invoke(flag);
        }

        // ------------------------------------------------------------ level lifecycle

        /// <summary>Called by the LevelController when a playable level starts.</summary>
        public void RegisterLevel(LevelController level)
        {
            CurrentLevel = level;
            SetGameOver(false);
            string scene = level.gameObject.scene.name;
            if (Live.scene != scene)
            {
                // Fresh entry (or the scene was opened directly in the Editor).
                Live.scene = scene;
                Live.checkpointId = null;
            }

            level.ApplyState(Live);
            if (checkpointState == null || checkpointState.scene != scene) TakeCheckpoint(null);
            LevelStarted?.Invoke(level);
        }

        public void UnregisterLevel(LevelController level)
        {
            if (CurrentLevel == level) CurrentLevel = null;
        }

        public void ReachCheckpoint(string id)
        {
            if (IsGameOver) return;
            TakeCheckpoint(id);
            CheckpointReached?.Invoke(id);
        }

        private void TakeCheckpoint(string id)
        {
            if (id != null) Live.checkpointId = id;
            if (CurrentLevel != null) CurrentLevel.CaptureState(Live);
            checkpointState = Live.Clone();
            SaveManager.Save(checkpointState);
        }

        public void PlayerCaught()
        {
            if (IsGameOver || IsLoading) return;
            SetGameOver(true);
        }

        private void SetGameOver(bool value)
        {
            if (IsGameOver == value) return;
            IsGameOver = value;
            GameOverChanged?.Invoke(value);
        }

        // ------------------------------------------------------------ navigation

        public void NewGame()
        {
            SaveManager.Delete();
            Live = new GameState();
            checkpointState = null;
            Load(Level1Scene);
        }

        /// <summary>Start a level with a clean run state (tests, debug level select). Does not touch the save file.</summary>
        public void StartFresh(string scene)
        {
            Live = new GameState();
            checkpointState = null;
            Load(scene);
        }

        public bool CanContinue => SaveManager.HasSave;

        public void Continue()
        {
            if (!SaveManager.TryLoad(out GameState saved)) { NewGame(); return; }
            if (saved.scene == LegacyTowerScene) saved.scene = Level5Scene;
            checkpointState = saved;
            Live = saved.Clone();
            Load(saved.scene);
        }

        public void RetryCheckpoint()
        {
            Audio.AudioManager.Instance?.ResetForRespawn();
            if (checkpointState == null) { Load(SceneManager.GetActiveScene().name); return; }
            Live = checkpointState.Clone();
            Load(Live.scene);
        }

        /// <summary>Finish the current level and move on, keeping inventory and story flags.</summary>
        public void CompleteLevel(string nextScene)
        {
            if (IsLoading) return;
            if (CurrentLevel != null) CurrentLevel.CaptureState(Live);
            Live.scene = nextScene;
            Live.checkpointId = null;
            Live.objectiveId = null;
            Live.objectiveText = null;
            checkpointState = Live.Clone();
            if (nextScene == EndingScene) SaveManager.Delete();
            else SaveManager.Save(checkpointState);
            Load(nextScene);
        }

        public void GoToMainMenu() => Load(MainMenuScene);

        public void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        public void Load(string scene)
        {
            if (IsLoading) return;
            if (!Application.CanStreamedLevelBeLoaded(scene))
            {
                Debug.LogError("[GameFlow] Scene not in build settings: " + scene);
                return;
            }
            StartCoroutine(LoadRoutine(scene));
        }

        private IEnumerator LoadRoutine(string scene)
        {
            IsLoading = true;
            fade.blocksRaycasts = true;
            yield return Fade(1f);

            Time.timeScale = 1f;
            AudioListener.pause = false;
            SubtitleManager.Instance?.StopAll();
            ModalStack.Reset();
            SetGameOver(false);

            AsyncOperation op = SceneManager.LoadSceneAsync(scene);
            while (!op.isDone) yield return null;
            yield return null;

            yield return Fade(0f);
            fade.blocksRaycasts = false;
            IsLoading = false;
        }

        /// <summary>Fade to black and back without loading (cutscene cuts).</summary>
        public IEnumerator FadeOutIn(float holdSeconds)
        {
            yield return Fade(1f);
            yield return new WaitForSecondsRealtime(holdSeconds);
            yield return Fade(0f);
        }

        private IEnumerator Fade(float target)
        {
            float start = fade.alpha;
            for (float t = 0f; t < fadeSeconds; t += Time.unscaledDeltaTime)
            {
                fade.alpha = Mathf.Lerp(start, target, t / fadeSeconds);
                yield return null;
            }
            fade.alpha = target;
        }

        private void BuildFadeOverlay()
        {
            var canvasGo = new GameObject("FadeCanvas", typeof(Canvas), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;
            var imageGo = new GameObject("Black", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            imageGo.transform.SetParent(canvasGo.transform, false);
            var rect = (RectTransform)imageGo.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            imageGo.GetComponent<Image>().color = Color.black;
            fade = imageGo.GetComponent<CanvasGroup>();
            fade.alpha = 0f;
            fade.blocksRaycasts = false;
        }
    }
}
