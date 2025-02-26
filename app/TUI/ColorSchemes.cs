using Spectre.Console;

namespace PowerSupplyApp.TUI
{
    public static class ColorSchemes
    {
        // Schemes typically used for normal operation.
        #region Normal Schemes

        public static readonly ColorScheme ClassicRed = new()
        {
            TableAccent = Color.Grey,
            RowHeader = new Style(Color.White, null, Decoration.Bold),
            ColumnHeader = new Style(Color.White, null, Decoration.Bold),
            Bar = new Style(Color.DarkRed, Color.Grey),
            Preset = new Style(Color.Black, Color.Grey),
            PresetSelected = new Style(Color.White, Color.DarkRed),
            Caption = new Style(Color.White, null, Decoration.Dim),
            NumericData = new Style(Color.White, null),
            VoidData = new Style(Color.White, null),
            Units = new Style(Color.White, null),
            OffMode = new Style(Color.White, null, Decoration.Dim),
            ControlMode = new Style(Color.Black, Color.Grey),
            UserControl = new Style(Color.White, Color.Grey),
            FaultMessage = new Style(Color.White, Color.DarkRed)
        };

        public static readonly ColorScheme ClassicGold = new()
        {
            TableAccent = Color.Gold1,
            RowHeader = new Style(Color.White, null, Decoration.Bold),
            ColumnHeader = new Style(Color.White, null, Decoration.Bold),
            Bar = new Style(Color.Grey, Color.Grey11),
            Preset = new Style(Color.Grey, Color.Grey11),
            PresetSelected = new Style(Color.Black, Color.Grey),
            Caption = new Style(Color.White, null, Decoration.Dim),
            NumericData = new Style(Color.White, null),
            VoidData = new Style(Color.White, null),
            Units = new Style(Color.White, null),
            OffMode = new Style(Color.White, null, Decoration.Dim),
            ControlMode = new Style(Color.Black, Color.Grey),
            UserControl = new Style(Color.Black, Color.Gold1),
            FaultMessage = new Style(Color.White, Color.Gold1)
        };

        public static readonly ColorScheme BlackAndWhite = new()
        {
            TableAccent = Color.White,
            RowHeader = new Style(Color.White, null, Decoration.Bold),
            ColumnHeader = new Style(Color.White, null, Decoration.Bold),
            Bar = new Style(Color.White, Color.Black),
            Preset = new Style(Color.White, null),
            PresetSelected = new Style(Color.Black, Color.White),
            Caption = new Style(Color.White, null),
            NumericData = new Style(Color.White, null),
            VoidData = new Style(Color.White, null),
            Units = new Style(Color.White, null),
            OffMode = new Style(Color.White, null),
            ControlMode = new Style(Color.Black, Color.White),
            UserControl = new Style(Color.Black, Color.White),
            FaultMessage = new Style(Color.Black, Color.White)
        };

        public static readonly ColorScheme Grey = new()
        {
            TableAccent = Color.Grey11,
            RowHeader = new Style(Color.White, null, Decoration.Bold),
            ColumnHeader = new Style(Color.White, null, Decoration.Bold),
            Bar = new Style(Color.Grey, Color.Grey11),
            Preset = new Style(Color.Grey, Color.Grey11),
            PresetSelected = new Style(Color.Black, Color.Grey),
            Caption = new Style(Color.White, null, Decoration.Dim),
            NumericData = new Style(Color.White, null),
            VoidData = new Style(Color.White, null),
            Units = new Style(Color.White, null),
            OffMode = new Style(Color.White, null, Decoration.Dim),
            ControlMode = new Style(Color.Black, Color.Grey),
            UserControl = new Style(Color.Black, Color.Grey),
            FaultMessage = new Style(Color.Black, Color.Grey)
        };

        public static readonly ColorScheme DarkRed = new()
        {
            TableAccent = Color.Grey11,
            RowHeader = new Style(Color.White, null, Decoration.Bold),
            ColumnHeader = new Style(Color.White, null, Decoration.Bold),
            Bar = new Style(Color.DarkRed, new Color(22, 22, 22)),
            Preset = new Style(Color.White, Color.Grey11),
            PresetSelected = new Style(Color.White, Color.DarkRed),
            Caption = new Style(Color.White, null, Decoration.Dim),
            NumericData = new Style(Color.White, null),
            VoidData = new Style(Color.White, null),
            Units = new Style(Color.White, null),
            OffMode = new Style(Color.White, null, Decoration.Dim),
            ControlMode = new Style(Color.White, Color.DarkRed),
            UserControl = new Style(Color.White, Color.DarkRed),
            FaultMessage = new Style(Color.White, Color.DarkRed)
        };

