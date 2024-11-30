namespace SubTerminalEX {
    /// <summary>
    /// When this hook should run
    /// </summary>
    public enum STEHookTarget {
        /// <summary>
        /// Before the original command
        /// </summary>
        Before = 0,
        /// <summary>
        /// After the original command
        /// </summary>
        After = 1,
        /// <summary>
        /// After the original command
        /// </summary>
        Default = After,
    }
}