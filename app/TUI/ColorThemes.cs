namespace PowerSupplyApp.TUI
{
    public static class ColorThemes
    {
        public static readonly ColorTheme Classic = new()
        {
            Name = "classic",
            Normal = ColorSchemes.ClassicRed,
            Locked = ColorSchemes.ClassicGold,
            Faulted = ColorSchemes.FaultedDarkRed
        };

        public static readonly ColorTheme BlackAndWhite = new()
        {
            Name = "black-and-white",
            Normal = ColorSchemes.BlackAndWhite,
            Locked = ColorSchemes.BlackAndWhite,
            Faulted = ColorSchemes.BlackAndWhite
        };

        public static readonly ColorTheme Grey = new()
        {
            Name = "grey",
            Normal = ColorSchemes.Grey,
            Locked = ColorSchemes.Grey,
            Faulted = ColorSchemes.FaultedGrey
        };

        public static readonly ColorTheme DarkGreen = new()
        {
            Name = "dark-green",
            Normal = ColorSchemes.DarkGreen,
            Locked = ColorSchemes.DarkGreen,
            Faulted = ColorSchemes.FaultedDarkGreen
        };

        public static readonly ColorTheme DarkRed = new()
        {
            Name = "dark-red",
            Normal = ColorSchemes.DarkRed,
            Locked = ColorSchemes.DarkRed,
            Faulted = ColorSchemes.FaultedDarkRed
        };

        public static readonly ColorTheme DarkMagenta = new()
        {
            Name = "dark-magenta",
            Normal = ColorSchemes.DarkMagenta,
            Locked = ColorSchemes.DarkMagenta,
            Faulted = ColorSchemes.FaultedDarkMagenta
        };

        public static readonly ColorTheme Cyan = new()
        {
            Name = "cyan",
            Normal = ColorSchemes.Cyan,
            Locked = ColorSchemes.Cyan,
            Faulted = ColorSchemes.FaultedCyan
        };

        public static readonly ColorTheme Gold = new()
        {
            Name = "gold",
            Normal = ColorSchemes.Gold,
            Locked = ColorSchemes.Gold,
            Faulted = ColorSchemes.FaultedGold
        };

        public static readonly ColorTheme Blue = new()
        {
            Name = "blue",
            Normal = ColorSchemes.Blue,
            Locked = ColorSchemes.Blue,
            Faulted = ColorSchemes.FaultedBlue
        };

        public static readonly ColorTheme BlueViolet = new()
        {
            Name = "blue-violet",
            Normal = ColorSchemes.BlueViolet,
            Locked = ColorSchemes.BlueViolet,
            Faulted = ColorSchemes.FaultedBlueViolet
        };

        public static ColorTheme GetTheme(string? name)
        {
            return name switch
            {
                "black-and-white" => BlackAndWhite,
                "grey" => Grey,
                "dark-red" => DarkRed,
                "dark-green" => DarkGreen,
                "blue" => Blue,
                "blue-violet" => BlueViolet,
                "dark-magenta" => DarkMagenta,
                "cyan" => Cyan,
                "gold" => Gold,
                _ => Classic,
            };
        }
    }
}
