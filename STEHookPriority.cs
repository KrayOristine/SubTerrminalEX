namespace SubTerminalEX {
    /// <summary>
    /// Priority for hook
    /// </summary>
    public enum STEHookPriority {
        /// <summary>
        /// Reserved, do not attempt to do this
        /// </summary>
        None = int.MinValue,

        /// <summary>
        /// Run last
        /// </summary>
        Last = 0,
        /// <summary>
        /// As it name suggest
        /// </summary>
        ExtremelyLow = 100,
        /// <summary>
        /// As it name suggest
        /// </summary>
        VeryLow = 250,
        /// <summary>
        /// As it name suggest
        /// </summary>
        Low = 400,
        /// <summary>
        /// As it name suggest
        /// </summary>
        Default = 500,
        /// <summary>
        /// As it name suggest
        /// </summary>
        High = 600,
        /// <summary>
        /// As it name suggest
        /// </summary>
        VeryHigh = 750,
        /// <summary>
        /// As it name suggest
        /// </summary>
        ExtremelyHigh = 900,
        /// <summary>
        /// Run first
        /// </summary>
        First = 1000,
    }
}