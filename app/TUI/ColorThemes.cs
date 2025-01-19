namespace PowerSupplyApp.TUI
{
    public class ColorThemes
    {
        public static ColorTheme Classic = new ColorTheme
        {
            Normal = ColorSchemes.ClassicRed,
            Locked = ColorSchemes.ClassicGold,
            Faulted = ColorSchemes.FaultedDarkRed
        };

        public static ColorTheme BlackAndWhite = new ColorTheme
        {
            Normal = ColorSchemes.BlackAndWhite,
            Locked = ColorSchemes.BlackAndWhite,
            Faulted = ColorSchemes.BlackAndWhite
        };

        public static ColorTheme Grey = new ColorTheme
        {
            Normal = ColorSchemes.Grey,
            Locked = ColorSchemes.Grey,
            Faulted = ColorSchemes.FaultedGrey
        };

        public static ColorTheme DarkGreen = new ColorTheme
        {
            Normal = ColorSchemes.DarkGreen,
            Locked = ColorSchemes.DarkGreen,
            Faulted = ColorSchemes.FaultedDarkGreen
        };

        public static ColorTheme DarkRed = new ColorTheme
        {
            Normal = ColorSchemes.DarkRed,
            Locked = ColorSchemes.DarkRed,
            Faulted = ColorSchemes.FaultedDarkRed
        };

        public static ColorTheme DarkMagenta = new ColorTheme
        {
            Normal = ColorSchemes.DarkMagenta,
            Locked = ColorSchemes.DarkMagenta,
            Faulted = ColorSchemes.FaultedDarkMagenta
        };

        public static ColorTheme Cyan = new ColorTheme
        {
            Normal = ColorSchemes.Cyan,
            Locked = ColorSchemes.Cyan,
            Faulted = ColorSchemes.FaultedCyan
        };

        public static ColorTheme Gold = new ColorTheme
        {
            Normal = ColorSchemes.Gold,
            Locked = ColorSchemes.Gold,
            Faulted = ColorSchemes.FaultedGold
        };

        public static ColorTheme Blue = new ColorTheme
        {
            Normal = ColorSchemes.Blue,
            Locked = ColorSchemes.Blue,
            Faulted = ColorSchemes.FaultedBlue
        };

        public static ColorTheme BlueViolet = new ColorTheme
        {
            Normal = ColorSchemes.BlueViolet,
            Locked = ColorSchemes.BlueViolet,
            Faulted = ColorSchemes.FaultedBlueViolet
        };
    }
}
