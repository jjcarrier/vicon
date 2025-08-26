using LibDP100;
using System.Security.Cryptography;

namespace PowerSupplyApp
{
    internal partial class Program
    {
        private static bool debug = false;
        private static bool saveConfiguration = false;
        private static bool loadConfiguration = false;
        private static bool checkConfiguration = false;
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
                return (int)ProcessArgsResult.DeviceNotPresent;
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
                    return (int)ProcessArgsResult.SerialNumberRequired;
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

            AliasedDevice? devSettings = null;

            if (psu != null)
            {
                psu.DebugMode = debug;
                psu.GetDeviceInfo();
                psu.GetSystemParams();
                psu.Reload();

                if (loadConfiguration || saveConfiguration || checkConfiguration)
                {
                    devSettings = settings.FindDeviceBySerialNumber(psu.Device.SerialNumber);
                    if (devSettings == null)
                    {
                        return (int)ProcessArgsResult.NoAliasedDeviceFound;
                    }
                    else if (devSettings.Config == null)
                    {
                        return (int)ProcessArgsResult.NoConfigurationPresent;
                    }
                }

                if (loadConfiguration)
                {
                    if (devSettings?.Config == null)
                    {
                        return (int)ProcessArgsResult.NoConfigurationPresent;
                    }
                    LoadConfiguration(psu, devSettings.Config);
                    psu.Reload();
                }

                if (checkConfiguration)
                {
                    if (devSettings == null)
                    {
                        return (int)ProcessArgsResult.NoAliasedDeviceFound;
                    }

                    (string computedConfigHash, string computedDeviceHash, string recordedHash) = GetHashes(psu, devSettings);
                    if ((recordedHash != computedConfigHash) || (recordedHash != computedDeviceHash))
                    {
                        ExitAlternateScreenBuffer();
                        psu.DebugMode = debug;
                        result = CheckConfiguration(psu, devSettings, computedConfigHash, computedDeviceHash, recordedHash);
                        if (result != ProcessArgsResult.Ok)
                        {
                            return (int)result;
                        }
                        psu.DebugMode = false;
                        EnterAlternateScreenBuffer();
                    }
                }
            }
            else
            {
                ShowError("Could not initialize DP100!");
                return (int)ProcessArgsResult.InitializationFailed;
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

            if (saveConfiguration && result == ProcessArgsResult.Ok)
            {
                if (devSettings == null)
                {
                    return (int)ProcessArgsResult.NoAliasedDeviceFound;
                }

                SaveConfiguration(psu, devSettings);
            }

            psu.Disconnect();

            return (int)result;
        }

        private static (string computedConfigHash, string computedDeviceHash, string recordedHash) GetHashes(PowerSupply psu, AliasedDevice devSettings)
        {
            string computedConfigHash = devSettings.Config == null ? string.Empty : devSettings.Config.ComputeConfiguredHash();
            string computedDeviceHash = GetConfigurationHash(psu.SystemParams, psu.Presets);
            string recordedHash = devSettings.Config == null ? string.Empty : devSettings.Config.Hash;
            return (computedConfigHash, computedDeviceHash, recordedHash);
        }

