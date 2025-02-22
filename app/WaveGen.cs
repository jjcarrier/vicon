using LibDP100;
using Newtonsoft.Json;

namespace PowerSupplyApp
{
    public static class WaveGen
    {
        public static bool Running { get; set; } = false;
        public static WaveGenStatus LastErrorCode { get; set; } = WaveGenStatus.Ok;

        private static PowerSupply psu;
        private static PowerSupplySetpoint sp;
        private static ArbitraryWaveformGen awg;
        private static bool awgBusy = false;
        private static int awgIndex = 0;

        public static bool Init(PowerSupply psu, PowerSupplySetpoint setpoint)
        {
            WaveGen.psu = psu;
            sp = setpoint;
            awgIndex = 0;
            awgBusy = false;
            LastErrorCode = WaveGenStatus.Ok;

            return true;
        }

        /// <summary>
        /// Use to reset the AWG to the first index to allow the pattern to be re-run.
        /// </summary>
        public static bool Restart()
        {
            if (awg == null)
            {
                LastErrorCode = WaveGenStatus.NotLoaded;
                return false;
            }

            LastErrorCode = WaveGenStatus.Ok;
            awgIndex = 0;
            awgBusy = true;

            return true;
        }

        public static bool Load(string filepath)
        {
            if (string.IsNullOrEmpty(filepath))
            {
                LastErrorCode = WaveGenStatus.InvalidFilePath;
                return false;
            }

            if (!File.Exists(filepath))
            {
                LastErrorCode = WaveGenStatus.FileDoesNotExit;
                return false;
            }

            using (StreamReader file = File.OpenText(filepath))
            {
                JsonSerializer serializer = new JsonSerializer();
                awg = (ArbitraryWaveformGen)serializer.Deserialize(file, typeof(ArbitraryWaveformGen));
            }

            if (awg == null)
            {
                LastErrorCode = WaveGenStatus.InvalidFile;
                return false;
            }

            awgIndex = 0;
            awgBusy = true;

            return true;
        }

        public static string GetLastErrorMessage()
        {
            switch (LastErrorCode)
            {
                case WaveGenStatus.Ok:
                    return "No Error";
                case WaveGenStatus.FileDoesNotExit:
                    return "File does not exist!";
                case WaveGenStatus.InvalidFile:
                    return "Invalid file format!";
                case WaveGenStatus.InvalidFilePath:
                    return "Invalid file path!";
                case WaveGenStatus.NotLoaded:
                    return "No Error";
                case WaveGenStatus.SetpointFailure:
                    return "Failed to Write Setpoint";
                default:
                    return "Unknown Error!";
            }
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
            if (psu.SetOutput(sp) != PowerSupplyResult.OK)
            {
                LastErrorCode = WaveGenStatus.SetpointFailure;
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
