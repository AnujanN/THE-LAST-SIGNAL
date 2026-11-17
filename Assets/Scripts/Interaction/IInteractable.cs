namespace LastSignal.Interaction
{
    /// <summary>
    /// Shared contract for anything the player can use: doors, pickups, notes, radios, puzzle parts.
    /// Put the component on the object (or a parent) that owns the collider the player looks at.
    /// </summary>
    public interface IInteractable
    {
        /// <summary>Short action text, e.g. "Pick up Flashlight". Return a cached string; called often.</summary>
        string GetPrompt(PlayerInteractor interactor);

        /// <summary>False hides the prompt and ignores the interact button.</summary>
        bool CanInteract(PlayerInteractor interactor);

        void Interact(PlayerInteractor interactor);
    }
}
