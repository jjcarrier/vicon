using LibDP100;

namespace PowerSupplyApp
{
    internal partial class Program
    {
        private static bool debug = false;
        private static bool serializeAsJson = false;
        private static bool serializeAsJsonArray = false;
        private static int serializedOutput = 0;
        private static int numSerializedOutputs = 0;
        private static PowerSupply? psu;

        // Holds the user requested setpoint (which may or may not match the actual state of the device).
        private static PowerSupplySetpoint sp = new(0);
        private static PowerSupplySystemParams sys = new();

        private static int Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            ProcessArgsResult result = PreProcessArgs(args);
            if (result != ProcessArgsResult.Ok)
            {
                // Clear the negative result values.
                if (result < ProcessArgsResult.Ok) { result = ProcessArgsResult.Ok; }
                return (int)result;
            }

            int psuCount = Enumerator.Enumerate(settings.AliasedDevices);

            if (enumerate)
            {
                return PrintEnumeration();
            }

            if (psuCount == 0)
            {
                ShowError("No DP100 detected!");
                return 1;
            }

            if ((psuSerialNumber == string.Empty) && (psuCount > 1))
            {
                if (interactiveMode)
                {
                    EnterAlternateScreenBuffer();
                    Console.SetCursorPosition(0, 0);
                    psuSerialNumber = GetDeviceSelection(Enumerator.GetAliasedDevices());
                }
                else
                {
                    ShowError("Multiple DP100s detected. Please provide the --serial option!");
                    return 1;
                }
            }

            if (!string.IsNullOrEmpty(psuSerialNumber))
            {
                psu = Enumerator.GetDeviceBySerial(psuSerialNumber);
            }
            else if (psuCount == 1)
            {
                psuSerialNumber = Enumerator.GetAliasedDevices()[0].Serial;
                psu = Enumerator.GetDeviceByIndex(0);
            }

            // Device selection done, release unused instances so that other
            // applications may connect to them.
            Enumerator.Done();

            if (psu != null)
            {
                psu.GetDeviceInfo();
                psu.GetSystemParams();
                psu.Reload();
            }
            else
            {
                ShowError("Could not initialize DP100!");
                return 1;
            }

            // Only permit debug mode in non-interactive mode.
            psu.DebugMode = !interactiveMode && debug;

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

        private static int PrintEnumeration()
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

                if (dev == null)
                {
                    return -1;
                }

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
                    res = true;
                    dev.Device.Print();
                }

                if (!res)
                {
                    return -1;
                }
            }

            return 0;
        }
    }
}
