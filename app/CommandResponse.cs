using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace PowerSupplyApp
{
    public class CommandResponse
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public Operation Command { get; set; }

        public object Response { get; set; }
    }
}
