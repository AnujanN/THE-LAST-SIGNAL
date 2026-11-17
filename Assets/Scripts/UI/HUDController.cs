using LastSignal.Interaction;
using LastSignal.Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LastSignal.UI
{
    /// <summary>
    /// Minimal in-game HUD: interaction dot, contextual prompt, short feedback messages
    /// and the flashlight battery bar.
    /// </summary>
    public class HUDController : MonoBehaviour
    {
        [Tooltip("Found automatically in the scene if left empty.")]
        [SerializeField] private PlayerInteractor interactor;
        [SerializeField] private FlashlightController flashlight;

        [Header("Crosshair")]
        [SerializeField] private Image crosshair;
        [SerializeField] private Color crosshairIdle = new Color(1f, 1f, 1f, 0.35f);
        [SerializeField] private Color crosshairActive = new Color(1f, 0.9f, 0.6f, 0.95f);
        [SerializeField] private float crosshairIdleSize = 8f;
        [SerializeField] private float crosshairActiveSize = 16f;

        [Header("Prompt and messages")]
        [SerializeField] private TMP_Text promptText;
        [SerializeField] private TMP_Text messageText;
        [SerializeField] private CanvasGroup messageGroup;
        [SerializeField] private float messageSeconds = 2f;
        [SerializeField] private float messageFadeSeconds = 0.4f;

        [Header("Battery")]
        [SerializeField] private GameObject batteryRoot;
        [SerializeField] private Image batteryFill;
        [SerializeField] private Color batteryOnColor = new Color(1f, 0.95f, 0.75f, 0.9f);
        [SerializeField] private Color batteryOffColor = new Color(1f, 1f, 1f, 0.35f);
        [SerializeField] private Color batteryLowColor = new Color(1f, 0.35f, 0.25f, 0.9f);

        private float messageTimer;
        private string keyHint;

        private void Awake()
        {
            if (interactor == null) interactor = FindAnyObjectByType<PlayerInteractor>();
            if (flashlight == null && interactor != null) flashlight = interactor.GetComponentInChildren<FlashlightController>();
            if (flashlight == null) flashlight = FindAnyObjectByType<FlashlightController>();
            // Keyboard hint only where there is no touch UI.
            keyHint = Application.isMobilePlatform ? string.Empty : "[E]  ";
            if (messageGroup != null) messageGroup.alpha = 0f;
        }

        private void OnEnable()
        {
            if (interactor != null)
            {
                interactor.TargetChanged += OnTargetChanged;
                interactor.Interacted += OnInteracted;
                interactor.Message += ShowMessage;
            }
            if (flashlight != null)
            {
                flashlight.OwnershipChanged += OnFlashlightOwnership;
                flashlight.Toggled += OnFlashlightToggled;
                flashlight.ChargeChanged += OnChargeChanged;
            }
        }

        private void OnDisable()
        {
            if (interactor != null)
            {
                interactor.TargetChanged -= OnTargetChanged;
                interactor.Interacted -= OnInteracted;
                interactor.Message -= ShowMessage;
            }
            if (flashlight != null)
            {
                flashlight.OwnershipChanged -= OnFlashlightOwnership;
                flashlight.Toggled -= OnFlashlightToggled;
                flashlight.ChargeChanged -= OnChargeChanged;
            }
        }

        private void Start()
        {
            OnTargetChanged(interactor != null ? interactor.Current : null);
            RefreshBattery();
        }

        private void Update()
        {
            if (messageTimer <= 0f || messageGroup == null) return;
            messageTimer -= Time.unscaledDeltaTime;
            messageGroup.alpha = Mathf.Clamp01(messageTimer / messageFadeSeconds);
        }

        public void ShowMessage(string text)
        {
            if (messageText == null) return;
            messageText.text = text;
            messageTimer = messageSeconds + messageFadeSeconds;
            if (messageGroup != null) messageGroup.alpha = 1f;
        }

        private void OnInteracted(IInteractable target) => OnTargetChanged(interactor.Current);

        private void OnTargetChanged(IInteractable target)
        {
            bool has = target != null;
            if (promptText != null)
            {
                promptText.gameObject.SetActive(has);
                if (has) promptText.text = keyHint + target.GetPrompt(interactor);
            }
            if (crosshair != null)
            {
                crosshair.color = has ? crosshairActive : crosshairIdle;
                float s = has ? crosshairActiveSize : crosshairIdleSize;
                crosshair.rectTransform.sizeDelta = new Vector2(s, s);
            }
        }

        private void OnFlashlightOwnership(bool owned) => RefreshBattery();
        private void OnFlashlightToggled(bool on) => RefreshBattery();
        private void OnChargeChanged(float charge01) => RefreshBattery();

        private void RefreshBattery()
        {
            bool owned = flashlight != null && flashlight.IsOwned;
            if (batteryRoot != null) batteryRoot.SetActive(owned);
            if (!owned || batteryFill == null) return;
            float c = flashlight.Charge01;
            batteryFill.fillAmount = c;
            batteryFill.color = !flashlight.IsOn ? batteryOffColor : c < 0.2f ? batteryLowColor : batteryOnColor;
        }
    }
}
