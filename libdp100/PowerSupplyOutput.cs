namespace LibDP100
{
    public class PowerSupplyOutput
    {
        // Indicates whether the output is ON or OFF. NOTE: DP100 API used seemingly backwards terminology (close = OFF; open = ON).
        public bool On { get; set; } = false;

        // The output index (group number). Value range 0-9 Number. 0 is read-only.
        public byte Preset { get; set; } = 0;

        // The active setpoint and limits of the power supply.
        public PowerSupplySetpoint Setpoint { get; set; } = new PowerSupplySetpoint(0);

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
