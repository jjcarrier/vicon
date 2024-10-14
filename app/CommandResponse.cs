using Newtonsoft.Json.Converters;
using Newtonsoft.Json;

namespace PowerSupplyApp
{
    public class CommandResponse
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public Operation Command { get; set; }

        public object Response { get; set; }
    }
}
