using LibDP100;
using PowerSupplyApp;
using PowerSupplyApp.TUI;
using Spectre.Console;
using System.Text.Json;

namespace PowerSupplyApp
{
    internal partial class Program
    {
        private static bool interactiveMode = false;
        private static bool wavegenMode = false;
        private static bool enumerate = false;
        private static string psuSerialNumber = string.Empty;

        private static bool themeSet = false;
        private static bool pollRateSet = false;
        private static JsonSettings settings = new();

        private static void ShowHelp()
        {
            // Example for generating a sinusoid in powershell.
            // 0..255 | % { $mv = [int](2500 * [Math]::sin(2 * [Math]::PI * $_ / 256) + 2500); "{`"mv`": $mv, `"ma`": 1000}," }
            // 0..127 | % { $mv = [int](1650 * [Math]::sin(2 * [Math]::PI * $_ / 128) + 1650); "{`"mv`": $mv, `"ma`": 1000}," }
            var grid = new Grid();
            grid.AddColumn(new GridColumn().NoWrap());
            grid.AddColumn(new GridColumn().NoWrap());
            grid.AddColumn(new GridColumn().PadLeft(2));
            grid.AddRow("[underline][white]Options:[/][/]");
            grid.AddRow();
            grid.AddRow("  [white]--help[/], [white]-h[/]", "",
                "Displays this help information.");
            grid.AddRow("  [white]--version[/], [white]-v[/]", "",
                "Displays the version of the application.");
            grid.AddRow("  [white]--debug[/]", "",
                "Enables debug output of underlying driver. (Only intended for CLI mode)");
            grid.AddRow("  [white]--enumerate[/]", "",
                "Enumerates all connected power supplies and returns device information for each. If set, all other processing is ignored.");
            grid.AddRow("  [white]--blink[/]", "",
                "For visual identification, the lock indicator will blink 10x (~1x per second).");
            grid.AddRow("  [white]--serial[/], [white]--sn[/]", "[silver]<SERIAL>[/]",
                "Connects to the power supply that matches the specified [white]SERIAL[/] number.");
            grid.AddRow("  [white]--interactive[/]", "[[MS_POLL]]",
                "Switches into an interactive text-based user interface. Set [white]MS_POLL[/] to reduce the update/poll rate (default = 1, range 0-100), value persists between executions. This currently affects the perceived responsiveness of the UI but lowers the CPU utilization.");
            grid.AddRow("  [white]--theme[/]", "<THEME>",
                "Sets the interactive-mode's theme. Persists between executions. The [white]THEME[/] may be one of the following: 'classic', 'black-and-white', 'grey', 'dark-red', 'dark-green', 'dark-magenta', 'cyan', 'gold', 'blue', 'blue-violet'.");
            grid.AddRow("  [white]--json[/]", "",
                "Enables JSON output mode which may be used to integrate with other JSON-compatible tools.");
            grid.AddRow("  [white]--json-list[/]", "",
                "Enables JSON output (as a JSON array). This is the same as [white]--json[/] except the output is encapsulated as an array instead of individual JSON payloads. This may improve compatibility with some JSON tooling.");
            grid.AddRow("  [white]--delay[/], [white]-d[/]", "[silver]<MS>[/]",
                "Specifies the time to delay before processing the next argument. Where [white]MS[/] is the delay time in milliseconds.");
            grid.AddRow("  [white]--preset[/], [white]-p[/]", "[silver]<INDEX>[/]",
                "Loads preset parameters where [white]INDEX[/] is the preset to load. (units: index, range: 0-9).");
            grid.AddRow("  [white]--wavegen[/], [white]--awg[/]", "[silver]<FNAME>[/]",
                "Generates a waveform based on the JSON file specified by [white]FNAME[/]. This file consists of a: 'loop-count', 'milliseconds', and 'points' keys. The 'points' is an array of 'mv' and 'ma' key-values, and an optional 'ms' key-value to delay the execution of the next point.");
            grid.AddRow("  [white]--read-dev[/], [white]--rd[/]", "",
                "Reads the device information.");
            grid.AddRow("  [white]--read-sys[/], [white]--rs[/]", "",
                "Reads the current system parameters.");
            grid.AddRow("  [white]--read-out[/], [white]--ro[/]", "",
                "Reads the current output parameters.");
            grid.AddRow("  [white]--read-act[/], [white]--ra[/]", "[silver][[CNT]] [[MS]][/]",
                "Reads the active status, where [white]CNT[/] is the total number of times to read the status information, and [white]MS[/] is the delay time in milliseconds between each read operation.");
            grid.AddRow("  [white]--read-pre[/], [white]--rp[/]", "[silver][[IDX]] [[CNT]][/]",
                "Reads the preset parameters from the specified preset index, [white]IDX[/] (units: index, range: 0-9, default = 0). The optional [white]CNT[/] parameter allows a range of presets to be read in a single command.");
            grid.AddRow("  [white]--on[/]", "",
                "Sets the output ON.");
            grid.AddRow("  [white]--off[/]", "",
                "Sets the output OFF.");
            grid.AddRow("  [white]--millivolts[/], [white]--mv[/]", "[silver]<MV>[/]",
                "Sets the (maximum) output voltage in millivolts.");
            grid.AddRow("  [white]--milliamps[/], [white]--ma[/]", "[silver]<MA>[/]",
                "Sets the (maximum) output current in milliamperes.");
            grid.AddRow("  [white]--ovp[/]", "[silver]<MV>[/]",
                "Sets the Over-Voltage Protection level. Please note, the current setpoint parameters will be used to update the currently configured preset. Reaching or exceeding this limit will switch the output OFF. (units: mV)");
            grid.AddRow("  [white]--ocp[/]", "[silver]<MA>[/]",
                "Sets the Over-Current Protection level. Please note, the current setpoint parameters will be used to update the currently configured preset. Reaching or exceeding this limit will switch the output OFF. (units: mA)");
            grid.AddRow("  [white]--opp[/]", "[silver]<DECI_W>[/]",
                "Sets the Over-Power Protection level. Reaching or exceeding this limit will switch the output OFF. (units: 0.1 W, range: 0-1050)");
            grid.AddRow("  [white]--otp[/]", "[silver]<DECI_C>[/]",
                "Sets the Over-Temperature Protection level. Reaching or exceeding this limit will switch the output OFF. (units: 0.1 C, range: 500-800)");
            grid.AddRow("  [white]--rpp[/]", "[silver]<STATE>[/]",
                "Sets the Reverse Polarity Protection. (range: 0-1)");
            grid.AddRow("  [white]--auto-on[/]", "[silver]<STATE>[/]",
                "Sets the Automatic Output ON function. When enabled, the output will automatically turn on when the device is powered on. (range: 0-1)");
            grid.AddRow("  [white]--volume[/]", "[silver]<VALUE>[/]",
                "Sets the volume of the device's audible feedback. (range: 0-4)");
            grid.AddRow("  [white]--backlight[/]", "[silver]<BRIGHTNESS>[/]",
                "Sets the brightness of the device's LCD backlight. (range: 0-4)");

            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("A CLI interface for the AlienTek DP100 100W USB-C digital power supply.");
            AnsiConsole.WriteLine();
            AnsiConsole.Write(grid);
        }

