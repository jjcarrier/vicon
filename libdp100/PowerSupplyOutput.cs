namespace LibDP100
{
    /// <summary>
    /// Object used to process power supply output data.
    /// </summary>
    public class PowerSupplyOutput
    {
        /// <summary>
        /// Indicates whether the output is ON or OFF.
        /// </summary>
        public bool On { get; set; } = false;

        /// <summary>
        /// The output index (group number). Value range 0-9 Number.
        /// </summary>
        public byte Preset { get; set; } = 0;

        /// <summary>
        /// The active setpoint and limits of the power supply.
        /// </summary>
        public PowerSupplySetpoint Setpoint { get; set; } = new PowerSupplySetpoint(0);

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
            const int expectedNumBytes = 10;
            const int responseLenIndex = 4;
            const int responseDataIndex = 5;

            if (response[responseLenIndex] != expectedNumBytes)
            {
                return false;
            }

            Preset = (byte)(response[responseDataIndex] & 0xF);
            On = response[responseDataIndex + 1] != 0;
            Setpoint.Voltage = BitConverter.ToUInt16(response, responseDataIndex + 2);
            Setpoint.Current = BitConverter.ToUInt16(response, responseDataIndex + 4);
            Setpoint.OVP = BitConverter.ToUInt16(response, responseDataIndex + 6);
            Setpoint.OCP = BitConverter.ToUInt16(response, responseDataIndex + 8);

            return true;
        }

        /// <summary>
        /// Prints the object members to standard output.
        /// </summary>
        public void Print()
        {
            Console.WriteLine("[ OUT_PARAMS ]");
            Console.WriteLine("  State          : " + (On ? "ON" : "OFF"));
            Console.WriteLine("  Preset         : " + Preset);
            Console.WriteLine("  Setpoint");
            Console.WriteLine("    Voltage (mV) : " + Setpoint.Voltage);
            Console.WriteLine("    Current (mA) : " + Setpoint.Current);
            Console.WriteLine("    OVP (mV)     : " + Setpoint.OVP);
            Console.WriteLine("    OCP (mA)     : " + Setpoint.OCP);
            Console.WriteLine();
        }
    }
}
