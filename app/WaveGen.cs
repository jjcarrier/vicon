using LibDP100;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Threading;

namespace PowerSupplyApp
{
    public static class WaveGen
    {
        public static bool Running { get; set; } = false;

        private static PowerSupply psu;
        private static PowerSupplySetpoint sp;
        private static ArbitraryWaveformGen awg;
        private static bool awgBusy = false;
        private static int awgIndex = 0;

        public static void Init(PowerSupply psu, PowerSupplySetpoint setpoint)
        {
            WaveGen.psu = psu;
            sp = setpoint;
            awgIndex = 0;
            awgBusy = false;
        }

        /// <summary>
        /// Use to reset the AWG to the first index to allow the pattern to be re-run.
        /// </summary>
        public static void Restart()
        {
            if (awg == null)
            {
                return;
            }

            awgIndex = 0;
            awgBusy = true;
        }

        public static void Load(string filepath)
        {
            if (string.IsNullOrEmpty(filepath))
            {
                return;
            }

            using (StreamReader file = File.OpenText(filepath))
            {
                JsonSerializer serializer = new JsonSerializer();
                awg = (ArbitraryWaveformGen)serializer.Deserialize(file, typeof(ArbitraryWaveformGen));
            }

            if (awg == null)
            {
                return;
            }

            awgBusy = true;
            awgIndex = 0;
        }

        public static WaveformPoint GetCurrentWaveformPoint()
        {
            WaveformPoint point = awg.Points[awgIndex];

            if (point.Milliseconds < 0)
            {
                point.Milliseconds = (int)awg.Milliseconds;
            }

            return point;
        }

        public static bool Run()
        {
            if (!awgBusy)
            {
                return awgBusy;
            }

            WaveformPoint point = awg.Points[awgIndex];

            sp.Current = point.Milliamps;
            sp.Voltage = point.Millivolts;
            if (!psu.SetSetpoint(sp))
            {
                awgBusy = false;
            }

            if (point.Milliseconds >= 0)
            {
                Thread.Sleep(point.Milliseconds);
            }
            else
            {
                Thread.Sleep((int)awg.Milliseconds);
            }

            awgIndex++;

            if (awgIndex >= awg.Points.Count)
            {
                awgIndex = 0;
                if (awg.LoopCount > 0)
                {
                    awg.LoopCount--;

                    if (awg.LoopCount == 0)
                    {
                        awgBusy = false;
                    }
                }
            }

            return awgBusy;
        }

        public static void Generate()
        {
            while (Run()) {}
        }
    }
}