        private static ProcessArgsResult PreProcessArgs(string[] args)
        {
            if (!settings.Load())
            {
                Console.WriteLine($"ERROR: Invalid settings, please correct." + Environment.NewLine + settings.GetSettingsFilePath());
                return ProcessArgsResult.Error;
            }

            // First pass, to check for --help or --json args
            if (args.Length > 0)
            {
                int argIndex = 0;
                for (int i = 0; i < args.Length; i++)
                {
                    string arg = args[i].ToLower();

                    switch (arg)
                    {
                        case "-?":
                        case "-h":
                        case "--help":
                            ShowHelp();
                            return ProcessArgsResult.OkExitNow;
                        case "--version":
                        case "-v":
                            Console.WriteLine("v1.0.0");
                            return ProcessArgsResult.OkExitNow;
                        case "--debug":
                            debug = true;
                            break;
                        case "--theme":
                            if ((i + 1 >= args.Length) || args[i + 1].StartsWith('-'))
                            {
                                Console.WriteLine($"ERROR: Missing <THEME_NAME> parameter for '{args[i]}'.");
                                return ProcessArgsResult.MissingParameter;
                            }

                            themeSet = true;
                            settings.Theme = args[i + 1];
                            break;
                        case "--enumerate":
                            enumerate = true;
                            break;
                        case "--serial":
                        case "--sn":
                            if (argIndex + 1 < args.Length)
                            {
                                psuSerialNumber = args[argIndex + 1];
                                if (psuSerialNumber.StartsWith('-'))
                                {
                                    psuSerialNumber = string.Empty;
                                    return ProcessArgsResult.MissingParameter;
                                }
                            }
                            else
                            {
                                return ProcessArgsResult.MissingParameter;
                            }
                            break;
                        case "--interactive":
                            interactiveMode = true;
                            if ((i + 1 < args.Length) && !args[i + 1].StartsWith('-'))
                            {
                                bool result = uint.TryParse(args[i + 1], out uint ms);
                                if (result)
                                {
                                    if (ms > 100)
                                    {
                                        result = false;
                                    }
                                    else
                                    {
                                        pollRateSet = true;
                                        settings.PollRate = ms;
                                    }
                                }

                                if (!result)
                                {
                                    Console.WriteLine($"ERROR: Invalid <MS_POLL> parameter for '{args[i]}'.");
                                    return ProcessArgsResult.InvalidParameter;
                                }
                            }
                            break;
                        case "--json":
                            serializeAsJson = true;
                            break;
                        case "--json-list":
                            serializeAsJson = true;
                            serializeAsJsonArray = true;
                            break;
                        case "--delay":
                        case "-d":
                            break;
                        case "--awg":
                        case "--wavegen":
                            if ((i + 1 >= args.Length) || args[i + 1].StartsWith('-'))
                            {
                                Console.WriteLine($"ERROR: Missing <FILE_PATH> parameter for '{args[i]}'.");
                                return ProcessArgsResult.MissingParameter;
                            }

                            if (!WaveGen.Load(args[i + 1]))
                            {
                                Console.WriteLine(WaveGen.GetLastErrorMessage());
                                return ProcessArgsResult.Error;
                            }
                            break;
                        case "--blink":
                            // Blink is understood as a basic "interactive" mode, but will
                            // not invoke the TUI.
                            interactiveMode = true;
                            break;
                        case "--preset":
                        case "-p":
                        case "--read-out":
                        case "--ro":
                        case "--read-act":
                        case "--ra":
                        case "--read-sys":
                        case "--rs":
                        case "--read-dev":
                        case "--rd":
                        case "--read-pre":
                        case "--rp":
                        case "--millivolts":
                        case "--mv":
                        case "--milliamps":
                        case "--ma":
                        case "--ovp":
                        case "--ocp":
                        case "--opp":
                        case "--otp":
                        case "--rpp":
                        case "--auto-on":
                        case "--volume":
                        case "--backlight":
                        case "--":
                        case "--on":
                        case "--off":
                            numSerializedOutputs++;
                            break;
                        default:
                            if (arg.StartsWith('-'))
                            {
                                Console.WriteLine($"Unsupported argument '{arg}'." + Environment.NewLine +
                                                  "Use -?, -h, or --help for help information.");
                                return ProcessArgsResult.UnsupportedOption;
                            }
                            break;
                    }

                    argIndex++;
                }
            }
            else
            {
                ShowHelp();
                return ProcessArgsResult.Error;
            }

            if (pollRateSet || themeSet)
            {
                if (!settings.Store())
                {
                    Console.WriteLine("Failed to store settings!");
                    return ProcessArgsResult.Error;
                }
            }

            return ProcessArgsResult.Ok;
        }

