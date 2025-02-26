using System.Text.Json.Serialization;

namespace PowerSupplyApp
{
    public class CommandResponse
    {
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public Operation Command { get; set; }

        public required object Response { get; set; }
    }
}
