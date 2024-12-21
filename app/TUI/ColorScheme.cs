using Spectre.Console;

namespace PowerSupplyApp
{
    public class ColorScheme
    {
        public Color TableAccent { get; set; }
        public Style Units { get; set; }
        public Style ColumnHeader { get; set; }
        public Style RowHeader { get; set; }
        public Style Preset { get; set; }
        public Style PresetSelected { get; set; }
        public Style Bar { get; set; }
        public Style Caption { get; set; }
        public Style NumericData { get; set; }
        public Style NumericDataChanged { get; set; }
        public Style VoidData { get; set; }
        public Style OffMode { get; set; }
        public Style ControlMode { get; set; }
        public Style FaultMessage { get; set; }
    }
}
