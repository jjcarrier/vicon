using Spectre.Console;

namespace PowerSupplyApp.TUI
{
    public class ColorScheme
    {
        // For presentation of the table accents (horizontal separators/bars).
        public Style TableAccent { get; set; }

        // For presentation of the units of the data table.
        public Style Units { get; set; }

        // For presentation of the column headers of the data table.
        public Style ColumnHeader { get; set; }

        // For presentation of the row headers of the data table.
        public Style RowHeader { get; set; }

        // For presentation of the currently inactive presets.
        public Style Preset { get; set; }

        // For presentation of the currently selected preset.
        public Style PresetSelected { get; set; }

        // For presentation of the V/I meter bars.
        public Style Bar { get; set; }

        // For presentation of the table caption region (currently used to show timestamping).
        public Style Caption { get; set; }

        // For presentation of numeric data (setpoints, actual outputs, etc).
        public Style NumericData { get; set; }

        // For presentation of data set to "---" or "".
        public Style VoidData { get; set; }

        // For presentation of the "OFF" state for the control mode.
        public Style OffMode { get; set; }

        // For presentation of the "CC"/"CV" states for the control mode.
        public Style ControlMode { get; set; }

        // For presentation of the "LOCKED" or "AWG" modes.
        public Style UserControl { get; set; }

        // For presentation of fault messages such as "OVP", "OCP", etc.
        public Style FaultMessage { get; set; }
    }
}