        public static readonly ColorScheme DarkGreen = new()
        {
            TableAccent = Color.Grey11,
            RowHeader = new Style(Color.White, null, Decoration.Bold),
            ColumnHeader = new Style(Color.White, null, Decoration.Bold),
            Bar = new Style(Color.DarkGreen, Color.Grey11),
            Preset = new Style(Color.White, Color.Grey11),
            PresetSelected = new Style(Color.White, Color.DarkGreen),
            Caption = new Style(Color.White, null, Decoration.Dim),
            NumericData = new Style(Color.White, null),
            VoidData = new Style(Color.White, null),
            Units = new Style(Color.White, null),
            OffMode = new Style(Color.White, null, Decoration.Dim),
            ControlMode = new Style(Color.White, Color.DarkGreen),
            UserControl = new Style(Color.White, Color.DarkGreen),
            FaultMessage = new Style(Color.White, Color.DarkGreen)
        };

        public static readonly ColorScheme DarkMagenta = new()
        {
            TableAccent = Color.Grey11,
            RowHeader = new Style(Color.White, null, Decoration.Bold),
            ColumnHeader = new Style(Color.White, null, Decoration.Bold),
            Bar = new Style(Color.DarkMagenta, Color.Grey11),
            Preset = new Style(Color.White, Color.Grey11),
            PresetSelected = new Style(Color.White, Color.DarkMagenta),
            Caption = new Style(Color.White, null, Decoration.Dim),
            NumericData = new Style(Color.White, null),
            VoidData = new Style(Color.White, null),
            Units = new Style(Color.White, null),
            OffMode = new Style(Color.White, null, Decoration.Dim),
            ControlMode = new Style(Color.White, Color.DarkMagenta),
            UserControl = new Style(Color.White, Color.DarkMagenta),
            FaultMessage = new Style(Color.White, Color.DarkMagenta)
        };

        public static readonly ColorScheme Cyan = new()
        {
            TableAccent = Color.Grey11,
            RowHeader = new Style(Color.White, null, Decoration.Bold),
            ColumnHeader = new Style(Color.White, null, Decoration.Bold),
            Bar = new Style(Color.Cyan1, Color.Grey11),
            Preset = new Style(Color.White, Color.Grey11),
            PresetSelected = new Style(Color.Black, Color.Cyan1),
            Caption = new Style(Color.White, null, Decoration.Dim),
            NumericData = new Style(Color.White, null),
            VoidData = new Style(Color.White, null),
            Units = new Style(Color.White, null),
            OffMode = new Style(Color.White, null, Decoration.Dim),
            ControlMode = new Style(Color.Black, Color.Cyan1),
            UserControl = new Style(Color.Black, Color.Cyan1),
            FaultMessage = new Style(Color.Black, Color.Cyan1)
        };

        public static readonly ColorScheme Gold = new()
        {
            TableAccent = Color.Grey11,
            RowHeader = new Style(Color.White, null, Decoration.Bold),
            ColumnHeader = new Style(Color.White, null, Decoration.Bold),
            Bar = new Style(Color.Gold1, Color.Grey11),
            Preset = new Style(Color.White, Color.Grey11),
            PresetSelected = new Style(Color.Black, Color.Gold1),
            Caption = new Style(Color.White, null, Decoration.Dim),
            NumericData = new Style(Color.White, null),
            VoidData = new Style(Color.White, null),
            Units = new Style(Color.White, null),
            OffMode = new Style(Color.White, null, Decoration.Dim),
            ControlMode = new Style(Color.Black, Color.Gold1),
            UserControl = new Style(Color.Black, Color.Gold1),
            FaultMessage = new Style(Color.White, Color.Gold1)
        };

        public static readonly ColorScheme Blue = new()
        {
            TableAccent = Color.Grey11,
            RowHeader = new Style(Color.White, null, Decoration.Bold),
            ColumnHeader = new Style(Color.White, null, Decoration.Bold),
            Bar = new Style(Color.DodgerBlue2, Color.Grey11),
            Preset = new Style(Color.White, Color.Grey11),
            PresetSelected = new Style(Color.White, Color.DodgerBlue2),
            Caption = new Style(Color.White, null, Decoration.Dim),
            NumericData = new Style(Color.White, null),
            VoidData = new Style(Color.White, null),
            Units = new Style(Color.White, null),
            OffMode = new Style(Color.White, null, Decoration.Dim),
            ControlMode = new Style(Color.White, Color.DodgerBlue2),
            UserControl = new Style(Color.White, Color.DodgerBlue2),
            FaultMessage = new Style(Color.White, Color.DodgerBlue2)
        };

