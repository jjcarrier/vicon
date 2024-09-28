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
    }
}
