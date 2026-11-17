using LastSignal.Audio;
using LastSignal.GameFlow;
using UnityEngine;

namespace LastSignal.Interaction
{
    /// <summary>Recharges the flashlight. Stays used after checkpoint reloads.</summary>
    public class BatteryPickup : MonoBehaviour, IInteractable
    {
        [SerializeField] private string saveId = "battery";
        [SerializeField, Min(1f)] private float charge = 40f;

        private string Flag => "taken:" + saveId;

        private void Start()
        {
            if (WorldFlags.Has(Flag)) gameObject.SetActive(false);
        }

        public string GetPrompt(PlayerInteractor interactor) => "Take Battery";

        public bool CanInteract(PlayerInteractor interactor) => isActiveAndEnabled;

        public void Interact(PlayerInteractor interactor)
        {
            if (interactor.Flashlight != null) interactor.Flashlight.AddCharge(charge);
            interactor.ShowMessage("Flashlight recharged");
            AudioManager.Instance?.PlayAt(PlaceholderClips.Beep, transform.position, 0.6f);
            WorldFlags.Set(Flag);
            gameObject.SetActive(false);
        }
    }
}