        private static ProcessArgsResult ProcessArgs(PowerSupply inst, string[] args)
        {
            if (args.Length > 0)
            {
                // Now process each arg
                for (int i = 0; i < args.Length; i++)
                {
                    bool writeOp = false;
                    bool readOp = false;
                    Operation op = Operation.None;
                    string response = string.Empty;
                    bool result;
                    string arg = args[i].ToLower();

                    switch (arg)
                    {
                        default:
                            Console.WriteLine($"Internal error: argument '{arg}' has not been implemented.");
                            return ProcessArgsResult.NotImplemented;

                        case "--json":
                        case "--json-list":
                        case "--debug":
                        case "--enumerate":
                            // Do nothing, already handled in first pass.
                            break;
                        case "--theme":
                            // Do nothing, already handled in first pass.
                            i++;
                            break;

                        case "--serial":
                        case "--sn":
                            // Only increment the index, already handled in first pass.
                            i++;
                            break;

                        case "--blink":
                            RunBlinker();
                            break;

                        case "--interactive":
                            if (pollRateSet)
                            {
                                i++;
                            }

                            theme = settings.GetTheme();
                            RunInteractiveMode(TimeSpan.FromMilliseconds(settings.PollRate));
                            break;

                        case "--awg":
                        case "--wavegen":
                            if (!WaveGen.Init(inst, sp) || !WaveGen.Restart())
                            {
                                Console.WriteLine(WaveGen.GetLastErrorMessage());
                                return ProcessArgsResult.Error;
                            }

                            if (!interactiveMode)
                            {
                                do
                                {
                                    if (serializeAsJson)
                                    {
                                        result = SerializeObject(new CommandResponse
                                        {
                                            Command = Operation.WriteSetpoint,
                                            Response = WaveGen.GetCurrentWaveformPoint()
                                        });
                                    }
                                } while (WaveGen.Run());

                                // TODO: support --json for both command/response.
                                //WaveGen.Generate();
                            }
                            else
                            {
                                wavegenMode = true;
                            }

                            i++;
                            break;

                        case "-d":
                        case "--delay":
                            if ((i + 1 < args.Length) &&
                                (!args[i + 1].StartsWith('-')))
                            {
                                int milliseconds;
                                result = int.TryParse(args[i + 1], out milliseconds);
                                if (result)
                                {
                                    Thread.Sleep(milliseconds);
                                }

                                if (!CheckResult(result, args, ref i))
                                {
                                    return ProcessArgsResult.InvalidParameter;
                                }
                            }
                            else
                            {
                                Console.WriteLine($"ERROR: Missing <DELAY_MS> parameter for '{args[i]}'.");
                                return ProcessArgsResult.MissingParameter;
                            }
                            break;

                        case "--read-act":
                        case "--ra":
                            readOp = true;
                            op = Operation.ReadActState;
                            break;

                        case "--read-out":
                        case "--ro":
                            readOp = true;
                            op = Operation.ReadOutput;
                            break;

                        case "--read-dev":
                        case "--rd":
                            readOp = true;
                            op = Operation.ReadDevice;
                            break;

                        case "--read-sys":
                        case "--rs":
                            readOp = true;
                            op = Operation.ReadSystem;
                            break;

                        case "--read-pre":
                        case "--rp":
                            readOp = true;
                            op = Operation.ReadPreset;
                            break;

                        case "--on":
                            writeOp = true;
                            op = Operation.WriteOutputOn;
                            break;

                        case "--off":
                            writeOp = true;
                            op = Operation.WriteOutputOff;
                            break;

                        case "--preset":
                        case "-p":
                            writeOp = true;
                            op = Operation.UsePreset;
                            break;

                        case "--mv":
                        case "--millivolts":
                            writeOp = true;
                            op = Operation.WriteVoltage;
                            break;

                        case "--ovp":
                            writeOp = true;
                            op = Operation.WriteOVP;
                            break;

                        case "--ma":
                        case "--milliamps":
                            writeOp = true;
                            op = Operation.WriteCurrent;
                            break;

                        case "--ocp":
                            writeOp = true;
                            op = Operation.WriteOCP;
                            break;

                        case "--opp":
                            writeOp = true;
                            op = Operation.WriteOPP;
                            break;

                        case "--otp":
                            writeOp = true;
                            op = Operation.WriteOTP;
                            break;

                        case "--rpp":
                            writeOp = true;
                            op = Operation.WriteRPP;
                            break;

                        case "--auto-on":
                            writeOp = true;
                            op = Operation.WriteAutoOn;
                            break;

                        case "--volume":
                            writeOp = true;
                            op = Operation.WriteVolume;
                            break;

                        case "--backlight":
                            writeOp = true;
                            op = Operation.WriteBacklight;
                            break;
                    }

                    if (readOp)
                    {
                        if (!CheckResult(ProcessRead(inst, op, args, i), args, ref i))
                        {
                            return ProcessArgsResult.ReadError;
                        }
                    }
                    else if (writeOp)
                    {
                        if (!CheckResult(ProcessWrite(inst, op, args, i), args, ref i))
                        {
                            return ProcessArgsResult.WriteError;
                        }
                    }
                }
            }

            return 0;
        }

