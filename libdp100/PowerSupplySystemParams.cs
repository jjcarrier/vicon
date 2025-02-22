namespace LibDP100
{
    public class PowerSupplySystemParams
    {
        // Over power protection. Unit: 0.1W.
        public ushort OPP { get; set; } = 0;

        // Over temperature protection. Unit Celsius. Value range 50-80
        public ushort OTP { get; set; } = 0;

        // Reverse polarity protection enable/disable.
        public bool RPP { get; set; } = false;

        // Enable/Disable automatic output-on.
        public bool AutoOn { get; set; } = false;

        // Backlight level value 0-4.
        public byte Backlight { get; set; } = 0;

        // Volume level ranges from 0-4.
        public byte Volume { get; set; } = 0;

        // Default constructor.
        public PowerSupplySystemParams() { }

        // Construct the setpoint as a copy of another.
        public PowerSupplySystemParams(PowerSupplySystemParams sys)
        {
            Copy(sys);
        }

        // Make a copy of the provided setpoint object.
        public void Copy(PowerSupplySystemParams sys)
        {
            OPP = sys.OPP;
            OTP = sys.OTP;
            Backlight = sys.Backlight;
            Volume = sys.Volume;
        }

        public bool Parse(byte[] response)
        {
            const int expectedNumBytes = 8;
            const int responseLenIndex = 4;
            const int responseDataIndex = 5;

            if (response[responseLenIndex] != expectedNumBytes)
            {
                return false;
            }

            OTP = BitConverter.ToUInt16(response, responseDataIndex);
            OPP = BitConverter.ToUInt16(response, responseDataIndex + 2);
            Backlight = response[responseDataIndex + 4];
            Volume = response[responseDataIndex + 5];
            RPP = response[responseDataIndex + 6] != 0;
            AutoOn = response[responseDataIndex + 7] != 0;

            return true;
        }

        public void Print()
        {
            Console.WriteLine("[ SYS_PARAMS ] ");
            Console.WriteLine("  OTP (C)   : " + OTP);
            Console.WriteLine("  OPP (W)   : " + OPP / 10.0f);
            Console.WriteLine("  Backlight : " + Backlight);
            Console.WriteLine("  Volume    : " + Volume);
            Console.WriteLine("  RPP       : " + (RPP ? "Enabled" : "Disabled"));
            Console.WriteLine("  AUTO-ON   : " + (AutoOn ? "Enabled" : "Disabled"));
            Console.WriteLine();
        }
    }
}
