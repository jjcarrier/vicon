using System.Text;

namespace LibDP100
{
    /// <summary>
    /// The power supply informational data.
    /// </summary>
    public class PowerSupplyInfo
    {
        /// <summary>
        /// The device name/type.
        /// </summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// The device's hardware version.
        /// </summary>
        public string HardwareVersion { get; set; } = string.Empty;

        /// <summary>
        /// The devices software version.
        /// </summary>
        public string SoftwareVersion { get; set; } = string.Empty;

        /// <summary>
        /// The devices bootloader version.
        /// </summary>
        public string BootloaderVersion { get; set; } = string.Empty;

        /// <summary>
        /// The devices uniquely identifiable serial number.
        /// </summary>
        public string SerialNumber { get; set; } = string.Empty;

        /// <summary>
        /// The devices software state (reports whether in application or bootloader mode).
        /// </summary>
        public string SoftwareState { get; set; } = string.Empty;

        /// <summary>
        /// The devices manufacturing date.
        /// </summary>
        public string MfgDate { get; set; } = string.Empty;

        /// <summary>
        /// A user assigned alias for the device for identification purposes.
        /// </summary>
        public string Alias { get; set; } = string.Empty;

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
            const int expectedNumBytes = 0x28;
            const int responseLenIndex = 4;
            const int responseDataIndex = 5;
            const int runAreaApp = 0xAA;

            if (response[responseLenIndex] != expectedNumBytes)
            {
                return false;
            }

            Type = Encoding.ASCII.GetString(response, responseDataIndex, 16).Split('\0').First();
            HardwareVersion = $"V{BitConverter.ToUInt16(response, responseDataIndex + 16) / 10.0}";
            SoftwareVersion = $"V{BitConverter.ToUInt16(response, responseDataIndex + 18) / 10.0}";
            BootloaderVersion = $"V{BitConverter.ToUInt16(response, responseDataIndex + 20) / 10.0}";
            SerialNumber = BitConverter.ToString(BitConverter.GetBytes(BitConverter.ToUInt32(response, responseDataIndex + 32)))
                .Replace("-", string.Empty);

            if (BitConverter.ToUInt16(response, responseDataIndex + 22) == runAreaApp)
            {
                SoftwareState = "APP";
            }
            else
            {
                SoftwareState = "BOOT";
            }

            int year = BitConverter.ToUInt16(response, responseDataIndex + 36);
            int month = response[responseDataIndex + 38];
            int day = response[responseDataIndex + 39]; ;
            MfgDate = $"{year}-{month:D02}-{day:D02}";

            return true;
        }

        /// <summary>
        /// Prints the object members to standard output.
        /// </summary>
        public void Print()
        {
            Console.WriteLine("[ DEV_INFO ] ");
            Console.WriteLine("  Type   : " + Type);
            Console.WriteLine("  SN     : " + SerialNumber);
            Console.WriteLine("  Mfg    : " + MfgDate);
            Console.WriteLine("  HW     : " + HardwareVersion);
            Console.WriteLine("  SW     : " + SoftwareVersion);
            Console.WriteLine("  BOOT   : " + BootloaderVersion);
            Console.WriteLine("  Status : " + SoftwareState);
            if (!string.IsNullOrWhiteSpace(Alias))
            {
                Console.WriteLine("  Alias  : " + Alias);
            }
            Console.WriteLine();
        }
    }
}
