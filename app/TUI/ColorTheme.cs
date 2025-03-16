namespace PowerSupplyApp.TUI
{
    public class ColorTheme
    {
        /// <summary>
        /// The name associated with the theme instance.
        /// </summary>
        public string Name = string.Empty;

        /// <summary>
        /// The scheme to apply during normal operation.
        /// </summary>
        public ColorScheme Normal { get; set; } = ColorSchemes.Grey;

        /// <summary>
        /// The scheme to apply during normal-locked operation.
        /// </summary>
        public ColorScheme Locked { get; set; } = ColorSchemes.Grey;

        /// <summary>
        /// The scheme to apply during faulted operation.
        /// </summary>
        public ColorScheme Faulted { get; set; } = ColorSchemes.Grey;
    }
}
