namespace LibDP100
{
    public class PowerSupplyActiveState
    {
        private const ushort InvalidVoltage = 0xFFFF;
        private const ushort InvalidCurrent = 0xFFFF;
        private const ushort InvalidTemperature = 0xFFFF;

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

        // The temperature of the device.
        public ushort Temperature { get; set; } = InvalidTemperature;

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
            Temperature = BitConverter.ToUInt16(response, responseDataIndex + 8);
            //var temp2 = BitConverter.ToUInt16(response, responseDataIndex + 10);
            VoltageUsb5V = BitConverter.ToUInt16(response, responseDataIndex + 12);
            OutputMode = (PowerSupplyOutputMode)response[responseDataIndex + 14];
            FaultStatus = (PowerSupplyFaultStatus)response[responseDataIndex + 15];

            return true;
        }

        public void Print()
        {
            Console.WriteLine("[ ACTIVE_STATE ]");
            Console.WriteLine($"  Time (ns)    : {Timestamp.Ticks:X08}");
            Console.WriteLine($"  Fault Status : {FaultStatus}");
            Console.WriteLine($"  Output Mode  : {OutputMode}");
            Console.WriteLine($"  Temp (C)     : {(double)Temperature / 10}");
            Console.WriteLine($"  V[usb] (mV)  : {VoltageUsb5V}");
            Console.WriteLine($"  V[in] (mV)   : {VoltageInput}");
            Console.WriteLine($"  V[max] (mV)  : {VoltageOutputMax}");
            Console.WriteLine($"  Voltage (mV) : {Voltage}");
            Console.WriteLine($"  Current (mA) : {Current}");
            Console.WriteLine();
        }
    }
}
