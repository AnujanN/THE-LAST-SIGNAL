using System.Text;
using LastSignal.Audio;
using LastSignal.Enemy;
using LastSignal.GameFlow;
using LastSignal.Interaction;
using LastSignal.Inventory;
using LastSignal.Player;
using LastSignal.Utilities;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LastSignal.UI
{
    /// <summary>
    /// Story and state panels on the HUD: objective, subtitles, note reader, inventory,
    /// game over, level title, hidden indicator and the threat vignette.
    /// </summary>
    public class StoryHUD : MonoBehaviour
    {
        [Header("Objective")]
        [SerializeField] private TMP_Text objectiveText;
        [SerializeField] private CanvasGroup objectiveGroup;

        [Header("Subtitles")]
        [SerializeField] private GameObject subtitlePanel;
        [SerializeField] private TMP_Text subtitleText;

        [Header("Note reader")]
        [SerializeField] private GameObject notePanel;
        [SerializeField] private TMP_Text noteTitle;
        [SerializeField] private TMP_Text noteBody;
        [Tooltip("The sheet (image, title, body); hidden while a note is held up in Alex's hand.")]
        [SerializeField] private GameObject noteSheet;
        [SerializeField] private Image notePaper;
        [Tooltip("Paper sprites by style (PaperArt).")]
        [SerializeField] private Sprite[] paperSprites = new Sprite[0];
        [SerializeField] private TMP_Text noteCloseLabel;
        [SerializeField] private Image noteBackdrop;

        /// <summary>The reader was closed (a held note goes back where it was).</summary>
        public static event System.Action NoteClosed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => NoteClosed = null;

        [Header("Inventory")]
        [SerializeField] private GameObject inventoryPanel;
        [SerializeField] private TMP_Text inventoryText;

        [Header("Game over")]
        [SerializeField] private GameObject gameOverPanel;

        [Header("Level title")]
        [SerializeField] private CanvasGroup titleGroup;
        [SerializeField] private TMP_Text titleText;

        [Header("Status")]
        [SerializeField] private GameObject hiddenLabel;
        [SerializeField] private Image threatVignette;
        [Tooltip("Breath meter shown while hidden (hold Space / BREATH).")]
        [SerializeField] private GameObject breathRoot;
        [SerializeField] private Image breathFill;
        [Tooltip("Full-screen static that tears the picture when The Echo catches you.")]
        [SerializeField] private RawImage catchOverlay;

        private PlayerInputRouter router;
        private InventoryManager inventory;
        private FlashlightController flashlight;
        private PlayerBreath breath;
        private float objectiveFlash;
        private float titleTimer;
        private float gameOverDelay = -1f;
        private float catchTimer, catchDuration = 1f;
        private Texture2D staticTexture;
        private readonly StringBuilder sb = new StringBuilder(256);
        private readonly object noteOwner = new object();
        private readonly object inventoryOwner = new object();
        private readonly object gameOverOwner = new object();

        private void Awake()
        {
            router = FindAnyObjectByType<PlayerInputRouter>();
            if (router != null)
            {
                inventory = router.GetComponent<InventoryManager>();
                flashlight = router.GetComponentInChildren<FlashlightController>();
                breath = router.GetComponent<PlayerBreath>();
            }
            SetActive(subtitlePanel, false);
            SetActive(notePanel, false);
            SetActive(inventoryPanel, false);
            SetActive(gameOverPanel, false);
            SetActive(hiddenLabel, false);
            SetActive(breathRoot, false);
            if (catchOverlay != null) catchOverlay.gameObject.SetActive(false);
            if (titleGroup != null) titleGroup.alpha = 0f;
            if (threatVignette != null) threatVignette.color = new Color(1f, 1f, 1f, 0f);
        }

        private void OnEnable()
        {
            if (ObjectiveManager.Instance != null) ObjectiveManager.Instance.Changed += OnObjective;
            if (SubtitleManager.Instance != null)
            {
                SubtitleManager.Instance.LineStarted += OnLine;
                SubtitleManager.Instance.LineEnded += OnLineEnded;
            }
            if (GameFlowManager.Instance != null)
            {
                GameFlowManager.Instance.GameOverChanged += OnGameOver;
                GameFlowManager.Instance.LevelStarted += OnLevelStarted;
                GameFlowManager.Instance.CheckpointReached += OnCheckpoint;
            }
            NoteInteractable.ReadRequested += OpenNote;
            HidingSpot.HiddenChanged += OnHidden;
            EchoCatchSequence.Started += OnCaught;
            if (ObjectiveManager.Instance != null && !string.IsNullOrEmpty(ObjectiveManager.Instance.CurrentText))
                OnObjective(ObjectiveManager.Instance.CurrentText, false);
        }

        private void OnDisable()
        {
            if (ObjectiveManager.Instance != null) ObjectiveManager.Instance.Changed -= OnObjective;
            if (SubtitleManager.Instance != null)
            {
                SubtitleManager.Instance.LineStarted -= OnLine;
                SubtitleManager.Instance.LineEnded -= OnLineEnded;
            }
            if (GameFlowManager.Instance != null)
            {
                GameFlowManager.Instance.GameOverChanged -= OnGameOver;
                GameFlowManager.Instance.LevelStarted -= OnLevelStarted;
                GameFlowManager.Instance.CheckpointReached -= OnCheckpoint;
            }
            NoteInteractable.ReadRequested -= OpenNote;
            HidingSpot.HiddenChanged -= OnHidden;
            EchoCatchSequence.Started -= OnCaught;
            ModalStack.Close(noteOwner);
            ModalStack.Close(inventoryOwner);
            ModalStack.Close(gameOverOwner);
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;

            if (objectiveFlash > 0f && objectiveGroup != null)
            {
                objectiveFlash -= dt;
                objectiveGroup.alpha = 0.75f + 0.25f * Mathf.Abs(Mathf.Sin(objectiveFlash * 6f));
                if (objectiveFlash <= 0f) objectiveGroup.alpha = 0.85f;
            }

            if (titleTimer > 0f && titleGroup != null)
            {
                titleTimer -= dt;
                titleGroup.alpha = Mathf.Clamp01(titleTimer < 1f ? titleTimer : 1f);
            }

            if (gameOverDelay > 0f)
            {
                gameOverDelay -= dt;
                if (gameOverDelay <= 0f) ShowGameOver();
            }

            UpdateCatchOverlay(dt);

            if (threatVignette != null && AudioManager.Instance != null)
            {
                float t = AudioManager.Instance.ThreatLevel;
                float pulse = 0.85f + 0.15f * Mathf.Sin(Time.time * (3f + t * 5f));
                threatVignette.color = new Color(1f, 1f, 1f, t * 0.75f * pulse);
            }

            if (breathRoot != null && breathRoot.activeSelf && breath != null && breathFill != null)
            {
                breathFill.fillAmount = breath.Breath01;
                Color full = new Color(0.75f, 0.9f, 1f, 0.9f), empty = new Color(1f, 0.3f, 0.25f, 0.95f);
                breathFill.color = Color.Lerp(empty, full, breath.Breath01);
            }

            if (router == null) return;
            if (router.InventoryPressed)
            {
                if (inventoryPanel != null && inventoryPanel.activeSelf) CloseInventory();
                else if (!ModalStack.AnyOpen) OpenInventory();
            }
            if (router.PausePressed)
            {
                if (notePanel != null && notePanel.activeSelf) CloseNote();
                else if (inventoryPanel != null && inventoryPanel.activeSelf) CloseInventory();
            }
        }

        // ---------------------------------------------------------------- objective / title

        private void OnObjective(string text, bool isNew)
        {
            if (objectiveText != null) objectiveText.text = string.IsNullOrEmpty(text) ? "" : "OBJECTIVE\n<size=120%>" + text + "</size>";
            if (isNew)
            {
                objectiveFlash = 2.5f;
                if (!string.IsNullOrEmpty(text)) HudMessage.Show("New objective");
            }
        }

        private void OnLevelStarted(LevelController level)
        {
            if (titleText == null || titleGroup == null) return;
            titleText.text = "LEVEL " + level.LevelNumber + "\n<size=140%>" + level.LevelName.ToUpperInvariant() + "</size>";
            titleTimer = 4f;
        }

        private void OnCheckpoint(string id) => HudMessage.Show("Progress saved");

        private void OnHidden(bool hidden)
        {
            SetActive(hiddenLabel, hidden);
            SetActive(breathRoot, hidden && breath != null);
        }

        // ---------------------------------------------------------------- subtitles

        private void OnLine(SubtitleLine line)
        {
            if (subtitleText == null) return;
            sb.Clear();
            if (!string.IsNullOrEmpty(line.speaker))
                sb.Append("<color=").Append(SpeakerColour(line.speaker)).Append("><b>").Append(line.speaker.ToUpperInvariant()).Append(":</b></color> ");
            sb.Append(line.text);
            subtitleText.text = sb.ToString();
            SetActive(subtitlePanel, true);
        }

        private void OnLineEnded() => SetActive(subtitlePanel, false);

        private static string SpeakerColour(string speaker)
        {
            string s = speaker.ToUpperInvariant();
            if (s.Contains("MAYA")) return "#FFD9A0";
            if (s.Contains("ECHO")) return "#FF7A7A";
            if (s.Contains("RADIO")) return "#A8C8FF";
            return "#FFFFFF";
        }

        // ---------------------------------------------------------------- notes

        private void OpenNote(NoteInteractable note, bool held)
        {
            if (notePanel == null || note == null) return;
            if (noteTitle != null) { noteTitle.text = note.Title; noteTitle.color = note.Ink; }
            if (noteBody != null) { noteBody.text = note.Body; noteBody.color = note.Ink; }
            int style = note.PaperStyle;
            if (notePaper != null && style >= 0 && style < paperSprites.Length && paperSprites[style] != null) notePaper.sprite = paperSprites[style];
            // Held up in the hand: the sheet itself is in the world; only the button stays on screen.
            if (noteSheet != null) noteSheet.SetActive(!held);
            if (noteBackdrop != null) noteBackdrop.enabled = !held;
            if (noteCloseLabel != null) noteCloseLabel.text = held ? "PUT BACK" : "CLOSE";
            notePanel.SetActive(true);
            ModalStack.Open(noteOwner);
        }

        public void CloseNote()
        {
            bool wasOpen = notePanel != null && notePanel.activeSelf;
            SetActive(notePanel, false);
            ModalStack.Close(noteOwner);
            if (wasOpen) NoteClosed?.Invoke();
        }

        // ---------------------------------------------------------------- inventory

        public void OpenInventory()
        {
            if (inventoryPanel == null) return;
            sb.Clear();
            if (inventory == null || inventory.Items.Count == 0) sb.Append("Nothing yet.");
            else
                foreach (InventoryManager.Entry e in inventory.Items)
                {
                    sb.Append("•  ").Append(e.displayName);
                    if (e.count > 1) sb.Append("  x").Append(e.count);
                    sb.Append('\n');
                }
            if (flashlight != null && flashlight.IsOwned)
                sb.Append("\nFlashlight battery: ").Append(Mathf.RoundToInt(flashlight.Charge01 * 100f)).Append('%');
            // Journal: key facts from notes and recordings, so earlier clues are never lost.
            if (GameFlowManager.Instance != null)
            {
                bool header = false;
                foreach (string f in GameFlowManager.Instance.Live.flags)
                {
                    if (!f.StartsWith("clue:")) continue;
                    if (!header) { sb.Append("\n\n<b>JOURNAL</b>\n"); header = true; }
                    sb.Append("<size=80%>-  ").Append(f, 5, f.Length - 5).Append("</size>\n");
                }
            }
            if (inventoryText != null) inventoryText.text = sb.ToString();
            inventoryPanel.SetActive(true);
            ModalStack.Open(inventoryOwner);
        }

        public void CloseInventory()
        {
            SetActive(inventoryPanel, false);
            ModalStack.Close(inventoryOwner);
        }

        // ---------------------------------------------------------------- game over

        private void OnGameOver(bool over)
        {
            if (over) gameOverDelay = 1.2f;
            else
            {
                gameOverDelay = -1f;
                if (catchOverlay != null) catchOverlay.gameObject.SetActive(false);
                SetActive(gameOverPanel, false);
                ModalStack.Close(gameOverOwner);
            }
        }

        // ---------------------------------------------------------------- caught

        private void OnCaught(float seconds)
        {
            if (catchOverlay == null) return;
            if (staticTexture == null) staticTexture = MakeStatic();
            catchOverlay.texture = staticTexture;
            catchDuration = Mathf.Max(0.1f, seconds);
            catchTimer = catchDuration;
            catchOverlay.gameObject.SetActive(true);
        }

        private void UpdateCatchOverlay(float dt)
        {
            if (catchOverlay == null || !catchOverlay.gameObject.activeSelf) return;
            if (catchTimer <= 0f) return; // hold black until the game-over screen replaces it
            catchTimer -= dt;
            float p = 1f - Mathf.Clamp01(catchTimer / catchDuration);
            catchOverlay.uvRect = new Rect(Random.value, Random.value, Random.Range(1f, 2.5f), Random.Range(0.3f, 1.2f));
            if (p < 0.75f)
            {
                // Torn static, flashing between white and blood red.
                Color c = Color.Lerp(new Color(0.9f, 0.9f, 0.95f), new Color(0.75f, 0.02f, 0.02f), Random.value);
                c.a = Random.Range(0.06f, 0.42f) * (p < 0.12f ? 1.8f : 1f);
                catchOverlay.color = c;
            }
            else
            {
                catchOverlay.color = new Color(0f, 0f, 0f, Mathf.Lerp(0.5f, 1f, (p - 0.75f) / 0.25f));
            }
        }

        private static Texture2D MakeStatic()
        {
            const int w = 128, h = 128;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Repeat };
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
            {
                // Some rows are bright "tearing" lines.
                bool tear = Random.value < 0.08f;
                for (int x = 0; x < w; x++)
                {
                    byte v = (byte)(tear ? Random.Range(170, 255) : Random.Range(0, 255));
                    px[y * w + x] = new Color32(v, v, v, (byte)(tear ? 255 : Random.Range(60, 255)));
                }
            }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            return tex;
        }

        private void ShowGameOver()
        {
            gameOverDelay = -1f;
            if (catchOverlay != null) catchOverlay.gameObject.SetActive(false);
            CloseNote();
            CloseInventory();
            SetActive(gameOverPanel, true);
            ModalStack.Open(gameOverOwner);
        }

        public void RetryCheckpoint()
        {
            SetActive(gameOverPanel, false);
            ModalStack.Close(gameOverOwner);
            GameFlowManager.Instance?.RetryCheckpoint();
        }

        public void QuitToMenu()
        {
            SetActive(gameOverPanel, false);
            ModalStack.Close(gameOverOwner);
            GameFlowManager.Instance?.GoToMainMenu();
        }

        private static void SetActive(GameObject go, bool value)
        {
            if (go != null && go.activeSelf != value) go.SetActive(value);
        }
    }
}
