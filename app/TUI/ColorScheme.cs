using Spectre.Console;

namespace PowerSupplyApp.TUI
{
    public class ColorScheme
    {
        // For presentation of the table accents (horizontal separators/bars).
        public required Style TableAccent { get; set; }

        // For presentation of the units of the data table.
        public required Style Units { get; set; }

        // For presentation of the column headers of the data table.
        public required Style ColumnHeader { get; set; }

        // For presentation of the row headers of the data table.
        public required Style RowHeader { get; set; }

        // For presentation of the currently inactive presets.
        public required Style Preset { get; set; }

        // For presentation of the currently selected preset.
        public required Style PresetSelected { get; set; }

        // For presentation of the V/I meter bars.
        public required Style Bar { get; set; }

        // For presentation of the table caption region (currently used to show timestamping).
        public required Style Caption { get; set; }

        // For presentation of numeric data (setpoints, actual outputs, etc).
        public required Style NumericData { get; set; }

        // For presentation of data set to "---" or "".
        public required Style VoidData { get; set; }

        // For presentation of the "OFF" state for the control mode.
        public required Style OffMode { get; set; }

        // For presentation of the "CC"/"CV" states for the control mode.
        public required Style ControlMode { get; set; }

        // For presentation of the "LOCKED" or "AWG" modes.
        public required Style UserControl { get; set; }

        // For presentation of fault messages such as "OVP", "OCP", etc.
        public required Style FaultMessage { get; set; }
    }
}
