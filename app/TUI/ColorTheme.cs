namespace PowerSupplyApp.TUI
{
    public class ColorTheme
    {
        // The scheme to apply during normal operation.
        public ColorScheme Normal { get; set; }

        // The scheme to apply during normal-locked operation.
        public ColorScheme Locked { get; set; }

        // The scheme to apply during faulted operation.
        public ColorScheme Faulted { get; set; }
    }
}
