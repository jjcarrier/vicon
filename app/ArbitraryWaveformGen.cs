using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel;

namespace PowerSupplyApp
{
    public class WaveformPoint
    {
        [JsonProperty(PropertyName = "mv")]
        public ushort Millivolts { get; set; }

        [JsonProperty(PropertyName = "ma")]
        public ushort Milliamps { get; set; }

        [JsonProperty(PropertyName = "ms", DefaultValueHandling = DefaultValueHandling.Populate)]
        [DefaultValue(-1)]
        public int Milliseconds { get; set; }
    }

    public class ArbitraryWaveformGen
    {
        [JsonProperty(PropertyName = "loop-count")]
        public int LoopCount { get; set; }

        [JsonProperty(PropertyName = "milliseconds")]
        public uint Milliseconds { get; set; }

        [JsonProperty(PropertyName = "points")]
        public List<WaveformPoint> Points { get; set; }
    }
}
