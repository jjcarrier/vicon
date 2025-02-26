namespace PowerSupplyApp.TUI
{
    public static class ColorThemes
    {
        public static readonly ColorTheme Classic = new()
        {
            Normal = ColorSchemes.ClassicRed,
            Locked = ColorSchemes.ClassicGold,
            Faulted = ColorSchemes.FaultedDarkRed
        };

        public static readonly ColorTheme BlackAndWhite = new()
        {
            Normal = ColorSchemes.BlackAndWhite,
            Locked = ColorSchemes.BlackAndWhite,
            Faulted = ColorSchemes.BlackAndWhite
        };

        public static readonly ColorTheme Grey = new()
        {
            Normal = ColorSchemes.Grey,
            Locked = ColorSchemes.Grey,
            Faulted = ColorSchemes.FaultedGrey
        };

        public static readonly ColorTheme DarkGreen = new()
        {
            Normal = ColorSchemes.DarkGreen,
            Locked = ColorSchemes.DarkGreen,
            Faulted = ColorSchemes.FaultedDarkGreen
        };

        public static readonly ColorTheme DarkRed = new()
        {
            Normal = ColorSchemes.DarkRed,
            Locked = ColorSchemes.DarkRed,
            Faulted = ColorSchemes.FaultedDarkRed
        };

        public static readonly ColorTheme DarkMagenta = new()
        {
            Normal = ColorSchemes.DarkMagenta,
            Locked = ColorSchemes.DarkMagenta,
            Faulted = ColorSchemes.FaultedDarkMagenta
        };

        public static readonly ColorTheme Cyan = new()
        {
            Normal = ColorSchemes.Cyan,
            Locked = ColorSchemes.Cyan,
            Faulted = ColorSchemes.FaultedCyan
        };

        public static readonly ColorTheme Gold = new()
        {
            Normal = ColorSchemes.Gold,
            Locked = ColorSchemes.Gold,
            Faulted = ColorSchemes.FaultedGold
        };

        public static readonly ColorTheme Blue = new()
        {
            Normal = ColorSchemes.Blue,
            Locked = ColorSchemes.Blue,
            Faulted = ColorSchemes.FaultedBlue
        };

        public static readonly ColorTheme BlueViolet = new()
        {
            Normal = ColorSchemes.BlueViolet,
            Locked = ColorSchemes.BlueViolet,
            Faulted = ColorSchemes.FaultedBlueViolet
        };
    }
}
