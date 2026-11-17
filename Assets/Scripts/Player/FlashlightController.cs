using System;
using LastSignal.Inventory;
using LastSignal.Utilities;
using UnityEngine;

namespace LastSignal.Player
{
    /// <summary>
    /// Flashlight toggle with optional battery drain (off in Level 1, on in Levels 2 and 3).
    /// Can require an inventory item (e.g. "flashlight") before it works.
    /// </summary>
    public class FlashlightController : MonoBehaviour
    {
        [SerializeField] private Light spotLight;
        [SerializeField] private PlayerInputRouter input;
        [SerializeField] private InventoryManager inventory;
        [SerializeField] private AudioSource audioSource;

        [Header("Ownership")]
        [Tooltip("If set, the flashlight only works once this item is in the inventory.")]
        [SerializeField] private string requiredItemId = "flashlight";
        [SerializeField] private bool startOn;

        [Header("Battery")]
        [SerializeField] private bool drainEnabled;
        [SerializeField, Min(1f)] private float maxCharge = 100f;
        [Tooltip("Charge used per second while on.")]
        [SerializeField, Min(0f)] private float drainPerSecond = 0.8f;
        [SerializeField, Min(0f)] private float startCharge = 100f;

        [Header("Feedback")]
        [SerializeField] private AudioClip toggleSound;

        private float charge;
        private float baseIntensity;

        public bool IsOn { get; private set; }
        public bool IsOwned => string.IsNullOrEmpty(requiredItemId) || (inventory != null && inventory.Has(requiredItemId));
        public float Charge01 => charge / maxCharge;
        public bool DrainEnabled { get => drainEnabled; set => drainEnabled = value; }

        public event Action<bool> Toggled;
        public event Action<bool> OwnershipChanged;
        public event Action<float> ChargeChanged;

        private void Awake()
        {
            if (input == null) input = GetComponentInParent<PlayerInputRouter>();
            if (inventory == null) inventory = GetComponentInParent<InventoryManager>();
            if (spotLight != null) baseIntensity = spotLight.intensity;
            charge = Mathf.Clamp(startCharge, 0f, maxCharge);
        }

        private void OnEnable()
        {
            if (inventory != null)
            {
                inventory.ItemAdded += OnInventoryChanged;
                inventory.ItemRemoved += OnInventoryChanged;
            }
        }

        private void OnDisable()
        {
            if (inventory != null)
            {
                inventory.ItemAdded -= OnInventoryChanged;
                inventory.ItemRemoved -= OnInventoryChanged;
            }
        }

        private void Start()
        {
            SetOn(startOn && IsOwned, playSound: false);
        }

        private void Update()
        {
            if (input != null && input.FlashlightPressed) Toggle();

            if (IsOn && drainEnabled && drainPerSecond > 0f)
            {
                charge = Mathf.Max(0f, charge - drainPerSecond * Time.deltaTime);
                ChargeChanged?.Invoke(Charge01);
                if (charge <= 0f) SetOn(false);
            }
        }

        public void Toggle() => SetOn(!IsOn);

        public void SetOn(bool on, bool playSound = true)
        {
            if (on && (!IsOwned || charge <= 0f)) on = false;
            bool changed = on != IsOn;
            IsOn = on;
            if (spotLight != null)
            {
                spotLight.enabled = on;
                spotLight.intensity = baseIntensity;
            }
            if (playSound && audioSource != null)
                audioSource.PlayOneShot(toggleSound != null ? toggleSound : PlaceholderAudio.Click, 0.6f);
            if (changed) Toggled?.Invoke(IsOn);
        }

        public void AddCharge(float amount)
        {
            charge = Mathf.Clamp(charge + amount, 0f, maxCharge);
            ChargeChanged?.Invoke(Charge01);
        }

        private void OnInventoryChanged(InventoryManager.Entry entry)
        {
            if (entry.id != requiredItemId) return;
            OwnershipChanged?.Invoke(IsOwned);
            if (!IsOwned) SetOn(false, playSound: false);
        }
    }
}
