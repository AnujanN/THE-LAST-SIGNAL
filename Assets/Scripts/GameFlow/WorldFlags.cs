namespace LastSignal.GameFlow
{
    /// <summary>Null-safe shortcuts to the persistent world flags (work in test scenes too).</summary>
    public static class WorldFlags
    {
        public static bool Has(string flag) => GameFlowManager.Instance != null && GameFlowManager.Instance.HasFlag(flag);

        public static void Set(string flag) => GameFlowManager.Instance?.SetFlag(flag);
    }
}