        private static ProcessArgsResult CheckConfiguration(PowerSupply psu, AliasedDevice devSettings, string computedConfigHash, string computedDeviceHash, string recordedHash)
        {
            ProcessArgsResult result = ProcessArgsResult.Ok;

            if ((recordedHash == computedConfigHash) && (recordedHash == computedDeviceHash))
            {
                return result;
            }

            if (!interactiveMode)
            {
                ShowError("Configured state requires review!" + Environment.NewLine);
                if (devSettings.Config == null)
                {
                    Console.WriteLine("No configuration has been saved to check against." + Environment.NewLine);
                    Console.WriteLine("Possible actions:");
                    Console.WriteLine("  1. Remove --check to skip the check and use the current device state.");
                    Console.WriteLine("  2. Use --interactive to interactively resolve the the issue.");
                    Console.WriteLine("  3. Use --save to save this configuration to disk.");
                    result = ProcessArgsResult.NoConfigurationPresent;
                }
                else
                {
                    Console.WriteLine("The device state does not match saved configuration." + Environment.NewLine);
                    Console.WriteLine("Possible actions:");
                    Console.WriteLine("  1. Remove --check to skip the check and use the current device state.");
                    Console.WriteLine("  2. Use --interactive to interactively resolve the the issue.");
                    Console.WriteLine("  3. Use --save to save this configuration to disk.");
                    Console.WriteLine("  4. Use --load to load the device state from disk.");
                    result = ProcessArgsResult.ConfigurationMismatch;
                }
            }
            else
            {
                ShowWarning("Configured state requires review!");
            }

            if (devSettings.Config != null)
            {
                Console.WriteLine();
                Console.WriteLine($"Device Hash   : {computedDeviceHash}");
                Console.WriteLine($"Recorded Hash : {(string.IsNullOrWhiteSpace(recordedHash) ? "None" : recordedHash)}");
                Console.WriteLine($"Config Hash   : {(string.IsNullOrWhiteSpace(computedConfigHash) ? "None" : computedConfigHash)}");
            }

            if (recordedHash != computedConfigHash)
            {
                if (!string.IsNullOrWhiteSpace(recordedHash))
                {
                    Console.WriteLine();
                    ShowWarning("Possible corruption detected in stored settings!");
                }
            }

            if (computedConfigHash != computedDeviceHash)
            {
                Console.WriteLine();
                PrintConfigurationDiff(psu, devSettings.Config);
            }

            if (result != ProcessArgsResult.Ok)
            {
                return result;
            }

            Console.WriteLine();

            List<string> choices = ["Exit now", "Continue", "Save device state (to disk)"];
            if (devSettings.Config != null)
            {
                choices.Add("Load device state (from disk)");
            }

            int choice = GetChoice("Select an action:", choices);
            switch (choice)
            {
                default:
                case 1: // Exit application, make no change.
                    Console.WriteLine("Exiting.");
                    return ProcessArgsResult.OkExitNow;
                case 2: // Continue application, make no change.
                    Console.WriteLine("Continuing.");
                    return ProcessArgsResult.Ok;
                case 3: // Save device state to disk
                    if (!SaveConfiguration(psu, devSettings))
                    {
                        Console.WriteLine("Failed to store settings!");
                        return ProcessArgsResult.StoreError;
                    }
                    Console.WriteLine("Saved configuration.");
                    break;
                case 4: // Load device state from disk.
                    if (devSettings.Config == null)
                    {
                        return ProcessArgsResult.OkExitNow;
                    }
                    LoadConfiguration(psu, devSettings.Config);
                    if (devSettings.Config.Hash != computedConfigHash)
                    {
                        // The settings appear to also need a corrected hash, save now.
                        devSettings.Config.Hash = computedConfigHash;
                        if (!SaveConfiguration(psu, devSettings))
                        {
                            Console.WriteLine("Failed to store settings!");
                            return ProcessArgsResult.StoreError;
                        }
                    }
                    Console.WriteLine("Loaded configuration.");
                    break;
            }

            return ProcessArgsResult.Ok;
        }

        static bool SaveConfiguration(PowerSupply psu, AliasedDevice devSettings)
        {
            if (devSettings.Config == null)
            {
                devSettings.Config = new ConfiguredState();
            }

            devSettings.Config.SystemParams.OPP = psu.SystemParams.OPP;
            devSettings.Config.SystemParams.OTP = psu.SystemParams.OTP;
            devSettings.Config.SystemParams.RPP = psu.SystemParams.RPP;
            devSettings.Config.SystemParams.AutoOn = psu.SystemParams.AutoOn;
            devSettings.Config.SystemParams.Backlight = psu.SystemParams.Backlight;
            devSettings.Config.SystemParams.Volume = psu.SystemParams.Volume;

            devSettings.Config.Presets = new List<PowerSupplySetpoint>(psu.Presets.Length);
            devSettings.Config.Presets.Clear();

            for (int i = 0; i < psu.Presets.Length; i++)
            {
                devSettings.Config.Presets.Add(psu.Presets[i]);
            }

            devSettings.Config.Hash = devSettings.Config.ComputeConfiguredHash();
            if (!settings.Save())
            {
                return false;
            }

            return true;
        }

        static void LoadConfiguration(PowerSupply psu, ConfiguredState? config)
        {
            if (config == null)
            {
                return;
            }

            psu.SetSystemParams(config.SystemParams);
            foreach (var preset in config.Presets)
            {
                psu.SetPreset(preset.GetIndex(), preset);
            }
        }

        // Prints all settings which do not match between device and config
        static int PrintConfigurationDiff(PowerSupply psu, ConfiguredState? config)
        {
            if (config == null)
            {
                Console.WriteLine("Current device configuration:");
                Console.WriteLine();
                ShowConfiguration(psu.SystemParams, psu.Presets);
                return 1;
            }

            Console.WriteLine("Checking configuration differences:");

            int diffCount = ShowDifferences(psu, config);
            if (diffCount == 0)
            {
                Console.WriteLine("- No differences found.");
            }

            return diffCount;
        }

        static string GetConfigurationHash(PowerSupplySystemParams systemParams, PowerSupplySetpoint[] presets)
        {
            using var sha256 = SHA256.Create();
            var sb = new System.Text.StringBuilder();

            // Include settings critical for safe operation.
            sb.Append(systemParams.OPP);
            sb.Append(systemParams.OTP);
            sb.Append(systemParams.RPP);
            sb.Append(systemParams.AutoOn);
            foreach (var preset in presets)
            {
                sb.Append(preset.Voltage);
                sb.Append(preset.Current);
                sb.Append(preset.OVP);
                sb.Append(preset.OCP);
            }

            // Compute the hash
            var hashBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(sb.ToString()));
            return Convert.ToHexString(hashBytes);
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