        private static int ProcessWrite(PowerSupply inst, Operation op, string[] args, int index)
        {
            bool result;
            ushort parsedValue = 0;
            string formatMessage = string.Empty;
            int argsToProcess = 0;

            switch (op)
            {
                default:
                    return argsToProcess;

                case Operation.WriteOutputOn:
                    argsToProcess = 1;
                    result = inst.SetOutputOn() == PowerSupplyResult.OK;
                    break;

                case Operation.WriteOutputOff:
                    argsToProcess = 1;
                    result = inst.SetOutputOff() == PowerSupplyResult.OK;
                    break;

                case Operation.UsePreset:
                    argsToProcess = 2;
                    result = ushort.TryParse(args[index + 1], out parsedValue);
                    if (result)
                    {
                        result = inst.UsePreset((byte)parsedValue) == PowerSupplyResult.OK;
                    }
                    break;

                case Operation.WriteVoltage:
                    argsToProcess = 2;
                    result = ushort.TryParse(args[index + 1], out parsedValue);
                    if (result)
                    {
                        sp.Voltage = parsedValue;
                        result = inst.SetOutput(sp) == PowerSupplyResult.OK;
                        if (!result)
                        {
                            sp.Copy(inst.Output.Setpoint);
                        }
                    }
                    break;

                case Operation.WriteCurrent:
                    argsToProcess = 2;
                    result = ushort.TryParse(args[index + 1], out parsedValue);
                    if (result)
                    {
                        sp.Current = parsedValue;
                        result = inst.SetOutput(sp) == PowerSupplyResult.OK;
                        if (!result)
                        {
                            sp.Copy(inst.Output.Setpoint);
                        }
                    }
                    break;

                case Operation.WriteOVP:
                    argsToProcess = 2;
                    result = ushort.TryParse(args[index + 1], out parsedValue);
                    if (result)
                    {
                        // TODO: consider reworking this to use inst.Preset where only OVP is modified.
                        // Perhaps also add "--write-pre <PRESET> <MV> <MA> <OVP> <OCP>"
                        sp.OVP = parsedValue;
                        result = inst.SetPreset(inst.Output.Preset, sp) == PowerSupplyResult.OK;
                        if (!result)
                        {
                            sp.Copy(inst.Output.Setpoint);
                        }
                    }
                    break;

                case Operation.WriteOCP:
                    argsToProcess = 2;
                    result = ushort.TryParse(args[index + 1], out parsedValue);
                    if (result)
                    {
                        // TODO: consider reworking this to use inst.Preset where only OVP is modified.
                        // Perhaps also add "--write-pre <PRESET> <MV> <MA> <OVP> <OCP>"
                        sp.OCP = parsedValue;
                        result = inst.SetPreset(inst.Output.Preset, sp) == PowerSupplyResult.OK;
                        if (!result)
                        {
                            sp.Copy(inst.Output.Setpoint);
                        }
                    }
                    break;

                case Operation.WriteOPP:
                    argsToProcess = 2;
                    result = ushort.TryParse(args[index + 1], out parsedValue);
                    if (result)
                    {
                        sys.OPP = parsedValue;
                        result = inst.SetOTP(sys.OPP) == PowerSupplyResult.OK;
                        if (!result)
                        {
                            sys.Copy(inst.SystemParams);
                        }
                    }
                    break;

                case Operation.WriteOTP:
                    argsToProcess = 2;
                    result = ushort.TryParse(args[index + 1], out parsedValue);
                    if (result)
                    {
                        sys.OTP = parsedValue;
                        result = inst.SetOTP(sys.OTP) == PowerSupplyResult.OK;
                        if (!result)
                        {
                            sys.Copy(inst.SystemParams);
                        }
                    }
                    break;

                case Operation.WriteRPP:
                    argsToProcess = 2;
                    result = ushort.TryParse(args[index + 1], out parsedValue);
                    if (result)
                    {
                        sys.RPP = parsedValue != 0;
                        result = inst.SetRPP(sys.RPP) == PowerSupplyResult.OK;
                        if (!result)
                        {
                            sys.Copy(inst.SystemParams);
                        }
                    }
                    break;

                case Operation.WriteAutoOn:
                    argsToProcess = 2;
                    result = ushort.TryParse(args[index + 1], out parsedValue);
                    if (result)
                    {
                        sys.AutoOn = parsedValue != 0;
                        result = inst.SetAutoOn(sys.AutoOn) == PowerSupplyResult.OK;
                        if (!result)
                        {
                            sys.Copy(inst.SystemParams);
                        }
                    }
                    break;

                case Operation.WriteVolume:
                    argsToProcess = 2;
                    result = ushort.TryParse(args[index + 1], out parsedValue);
                    if (result)
                    {
                        sys.Volume = (byte)parsedValue;
                        result = inst.SetVolume(sys.Volume) == PowerSupplyResult.OK;
                        if (!result)
                        {
                            sys.Copy(inst.SystemParams);
                        }
                    }
                    break;

                case Operation.WriteBacklight:
                    argsToProcess = 2;
                    result = ushort.TryParse(args[index + 1], out parsedValue);
                    if (result)
                    {
                        sys.Backlight = (byte)parsedValue;
                        result = inst.SetBacklight(sys.Backlight) == PowerSupplyResult.OK;
                        if (!result)
                        {
                            sys.Copy(inst.SystemParams);
                        }
                    }
                    break;
            }

            if (!result)
            {
                // TODO: more detailed error codes should be reported here for debugging purposes.
                // Ex: "Not connected", "Out of Range", "Invalid command", "Invalid state"
                SerializeObject(new CommandResponse
                {
                    Command = op,
                    Response = new { Error = "Operation Failed" }
                });
                return 0;
            }

            switch (op)
            {
                default:
                    return 0;

                case Operation.WriteOutputOn:
                    // TODO: refactor this case and the other cases to reduce code redundancy.
                    // PrintResponse(dp100.Output.State, "Output: ON");
                    if (serializeAsJson)
                    {
                        result = SerializeObject(new CommandResponse
                        {
                            Command = op,
                            Response = new { inst.Output.On }
                        });
                    }
                    else
                    {
                        Console.WriteLine("Set Output : ON");
                    }
                    break;

                case Operation.WriteOutputOff:
                    if (serializeAsJson)
                    {
                        result = SerializeObject(new CommandResponse
                        {
                            Command = op,
                            Response = new { inst.Output.On }
                        });
                    }
                    else
                    {
                        Console.WriteLine("Set Output : OFF");
                    }
                    break;

                case Operation.UsePreset:
                    if (serializeAsJson)
                    {
                        result = SerializeObject(new CommandResponse
                        {
                            Command = op,
                            Response = new { inst.Output.Preset, inst.Output.Setpoint }
                        });
                    }
                    else
                    {
                        Console.WriteLine($"Set Preset     : {inst.Output.Preset}");
                        Console.WriteLine($"  Voltage (mV) : {inst.Output.Setpoint.Voltage}");
                        Console.WriteLine($"  Current (mA) : {inst.Output.Setpoint.Current}");
                        Console.WriteLine($"  OVP (mV)     : {inst.Output.Setpoint.OVP}");
                        Console.WriteLine($"  OCP (mA)     : {inst.Output.Setpoint.OCP}");
                    }
                    break;

                case Operation.WriteVoltage:
                    if (serializeAsJson)
                    {
                        result = SerializeObject(new CommandResponse
                        {
                            Command = op,
                            Response = inst.Output.Setpoint
                        });
                    }
                    else
                    {
                        Console.WriteLine($"Set Voltage (mV) : {sp.Voltage}");
                    }
                    break;

                case Operation.WriteCurrent:
                    if (serializeAsJson)
                    {
                        result = SerializeObject(new CommandResponse
                        {
                            Command = op,
                            Response = inst.Output.Setpoint
                        });
                    }
                    else
                    {
                        Console.WriteLine($"Set Current (mA) : {sp.Current}");
                    }
                    break;

                case Operation.WriteOVP:
                    if (serializeAsJson)
                    {
                        result = SerializeObject(new CommandResponse
                        {
                            Command = op,
                            Response = inst.Output.Setpoint
                        });
                    }
                    else
                    {
                        Console.WriteLine($"Set OVP (mV) : {sp.OVP}");
                    }
                    break;

                case Operation.WriteOCP:
                    if (serializeAsJson)
                    {
                        result = SerializeObject(new CommandResponse
                        {
                            Command = op,
                            Response = inst.Output.Setpoint
                        });
                    }
                    else
                    {
                        Console.WriteLine($"Set OCP (mA) : {sp.OCP}");
                    }
                    break;

                case Operation.WriteOPP:
                    if (serializeAsJson)
                    {
                        result = SerializeObject(new CommandResponse
                        {
                            Command = op,
                            Response = inst.SystemParams
                        });
                    }
                    else
                    {
                        Console.WriteLine($"Set OPP (mW) : {sys.OPP}");
                    }
                    break;

                case Operation.WriteOTP:
                    if (serializeAsJson)
                    {
                        result = SerializeObject(new CommandResponse
                        {
                            Command = op,
                            Response = inst.SystemParams
                        });
                    }
                    else
                    {
                        Console.WriteLine($"Set OTP (C) : {sys.OTP}");
                    }
                    break;

                case Operation.WriteRPP:
                    if (serializeAsJson)
                    {
                        result = SerializeObject(new CommandResponse
                        {
                            Command = op,
                            Response = inst.SystemParams
                        });
                    }
                    else
                    {
                        Console.WriteLine($"Set RPP : {sys.RPP}");
                    }
                    break;

                case Operation.WriteAutoOn:
                    if (serializeAsJson)
                    {
                        result = SerializeObject(new CommandResponse
                        {
                            Command = op,
                            Response = inst.SystemParams
                        });
                    }
                    else
                    {
                        Console.WriteLine($"Set AutoOn : {sys.AutoOn}");
                    }
                    break;

                case Operation.WriteVolume:
                    if (serializeAsJson)
                    {
                        result = SerializeObject(new CommandResponse
                        {
                            Command = op,
                            Response = inst.SystemParams
                        });
                    }
                    else
                    {
                        Console.WriteLine($"Set Volume : {sys.Volume}");
                    }
                    break;

                case Operation.WriteBacklight:
                    if (serializeAsJson)
                    {
                        result = SerializeObject(new CommandResponse
                        {
                            Command = op,
                            Response = inst.SystemParams
                        });
                    }
                    else
                    {
                        Console.WriteLine($"Set Backlight : {sys.Backlight}");
                    }
                    break;
            }

            return result ? argsToProcess : 0;
        }