        public static readonly ColorScheme BlueViolet = new()
        {
            TableAccent = Color.Grey11,
            RowHeader = new Style(Color.White, null, Decoration.Bold),
            ColumnHeader = new Style(Color.White, null, Decoration.Bold),
            Bar = new Style(Color.BlueViolet, Color.Grey11),
            Preset = new Style(Color.White, Color.Grey11),
            PresetSelected = new Style(Color.White, Color.BlueViolet),
            Caption = new Style(Color.White, null, Decoration.Dim),
            NumericData = new Style(Color.White, null),
            VoidData = new Style(Color.White, null),
            Units = new Style(Color.White, null),
            OffMode = new Style(Color.White, null, Decoration.Dim),
            ControlMode = new Style(Color.White, Color.BlueViolet),
            UserControl = new Style(Color.White, Color.BlueViolet),
            FaultMessage = new Style(Color.White, Color.BlueViolet)
        };

        #endregion

        // Schemes typically used for indication "fault" status.
        #region Faulted Schemes

        public static readonly ColorScheme FaultedDarkRed = new()
        {
            TableAccent = Color.DarkRed,
            RowHeader = new Style(Color.White, null, Decoration.Bold),
            ColumnHeader = new Style(Color.White, null, Decoration.Bold),
            Bar = new Style(Color.Grey, Color.Grey11),
            Preset = new Style(Color.Grey, Color.Grey11),
            PresetSelected = new Style(Color.Black, Color.Grey),
            Caption = new Style(Color.White, null, Decoration.Dim),
            NumericData = new Style(Color.White, null),
            VoidData = new Style(Color.White, null),
            Units = new Style(Color.White, null),
            OffMode = new Style(Color.White, null, Decoration.Dim),
            ControlMode = new Style(Color.Black, Color.Grey),
            UserControl = new Style(Color.White, Color.DarkRed),
            FaultMessage = new Style(Color.White, Color.DarkRed)
        };

        public static readonly ColorScheme FaultedDarkGreen = new()
        {
            TableAccent = Color.DarkGreen,
            RowHeader = new Style(Color.White, null, Decoration.Bold),
            ColumnHeader = new Style(Color.White, null, Decoration.Bold),
            Bar = new Style(Color.Grey, Color.Grey11),
            Preset = new Style(Color.Grey, Color.Grey11),
            PresetSelected = new Style(Color.Black, Color.Grey),
            Caption = new Style(Color.White, null, Decoration.Dim),
            NumericData = new Style(Color.White, null),
            VoidData = new Style(Color.White, null),
            Units = new Style(Color.White, null),
            OffMode = new Style(Color.White, null, Decoration.Dim),
            ControlMode = new Style(Color.Black, Color.Grey),
            UserControl = new Style(Color.White, Color.DarkGreen),
            FaultMessage = new Style(Color.White, Color.DarkGreen)
        };

        public static readonly ColorScheme FaultedDarkMagenta = new()
        {
            TableAccent = Color.DarkMagenta,
            RowHeader = new Style(Color.White, null, Decoration.Bold),
            ColumnHeader = new Style(Color.White, null, Decoration.Bold),
            Bar = new Style(Color.Grey, Color.Grey11),
            Preset = new Style(Color.Grey, Color.Grey11),
            PresetSelected = new Style(Color.Black, Color.Grey),
            Caption = new Style(Color.White, null, Decoration.Dim),
            NumericData = new Style(Color.White, null),
            VoidData = new Style(Color.White, null),
            Units = new Style(Color.White, null),
            OffMode = new Style(Color.White, null, Decoration.Dim),
            ControlMode = new Style(Color.Black, Color.Grey),
            UserControl = new Style(Color.White, Color.DarkMagenta),
            FaultMessage = new Style(Color.White, Color.DarkMagenta)
        };

