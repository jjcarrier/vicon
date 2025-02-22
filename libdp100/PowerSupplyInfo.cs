using System.Text;

namespace LibDP100
{
    public class PowerSupplyInfo
    {
        public string Type { get; set; } = string.Empty;
        public string HardwareVersion { get; set; } = string.Empty;
        public string SoftwareVersion { get; set; } = string.Empty;
        public string BootloaderVersion { get; set; } = string.Empty;
        public string SerialNumber { get; set; } = string.Empty;
        public string SoftwareState { get; set; } = string.Empty;
        public string MfgDate { get; set; } = string.Empty;

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
            Console.WriteLine();
        }
    }
}
