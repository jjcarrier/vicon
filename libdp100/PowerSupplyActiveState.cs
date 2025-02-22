namespace LibDP100
{
    /// <summary>
    /// The power supply active state. Provides data sensed/reported by the device.
    /// </summary>
    public class PowerSupplyActiveState
    {
        private const ushort InvalidVoltage = 0xFFFF;
        private const ushort InvalidCurrent = 0xFFFF;
        private const ushort InvalidTemperature = 0xFFFF;

        /// <summary>
        /// The time at which the reading was received.
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// The sensed output status (normal=0/uvp=6).
        /// </summary>
        public PowerSupplyFaultStatus FaultStatus { get; set; } = PowerSupplyFaultStatus.Invalid;

        /// <summary>
        /// The sensed output mode.
        /// </summary>
        public PowerSupplyOutputMode OutputMode { get; set; } = PowerSupplyOutputMode.Invalid;

        /// <summary>
        /// The maximum possible output voltage given the input voltage.
        /// Based on the sensed input voltage.
        /// </summary>
        public ushort VoltageOutputMax { get; set; } = InvalidVoltage;

        /// <summary>
        /// The sensed input voltage to the power supply.
        /// </summary>
        public ushort VoltageInput { get; set; } = InvalidVoltage;

        /// <summary>
        /// The sensed voltage on the output.
        /// </summary>
        public ushort Voltage { get; set; } = InvalidVoltage;

        /// <summary>
        /// The sensed current on the output.
        /// </summary>
        public ushort Current { get; set; } = InvalidCurrent;

        /// <summary>
        /// The sensed 5V USB rail (VBUS).
        /// </summary>
        public ushort VoltageUsb5V { get; set; } = InvalidVoltage;

        /// <summary>
        /// The temperature of the device.
        /// </summary>
        public ushort Temperature1 { get; set; } = InvalidTemperature;

        /// <summary>
        /// The temperature of the device.
        /// </summary>
        public ushort Temperature2 { get; set; } = InvalidTemperature;

        /// <summary>
        /// Parses/deserializes the HID response data, updating the related
        /// object members.
        /// </summary>
        /// <param name="response">The data to parse.</param>
        /// <returns>
        /// <see cref="true"/> when parsed successfully.
        /// <see cref="false"/> when parsing failed.
        /// </returns>
        public bool Parse(byte[] response)
        {
            const int expectedNumBytes = 16;
            const int responseLenIndex = 4;
            const int responseDataIndex = 5;

            Timestamp = DateTime.Now;

            if (response[responseLenIndex] != expectedNumBytes)
            {
                return false;
            }

            VoltageInput = BitConverter.ToUInt16(response, responseDataIndex);
            Voltage = BitConverter.ToUInt16(response, responseDataIndex + 2);
            Current = BitConverter.ToUInt16(response, responseDataIndex + 4);
            VoltageOutputMax = BitConverter.ToUInt16(response, responseDataIndex + 6);
            Temperature1 = BitConverter.ToUInt16(response, responseDataIndex + 8);
            Temperature2 = BitConverter.ToUInt16(response, responseDataIndex + 10);
            VoltageUsb5V = BitConverter.ToUInt16(response, responseDataIndex + 12);
            OutputMode = (PowerSupplyOutputMode)response[responseDataIndex + 14];
            FaultStatus = (PowerSupplyFaultStatus)response[responseDataIndex + 15];

            return true;
        }

        /// <summary>
        /// Prints the object members to standard output.
        /// </summary>
        public void Print()
        {
            Console.WriteLine("[ ACTIVE_STATE ]");
            Console.WriteLine($"  Time (ns)    : {Timestamp.Ticks:X08}");
            Console.WriteLine($"  Fault Status : {FaultStatus}");
            Console.WriteLine($"  Output Mode  : {OutputMode}");
            Console.WriteLine($"  Temp1 (C)    : {(double)Temperature1 / 10}");
            Console.WriteLine($"  Temp2 (C)    : {(double)Temperature2 / 10}");
            Console.WriteLine($"  V[usb] (mV)  : {VoltageUsb5V}");
            Console.WriteLine($"  V[in] (mV)   : {VoltageInput}");
            Console.WriteLine($"  V[max] (mV)  : {VoltageOutputMax}");
            Console.WriteLine($"  Voltage (mV) : {Voltage}");
            Console.WriteLine($"  Current (mA) : {Current}");
            Console.WriteLine();
        }
    }
}
