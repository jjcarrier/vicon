namespace PowerSupplyApp.TUI
{
    /// <summary>
    /// Represents a notice to be displayed to the user.
    /// </summary>
    public class Notice
    {
        public string Id { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }
}