        private static int CountArgsBetweenFlags(string[] args, int index)
        {
            int count = 0;
            bool betweenFlags = false;

            for (int i = index; i < args.Length; i++)
            {
                if (args[i].StartsWith("--") || args[i].StartsWith('-'))
                {
                    if (betweenFlags)
                    {
                        // Second flag/option detected, stop counting
                        break;
                    }
                    betweenFlags = true;
                    continue;
                }

                if (betweenFlags)
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>
        /// Handles read operations.
        /// </summary>
        /// <param name="inst"></param>
        /// <param name="op"></param>
        /// <param name="args"></param>
        /// <param name="index"></param>
        /// <returns>The number of arguments processed. <= 0 indicates an error.</returns>
        private static int ProcessRead(PowerSupply inst, Operation op, string[] args, int index)
        {
            const byte maxPresetIndex = 9;
            int argsToProcess;
            bool result = true;
            uint loopCount = 1;
            TimeSpan loopDelay = TimeSpan.Zero;
            byte preset = 0;

            int optionalArgCount = CountArgsBetweenFlags(args, index);

            if (op == Operation.ReadActState && optionalArgCount <= 2)
            {
                uint ms = 0;
                if ((optionalArgCount == 0) ||
                    ((optionalArgCount == 1) && uint.TryParse(args[index + 1], out loopCount)) ||
                    ((optionalArgCount == 2) && uint.TryParse(args[index + 1], out loopCount) && uint.TryParse(args[index + 2], out ms)))
                {
                    loopDelay = TimeSpan.FromMilliseconds(ms);
                }
                else
                {
                    // Parsing error.
                    return 0;
                }
            }
            else if (op == Operation.ReadPreset && optionalArgCount <= 2)
            {
                if ((optionalArgCount == 0) ||
                    ((optionalArgCount == 1) && byte.TryParse(args[index + 1], out preset)) ||
                    ((optionalArgCount == 2) && byte.TryParse(args[index + 1], out preset) && uint.TryParse(args[index + 2], out loopCount)))
                {
                    if (preset > maxPresetIndex || loopCount == 0)
                    {
                        return 0;
                    }
                }
                else
                {
                    // Parsing error.
                    return 0;
                }
            }
            else if (optionalArgCount == 0)
            {
                // Nothing to do.
            }
            else
            {
                // Wrong number of parameters.
                return 0;
            }

            argsToProcess = optionalArgCount + 1;

            if (loopCount == 0)
            {
                return 0;
            }
            else
            {
                numSerializedOutputs--;
                numSerializedOutputs += (int)loopCount;
            }

            for (int l = 0; l < loopCount; l++)
            {
                switch (op)
                {
                    case Operation.ReadActState:
                        result = inst.GetActiveStatus() == PowerSupplyResult.OK;
                        break;

                    case Operation.ReadOutput:
                        result = inst.GetOutput() == PowerSupplyResult.OK;
                        break;

                    case Operation.ReadDevice:
                        result = inst.GetDeviceInfo() == PowerSupplyResult.OK;
                        break;

                    case Operation.ReadSystem:
                        result = inst.GetSystemParams() == PowerSupplyResult.OK;
                        break;

                    case Operation.ReadPreset:
                        result = inst.GetPreset(preset) == PowerSupplyResult.OK;
                        break;

                    default:
                        result = false;
                        break;
                }

                if (!result)
                {
                    break;
                }

                switch (op)
                {
                    case Operation.ReadActState:
                        if (serializeAsJson)
                        {
                            result = SerializeObject(new CommandResponse
                            {
                                Command = op,
                                Response = new { inst.ActiveState }
                            });
                        }
                        else
                        {
                            Console.WriteLine();
                            result = true;
                            inst.ActiveState.Print();
                        }

                        break;

                    case Operation.ReadOutput:
                        if (serializeAsJson)
                        {
                            result = SerializeObject(new CommandResponse
                            {
                                Command = op,
                                Response = new { inst.Output }
                            });
                        }
                        else
                        {
                            Console.WriteLine();
                            result = true;
                            inst.Output.Print();
                        }

                        break;

                    case Operation.ReadDevice:
                        if (serializeAsJson)
                        {
                            result = SerializeObject(new CommandResponse
                            {
                                Command = op,
                                Response = new { inst.Device }
                            });
                        }
                        else
                        {
                            Console.WriteLine();
                            result = true;
                            inst.Device.Print();
                        }

                        break;

                    case Operation.ReadSystem:
                        if (serializeAsJson)
                        {
                            result = SerializeObject(new CommandResponse
                            {
                                Command = op,
                                Response = new { inst.SystemParams }
                            });
                        }
                        else
                        {
                            Console.WriteLine();
                            result = true;
                            inst.SystemParams.Print();
                        }

                        break;

                    case Operation.ReadPreset:
                        if (serializeAsJson)
                        {
                            result = SerializeObject(new CommandResponse
                            {
                                Command = op,
                                Response = new
                                {
                                    Index = preset,
                                    Preset = inst.Presets[preset]
                                }
                            });
                        }
                        else
                        {
                            Console.WriteLine();
                            result = true;
                            inst.Presets[preset].Print();
                        }

                        preset++;
                        if (preset > maxPresetIndex)
                        {
                            break;
                        }

                        break;

                    default:
                        result = false;
                        break;
                }

                Thread.Sleep(loopDelay);
            }

            return result ? argsToProcess : 0;
        }

        private static bool CheckResult(int numArgsProcessed, string[] args, ref int index)
        {
            if (numArgsProcessed > 0)
            {
                index += (numArgsProcessed - 1);
                return true;
            }
            else
            {
                // TODO improve, output each unprocessed arg.
                Console.WriteLine($"Could not process '{args[index]}'");
                return false;
            }
        }

        private static bool CheckResult(bool result, string arg)
        {
            if (result)
            {
                return true;
            }
            else
            {
                Console.WriteLine($"Could not process '{arg}'");
                return false;
            }
        }

        private static bool CheckResult(bool result, string[] args, ref int index)
        {
            if (result)
            {
                index++;
                return true;
            }
            else
            {
                Console.WriteLine($"Could not process '{args[index]}' ({args[index + 1]})");
                return false;
            }
        }

        private static bool SerializeObject(object? response)
        {
            JsonSerializerOptions options = new JsonSerializerOptions
            {
                Converters = { new CustomDateTimeConverter() }
            };

            if (!serializeAsJsonArray)
            {
                serializedOutput++;
                Console.WriteLine(JsonSerializer.Serialize(response, options));
            }
            else
            {
                bool first = serializedOutput == 0;
                serializedOutput++;
                bool last = serializedOutput >= numSerializedOutputs;

                if (first)
                {
                    Console.Write('[');
                }

                if (response != null)
                {
                    Console.WriteLine(JsonSerializer.Serialize(response, options));
                }

                if (last)
                {
                    Console.Write(']');
                }
                else
                {
                    Console.Write(',');
                }
            }

            return true;
        }

        private static void PrintResponse(object param, string message)
        {
            if (serializeAsJson)
            {
                SerializeObject(param);
            }
            else
            {
                Console.WriteLine(message);
            }
        }

        private static void PrintResponse(object param, string format, ushort arg)
        {
            if (serializeAsJson)
            {
                SerializeObject(param);
            }
            else
            {
                string message = string.Format(format, arg);
                Console.WriteLine(message);
            }
        }
    }
}
