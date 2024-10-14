using LibDP100;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.IO;
using System.Threading;
using Spectre.Console;

namespace PowerSupplyApp
{
    partial class Program
    {
        static bool interactiveMode = false;
        static bool wavegenMode = false;

        static void ShowHelp()
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
                "Enabled debug output of underlying driver.");
            grid.AddRow("  [white]--interactive[/]", "",
                "Switches into interactive mode.");
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
                "Reads the actual (sensed) output levels, where [white]CNT[/] is the total number of times to read the output level, and [white]MS[/] is the delay time in milliseconds between each read operation.");
            grid.AddRow("  [white]--read-pre[/], [white]--rp[/]", "[silver][[IDX]] [[CNT]][/]",
                "Reads the preset parameters from the specified preset index, [white]IDX[/] (units: index, range: 0-9, default = 0). The optional [white]CNT[/] parameter allows a range of presets to be read in a single command.");
            grid.AddRow("  [white]--on[/]", "",
                "Sets the output ON.");
            grid.AddRow("  [white]--off[/]", "",
                "Sets the output OFF.");
            grid.AddRow("  [white]--millivolts[/], [white]--mv[/]", "[silver]<MV>[/]",
                "Sets the (maximum) output voltage in millivolts.");
            grid.AddRow("  [white]--milliamps[/], [white]--ma[/]", "[silver]<MA>[/]",
                "Sets the (maximum) output current in milliamps.");
            grid.AddRow("  [white]--ovp[/]", "[silver]<MV>[/]",
                "Sets the Over-Voltage Protection level. Reaching or exceeding this limit will switch the output OFF. (units: mV)");
            grid.AddRow("  [white]--ocp[/]", "[silver]<MA>[/]",
                "Sets the Over-Current Protection level. Reaching or exceeding this limit will switch the output OFF. (units: mA)");
            grid.AddRow("  [white]--opp[/]", "[silver]<DECI_W>[/]",
                "Sets the Over-Power Protection level. Reaching or exceeding this limit will switch the output OFF. (units: 0.1 W, range: 0-1050)");
            grid.AddRow("  [white]--otp[/]", "[silver]<DECI_C>[/]",
                "Sets the Over-Temperature Protection level. Reaching or exceeding this limit will switch the output OFF. (units: 0.1 C, range: 500-800)");

            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("A CLI interface for the AlienTek DP100 100W USB-C digital powersupply.");
            AnsiConsole.WriteLine();
            AnsiConsole.Write(grid);
        }

        static ProcessArgsResult PreProcessArgs(string[] args)
        {
            // First pass, to check for --help or --json args
            if (args.Length > 0)
            {
                foreach (string a in args)
                {
                    string arg = a.ToLower();

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
                        case "--interactive":
                            interactiveMode = true;
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
                        case "--on":
                        case "--off":
                            numSerializedOutputs++;
                            break;
                        default:
                            if (arg.StartsWith("-"))
                            {
                                Console.WriteLine($"Unsupported argument '{arg}'." + Environment.NewLine +
                                                  "Use -?, -h, or --help for help information.");
                                return ProcessArgsResult.UnsupportedOption;
                            }
                            break;
                    }
                }
            }
            else
            {
                ShowHelp();
                return ProcessArgsResult.Error;
            }

            return ProcessArgsResult.Ok;
        }

        static ProcessArgsResult ProcessArgs(PowerSupply inst, string[] args)
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
                            // Do nothing, already handled in first pass.
                            break;

                        case "--interactive":
                            RunInteractiveMode();
                            break;

                        case "--awg":
                        case "--wavegen":
                            if ((i + 1 < args.Length) &&
                                (!args[i + 1].StartsWith("-")))
                            {
                                WaveGen.Init(inst, sp);
                                WaveGen.Load(args[i + 1]);
                                WaveGen.Restart();
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
                            }
                            else
                            {
                                Console.WriteLine($"ERROR: Missing <FILE_PATH> parameter for '{args[i]}'.");
                                return ProcessArgsResult.MissingParameter;
                            }
                            break;

                        case "-d":
                        case "--delay":
                            if ((i + 1 < args.Length) &&
                                (!args[i + 1].StartsWith("-")))
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
                            op = Operation.ReadActuals;
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
                            op = Operation.WritePreset;
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

        static int ProcessWrite(PowerSupply inst, Operation op, string[] args, int index)
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
                    result = inst.SetOutputOn();
                    break;

                case Operation.WriteOutputOff:
                    argsToProcess = 1;
                    result = inst.SetOutputOff();
                    break;

                case Operation.WritePreset:
                    argsToProcess = 2;
                    result = ushort.TryParse(args[index + 1], out parsedValue);
                    if (result)
                    {
                        result = inst.SetOutputToPreset((byte)parsedValue);
                    }
                    break;

                case Operation.WriteVoltage:
                    argsToProcess = 2;
                    result = ushort.TryParse(args[index + 1], out parsedValue);
                    if (result)
                    {
                        sp.Voltage = parsedValue;
                        result = inst.SetSetpoint(sp);
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
                        result = inst.SetSetpoint(sp);
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
                        sp.OVP = parsedValue;
                        result = inst.SetSetpointPreset(inst.Output.Preset, sp);
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
                        sp.OCP = parsedValue;
                        result = inst.SetSetpointPreset(inst.Output.Preset, sp);
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
                        result = inst.SetSystemParams(sys);
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
                        result = inst.SetSystemParams(sys);
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
                        Console.WriteLine("Output: ON");
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
                        Console.WriteLine("Output: OFF");
                    }
                    break;

                case Operation.WritePreset:
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
                        Console.WriteLine($"Set Preset: {parsedValue}");
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
                        Console.WriteLine($"Set voltage (mV): {sp.Voltage}");
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
                        Console.WriteLine($"Set current (mA): {sp.Current}");
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
                        Console.WriteLine($"Set OVP (mV): {sp.OVP}");
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
                        Console.WriteLine($"Set OCP (mA): {sp.OCP}");
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
                        Console.WriteLine($"Set OPP (mW): {sys.OPP}");
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
                        Console.WriteLine($"Set OTP (C): {sys.OTP}");
                    }
                    break;
            }

            return result ? argsToProcess : 0;
        }

        static int CountArgsBetweenFlags(string[] args, int index)
        {
            int count = 0;
            bool betweenFlags = false;

            for (int i = index; i < args.Length; i++)
            {
                if (args[i].StartsWith("--") || args[i].StartsWith("-"))
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
        static int ProcessRead(PowerSupply inst, Operation op, string[] args, int index)
        {
            const byte maxPresetIndex= 9;
            int argsToProcess;
            bool result = true;
            uint loopCount = 1;
            TimeSpan loopDelay = TimeSpan.Zero;
            byte preset = 0;

            int optionalArgCount = CountArgsBetweenFlags(args, index);

            if (op == Operation.ReadActuals && optionalArgCount <= 2)
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
                    case Operation.ReadActuals:
                        result = inst.RefreshActualOutput();
                        break;

                    case Operation.ReadOutput:
                        result = inst.RefreshOutputParams();
                        break;

                    case Operation.ReadDevice:
                        result = inst.RefreshDevInfo();
                        break;

                    case Operation.ReadSystem:
                        result = inst.RefreshSystemParams();
                        break;

                    case Operation.ReadPreset:
                        result = inst.RefreshPreset(preset);
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
                    case Operation.ReadActuals:
                        if (serializeAsJson)
                        {
                            result = SerializeObject(new CommandResponse
                            {
                                Command = op,
                                Response = new { inst.ActualOutput }
                            });
                        }
                        else
                        {
                            Console.WriteLine();
                            result = inst.PrintActualOutput();
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
                            result = inst.PrintOutputParams();
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
                            result = inst.PrintDevInfo();
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
                            result = inst.PrintSystemParams();
                        }

                        break;

                    case Operation.ReadPreset:
                        if (serializeAsJson)
                        {
                            result = SerializeObject(new CommandResponse
                            {
                                Command = op,
                                Response = new {
                                    Index = preset,
                                    Preset = inst.PresetParams[preset]
                                }
                            });
                        }
                        else
                        {
                            Console.WriteLine();
                            result = inst.PrintPreset(preset);
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

        static bool CheckResult(int numArgsProcessed, string[] args, ref int index)
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

        static bool CheckResult(bool result, string arg)
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

        static bool CheckResult(bool result, string[] args, ref int index)
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

        static bool SerializeObject(object response)
        {
            IsoDateTimeConverter dtFmt = new IsoDateTimeConverter()
            {
                DateTimeFormat = "HH:mm:ss.fffffff"
            };

            if (!serializeAsJsonArray)
            {
                serializedOutput++;
                //Console.WriteLine(JsonConvert.SerializeObject(response));
                Console.WriteLine(JsonConvert.SerializeObject(response, dtFmt));
            }
            else
            {
                bool first = serializedOutput == 0;
                serializedOutput++;
                bool last = serializedOutput == numSerializedOutputs;

                if (first)
                {
                    Console.Write('[');
                }

                //Console.Write(JsonConvert.SerializeObject(response));
                Console.WriteLine(JsonConvert.SerializeObject(response, dtFmt));
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

        static void PrintResponse(object param, string message)
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

        static void PrintResponse(object param, string format, ushort arg)
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
