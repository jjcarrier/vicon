using System.ComponentModel;
using System.Text.Json.Serialization;

namespace PowerSupplyApp
{
    public class WaveformPoint
    {
        [JsonPropertyName("mv")]
        public ushort Millivolts { get; set; }

        [JsonPropertyName("ma")]
        public ushort Milliamps { get; set; }

        [JsonPropertyName("ms")] //, DefaultValueHandling = DefaultValueHandling.Populate)]
        [DefaultValue(-1)]
        public int Milliseconds { get; set; }
    }

    public class ArbitraryWaveformGen
    {
        [JsonPropertyName("loop-count")]
        public int LoopCount { get; set; }

        [JsonPropertyName("milliseconds")]
        public uint Milliseconds { get; set; }

        [JsonPropertyName("points")]
        public List<WaveformPoint> Points { get; set; }
    }
}
