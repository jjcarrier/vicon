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
            ProcessArgsResult result = PreProcessArgs(args);
            if (result != ProcessArgsResult.Ok)
            {
                // Clear the negative result values.
                if (result < ProcessArgsResult.Ok) { result = ProcessArgsResult.Ok; }
                return (int)result;
            }

            bool psuReady = false;
            int psuCount = Enumerator.Enumerate();

            if (enumerate)
            {
                numSerializedOutputs = Enumerator.GetDeviceCount();
                if (numSerializedOutputs == 0)
                {
                    if (serializeAsJson)
                    {
                        SerializeObject(null);
                    }
                    else
                    {
                        Console.WriteLine("No DP100 detected!");
                    }
                }

                for (int i = 0; i < numSerializedOutputs; i++)
                {
                    var dev = Enumerator.GetDeviceByIndex(i);
                    bool res;

                    if (serializeAsJson)
                    {
                        res = SerializeObject(new CommandResponse
                        {
                            Command = Operation.ReadDevice,
                            Response = new { dev.Device }
                        });
                    }
                    else
                    {
                        Console.WriteLine();
                        res = dev.PrintDevInfo();
                    }

                    if (!res)
                    {
                        return -1;
                    }
                }

                return 0;
            }

            if (psuCount == 0)
            {
                Console.WriteLine("ERROR: No DP100 detected!");
                return 1;
            }

            if ((psuSerialNumber == null) && (psuCount > 1))
            {
                if (interactiveMode)
                {
                    EnterAlternateScreenBuffer();
                    Console.SetCursorPosition(0, 0);
                    psuSerialNumber = GetDeviceSelection(Enumerator.GetSerialNumbers());
                }
                else
                {
                    Console.WriteLine("ERROR: Multiple DP100s detected. Please provide the --serial option!");
                    return 1;
                }
            }

            if (psuSerialNumber != null)
            {
                psu = Enumerator.GetDeviceBySerial(psuSerialNumber);
                psuReady = (psu != null);

                if (psuReady)
                {
                    psu.RefreshOutputParams();
                }
            }
            else if (psuCount == 1)
            {
                psu = Enumerator.GetDeviceByIndex(0);
                psu.RefreshOutputParams();
                psuReady = true;
            }

            if (!psuReady)
            {
                Console.WriteLine("ERROR: Could not initialize DP100!");
                return 1;
            }

            psu.DebugMode = debug;

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
