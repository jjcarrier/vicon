using System.Text.Json.Serialization;

namespace LibDP100
{
    /// <summary>
    /// The power supply system parameters.
    /// </summary>
    public class PowerSupplySystemParams
    {
        /// <summary>
        /// Over power protection. Unit: 0.1W.
        /// </summary>
        [JsonPropertyName("opp")]
        public ushort OPP { get; set; } = 0;

        /// <summary>
        /// Over temperature protection. Unit Celsius. Value range 40-80.
        /// </summary>
        [JsonPropertyName("otp")]
        public ushort OTP { get; set; } = 0;

        /// <summary>
        /// Reverse polarity protection enable/disable.
        /// </summary>
        [JsonPropertyName("rpp")]
        public bool RPP { get; set; } = false;

        /// <summary>
        /// Enable/Disable automatic output-on.
        /// </summary>
        [JsonPropertyName("auto-on")]
        public bool AutoOn { get; set; } = false;

        /// <summary>
        /// Backlight level value 0-4.
        /// </summary>
        [JsonPropertyName("backlight")]
        public byte Backlight { get; set; } = 0;

        /// <summary>
        /// Volume level ranges from 0-4.
        /// </summary>
        [JsonPropertyName("volume")]
        public byte Volume { get; set; } = 0;

        /// <summary>
        /// Default constructor.
        /// </summary>
        public PowerSupplySystemParams() { }

        /// <summary>
        /// Construct the setpoint as a copy of another.
        /// </summary>
        /// <param name="sys">The parameters to copy data from.</param>
        public PowerSupplySystemParams(PowerSupplySystemParams sys)
        {
            Copy(sys);
        }

        /// <summary>
        /// Make a copy of the provided setpoint object.
        /// </summary>
        /// <param name="sys">The parameters to copy data from.</param>
        public void Copy(PowerSupplySystemParams sys)
        {
            OPP = sys.OPP;
            OTP = sys.OTP;
            Backlight = sys.Backlight;
            Volume = sys.Volume;
        }

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

        /// <summary>
        /// Prints the object members to standard output.
        /// </summary>
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
