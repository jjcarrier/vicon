using System;
using LibDP100;

namespace PowerSupplyApp
{
    partial class Program
    {
        static bool debug = false;
        static bool serializeAsJson = false;
        static bool serializeAsJsonArray = false;

        static int serializedOutput = 0;
        static int numSerializedOutputs = 0;

        static PowerSupply psu;
        static PowerSupplySetpoint sp;
        static PowerSupplySystemParams sys;

        static int Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            psu = new PowerSupply();

            ProcessArgsResult result = PreProcessArgs(args);
            if (result != ProcessArgsResult.Ok)
            {
                // Clear the negative results.
                if (result < ProcessArgsResult.Ok) { result = ProcessArgsResult.Ok; }
                return (int)result;
            }

            psu.DebugMode = debug;

            if (!psu.ConnectAndRefresh())
            {
                Console.WriteLine("ERROR: Could not initialize DP100!");
                Console.ReadLine();
                return 1;
            }

            sp = new PowerSupplySetpoint(psu.Output.Setpoint);

            sys = new PowerSupplySystemParams
            {
                Backlight = psu.SystemParams.Backlight,
                Volume = psu.SystemParams.Volume,
                OPP = psu.SystemParams.OPP,
                OTP = psu.SystemParams.OTP
            };

            result = ProcessArgs(psu, args);

            psu.Disconnect();

            return (int)result;
        }
    }
}
