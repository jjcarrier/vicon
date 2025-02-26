using System.ComponentModel;
using System.Text.Json.Serialization;

namespace PowerSupplyApp
{
    public class WaveformPoint
    {
        [JsonPropertyName("mv")]
        public ushort Millivolts { get; set; } = 0;

        [JsonPropertyName("ma")]
        public ushort Milliamps { get; set; } = 0;

        [JsonPropertyName("ms")]
        [DefaultValue(-1)]
        public int Milliseconds { get; set; } = -1;
    }

    public class ArbitraryWaveformGen
    {
        [JsonPropertyName("loop-count")]
        public int LoopCount { get; set; } = 0;

        [JsonPropertyName("milliseconds")]
        public uint Milliseconds { get; set; } = 0;

        [JsonPropertyName("points")]
        public List<WaveformPoint> Points { get; set; } = [];
    }
}
