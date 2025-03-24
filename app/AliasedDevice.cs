using System.Text.Json.Serialization;

namespace PowerSupplyApp
{
    public class AliasedDevice
    {
        [JsonPropertyName("alias")]
        public string Alias { get; set; } = string.Empty;

        [JsonPropertyName("serial")]
        public string Serial { get; set; } = string.Empty;

        public override string ToString()
        {
            if (Alias == string.Empty)
            {
                return Serial;
            }
            else
            {
                return $"{Serial} : {Alias}";
            }
        }
    }
}
