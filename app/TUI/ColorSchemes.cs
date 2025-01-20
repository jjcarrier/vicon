using Spectre.Console;

namespace PowerSupplyApp.TUI
{
    public class ColorSchemes
    {
        // Schemes typically used for normal operation.
        #region Normal Schemes

        public static ColorScheme ClassicRed = new ColorScheme
        {
            TableAccent = Color.Grey,
            RowHeader = new Style(Color.White, null, Decoration.Bold),
            ColumnHeader = new Style(Color.White, null, Decoration.Bold),
            Bar = new Style(Color.DarkRed, Color.Grey50),
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

        public static ColorScheme ClassicGold = new ColorScheme
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

        public static ColorScheme BlackAndWhite = new ColorScheme
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

        public static ColorScheme Grey = new ColorScheme
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

        public static ColorScheme DarkRed = new ColorScheme
        {
            TableAccent = Color.Grey11,
            RowHeader = new Style(Color.White, null, Decoration.Bold),
            ColumnHeader = new Style(Color.White, null, Decoration.Bold),
            Bar = new Style(Color.DarkRed, Color.Grey50),
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

        public static ColorScheme DarkGreen = new ColorScheme
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

        public static ColorScheme DarkMagenta = new ColorScheme
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

        public static ColorScheme Cyan = new ColorScheme
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

        public static ColorScheme Gold = new ColorScheme
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

        public static ColorScheme Blue = new ColorScheme
        {
            TableAccent = Color.Grey11,
            RowHeader = new Style(Color.White, null, Decoration.Bold),
            ColumnHeader = new Style(Color.White, null, Decoration.Bold),
            Bar = new Style(Color.Blue, Color.Grey11),
            Preset = new Style(Color.White, Color.Grey11),
            PresetSelected = new Style(Color.Black, Color.Blue),
            Caption = new Style(Color.White, null, Decoration.Dim),
            NumericData = new Style(Color.White, null),
            VoidData = new Style(Color.White, null),
            Units = new Style(Color.White, null),
            OffMode = new Style(Color.White, null, Decoration.Dim),
            ControlMode = new Style(Color.Black, Color.Blue),
            UserControl = new Style(Color.Black, Color.Blue),
            FaultMessage = new Style(Color.Black, Color.Blue)
        };

        public static ColorScheme BlueViolet = new ColorScheme
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

        public static ColorScheme FaultedDarkRed = new ColorScheme
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

        public static ColorScheme FaultedDarkGreen = new ColorScheme
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

        public static ColorScheme FaultedDarkMagenta = new ColorScheme
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

        public static ColorScheme FaultedCyan = new ColorScheme
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

        public static ColorScheme FaultedGold = new ColorScheme
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

        public static ColorScheme FaultedGrey = new ColorScheme
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

        public static ColorScheme FaultedBlue = new ColorScheme
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

        public static ColorScheme FaultedBlueViolet = new ColorScheme
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
