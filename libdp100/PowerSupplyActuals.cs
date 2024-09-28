using System;

namespace LibDP100
{
    public class PowerSupplyActuals
    {
        private const ushort InvalidVoltage = 0xFFFF;
        private const ushort InvalidCurrent = 0xFFFF;

        // The time at which the reading was received.
        public DateTime Timestamp { get; set; }

        // The sensed output status (normal=0/uvp=6).
        public PowerSupplyFaultStatus FaultStatus { get; set; } = PowerSupplyFaultStatus.Invalid;

        // The sensed output mode.
        public PowerSupplyOutputMode OutputMode { get; set; } = PowerSupplyOutputMode.Invalid;

        // The maximum possible output voltage given the input voltage.
        // Based on the sensed input voltage.
        public ushort VoltageOutputMax { get; set; } = InvalidVoltage;

        // The sensed input voltage to the power supply.
        public ushort VoltageInput { get; set; } = InvalidVoltage;

        // The sensed voltage on the output.
        public ushort Voltage { get; set; } = InvalidVoltage;

        // The sensed current on the output.
        public ushort Current { get; set; } = InvalidCurrent;

        // The sensed 5V USB rail (VBUS).
        public ushort VoltageUsb5V { get; set; } = InvalidVoltage;
    }
}