        public static readonly ColorScheme FaultedCyan = new()
        {
            TableAccent = Color.Cyan1,
            RowHeader = new Style(Color.White, null, Decoration.Bold),
            ColumnHeader = new Style(Color.White, null, Decoration.Bold),
            Bar = new Style(Color.Grey, Color.Grey11),
            Preset = new Style(Color.Grey, Color.Grey11),
            PresetSelected = new Style(Color.Black, Color.Grey),
            Caption = new Style(Color.White, null, Decoration.Dim),
            NumericData = new Style(Color.White, null),
            VoidData = new Style(Color.White, null),
            Units = new Style(Color.White, null),
            OffMode = new Style(Color.White, null, Decoration.Dim),
            ControlMode = new Style(Color.Black, Color.Grey),
            UserControl = new Style(Color.White, Color.Cyan1),
            FaultMessage = new Style(Color.Black, Color.Cyan1)
        };

        public static readonly ColorScheme FaultedGold = new()
        {
            TableAccent = Color.Gold1,
            RowHeader = new Style(Color.White, null, Decoration.Bold),
            ColumnHeader = new Style(Color.White, null, Decoration.Bold),
            Bar = new Style(Color.Grey, Color.Grey11),
            Preset = new Style(Color.Grey, Color.Grey11),
            PresetSelected = new Style(Color.Black, Color.Grey),
            Caption = new Style(Color.White, null, Decoration.Dim),
            NumericData = new Style(Color.White, null),
            VoidData = new Style(Color.White, null),
            Units = new Style(Color.White, null),
            OffMode = new Style(Color.White, null, Decoration.Dim),
            ControlMode = new Style(Color.Black, Color.Grey),
            UserControl = new Style(Color.White, Color.Gold1),
            FaultMessage = new Style(Color.Black, Color.Gold1)
        };

        public static readonly ColorScheme FaultedGrey = new()
        {
            TableAccent = Color.Grey,
            RowHeader = new Style(Color.White, null, Decoration.Bold),
            ColumnHeader = new Style(Color.White, null, Decoration.Bold),
            Bar = new Style(Color.Grey, Color.Grey11),
            Preset = new Style(Color.Grey, Color.Grey11),
            PresetSelected = new Style(Color.White, Color.Grey),
            Caption = new Style(Color.White, null, Decoration.Dim),
            NumericData = new Style(Color.White, null),
            VoidData = new Style(Color.White, null),
            Units = new Style(Color.White, null),
            OffMode = new Style(Color.White, null, Decoration.Dim),
            ControlMode = new Style(Color.Black, Color.Grey),
            UserControl = new Style(Color.White, Color.Grey),
            FaultMessage = new Style(Color.White, Color.Grey)
        };

        public static readonly ColorScheme FaultedBlue = new()
        {
            TableAccent = Color.Blue,
            RowHeader = new Style(Color.White, null, Decoration.Bold),
            ColumnHeader = new Style(Color.White, null, Decoration.Bold),
            Bar = new Style(Color.Grey, Color.Grey11),
            Preset = new Style(Color.Grey, Color.Grey11),
            PresetSelected = new Style(Color.Black, Color.Grey),
            Caption = new Style(Color.White, null, Decoration.Dim),
            NumericData = new Style(Color.White, null),
            VoidData = new Style(Color.White, null),
            Units = new Style(Color.White, null),
            OffMode = new Style(Color.White, null, Decoration.Dim),
            ControlMode = new Style(Color.Black, Color.Grey),
            UserControl = new Style(Color.Black, Color.Blue),
            FaultMessage = new Style(Color.Black, Color.Blue)
        };

        public static readonly ColorScheme FaultedBlueViolet = new()
        {
            TableAccent = Color.BlueViolet,
            RowHeader = new Style(Color.White, null, Decoration.Bold),
            ColumnHeader = new Style(Color.White, null, Decoration.Bold),
            Bar = new Style(Color.Grey, Color.Grey11),
            Preset = new Style(Color.Grey, Color.Grey11),
            PresetSelected = new Style(Color.Black, Color.Grey),
            Caption = new Style(Color.White, null, Decoration.Dim),
            NumericData = new Style(Color.White, null),
            VoidData = new Style(Color.White, null),
            Units = new Style(Color.White, null),
            OffMode = new Style(Color.White, null, Decoration.Dim),
            ControlMode = new Style(Color.Black, Color.Grey),
            UserControl = new Style(Color.White, Color.BlueViolet),
            FaultMessage = new Style(Color.White, Color.BlueViolet)
        };

        #endregion
    }
}
