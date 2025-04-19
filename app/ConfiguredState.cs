using System.Text.Json.Serialization;
using System.Security.Cryptography;
using LibDP100;

namespace PowerSupplyApp
{
    public class ConfiguredState
    {
        [JsonPropertyName("hash")]
        public string Hash { get; set; } = string.Empty;

        [JsonPropertyName("system-params")]
        public PowerSupplySystemParams SystemParams { get; set; } = new();

        [JsonPropertyName("presets")]
        public List<PowerSupplySetpoint> Presets { get; set; } = [];

        /// <summary>
        /// The number of presets available on the device.
        /// </summary>
        public const byte NumPresets = 10;

        public void Print()
        {
            SystemParams.Print();
            foreach (var preset in Presets)
            {
                preset.Print();
            }
        }

        public string ComputeConfiguredHash()
        {
            using var sha256 = SHA256.Create();
            var sb = new System.Text.StringBuilder();

            // Include settings critical for safe operation.
            sb.Append(SystemParams.OPP);
            sb.Append(SystemParams.OTP);
            sb.Append(SystemParams.RPP);
            sb.Append(SystemParams.AutoOn);
            foreach (var preset in Presets)
            {
                sb.Append(preset.Voltage);
                sb.Append(preset.Current);
                sb.Append(preset.OVP);
                sb.Append(preset.OCP);
            }

            // Compute the hash
            var hashBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(sb.ToString()));
            return Convert.ToHexString(hashBytes);
        }

    }
}
