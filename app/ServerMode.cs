using LibDP100;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UsbPowerSupply = LibDP100.PowerSupply;

namespace PowerSupplyApp
{
    internal partial class Program
    {
        private static readonly object scpiCommandLock = new();
        private static volatile bool scpiServerRefreshInProgress;

        private sealed class ScpiServerSession
        {
            public ScpiServerSession(IPowerSupplyBackend powerSupply, string serialNumber, bool debugMode, bool showTui, TimeSpan sleepTime)
            {
                PowerSupply = powerSupply;
                SerialNumber = serialNumber;
                DebugMode = debugMode;
                ShowTui = showTui;
                SleepTime = sleepTime;
            }

            public IPowerSupplyBackend PowerSupply { get; set; }

            public string SerialNumber { get; }

            public bool DebugMode { get; }

            public bool ShowTui { get; }

            public bool IsHeadless => !ShowTui;

            public TimeSpan SleepTime { get; }
        }

        private static bool TryParseServerEndpoint(string endpoint, out string address, out int port)
        {
            address = string.Empty;
            port = 0;

            if (string.IsNullOrWhiteSpace(endpoint))
            {
                return false;
            }

            string hostPart;
            string portPart;
            string trimmed = endpoint.Trim();

            if (trimmed.StartsWith('['))
            {
                int closeBracket = trimmed.IndexOf(']');
                if (closeBracket <= 1 || closeBracket + 2 >= trimmed.Length || trimmed[closeBracket + 1] != ':')
                {
                    return false;
                }

                hostPart = trimmed[1..closeBracket];
                portPart = trimmed[(closeBracket + 2)..];
            }
            else
            {
                int separator = trimmed.LastIndexOf(':');
                if (separator <= 0 || separator >= trimmed.Length - 1)
                {
                    return false;
                }

                hostPart = trimmed[..separator];
                portPart = trimmed[(separator + 1)..];
            }

            if (!IPAddress.TryParse(hostPart, out _))
            {
                return false;
            }

            if (!int.TryParse(portPart, NumberStyles.Integer, CultureInfo.InvariantCulture, out port) || port is < 1 or > 65535)
            {
                return false;
            }

            address = hostPart;
            return true;
        }

        private static void RunHeadlessServerMode(TimeSpan sleepTime, bool debug, string bindAddress, int bindPort)
        {
            RunScpiServerMode(sleepTime, debug, bindAddress, bindPort, showTui: false);
        }

        private static void RunInteractiveServerMode(TimeSpan sleepTime, bool debug, string bindAddress, int bindPort)
        {
            RunScpiServerMode(sleepTime, debug, bindAddress, bindPort, showTui: true);
        }

        private static void RunScpiServerMode(TimeSpan sleepTime, bool debug, string bindAddress, int bindPort, bool showTui)
        {
            if (psu == null)
            {
                return;
            }

            if (!IPAddress.TryParse(bindAddress, out var ipAddress))
            {
                ShowError($"Invalid endpoint '{bindAddress}:{bindPort}'.");
                return;
            }

            runInteractive = true;
            Console.CancelKeyPress += OnCancelKeyPress;
            Console.Title = $"{psu.Device.Type} [server]";

            ScpiServerSession session = new(psu, psu.Device.SerialNumber, debug, showTui, sleepTime);

            // Main() has already initialized the device state before entering server mode.
            // Avoid reloading again here because Reload() mutates volatile preset state.
            Thread.Sleep(100);
            SynchronizeServerMirrors(session.PowerSupply);
            session.PowerSupply.DebugMode = false;

            Thread? serverThread = null;

            try
            {
                if (showTui)
                {
                    session.PowerSupply.ActiveStateEvent += ReceiveActiveState;
                    EnterAlternateScreenBuffer();
                    session.PowerSupply.StartWorkerThread(sleepTime);

                    serverThread = new Thread(() => RunScpiServerLoop(session, sleepTime, ipAddress, bindAddress, bindPort, announce: false))
                    {
                        IsBackground = true,
                        Name = "ScpiServer"
                    };

                    serverThread.Start();

                    if (wavegenMode)
                    {
                        selectedRow = -1;
                        RunWaveGenMode();
                    }
                    else
                    {
                        RunNormalMode();
                    }
                }
                else
                {
                    session.PowerSupply.DebugMode = debug;
                    RunScpiServerLoop(session, sleepTime, ipAddress, bindAddress, bindPort, announce: true);
                }
            }
            finally
            {
                runInteractive = false;
                if (showTui)
                {
                    serverThread?.Join(TimeSpan.FromSeconds(2));
                    session.PowerSupply.StopWorkerThread();
                    session.PowerSupply.ActiveStateEvent -= ReceiveActiveState;
                    ExitAlternateScreenBuffer();
                }

                Console.CancelKeyPress -= OnCancelKeyPress;
                session.PowerSupply.DebugMode = debug;
                psu = session.PowerSupply;
            }
        }

        private static void RunScpiServerLoop(ScpiServerSession session, TimeSpan sleepTime, IPAddress ipAddress, string bindAddress, int bindPort, bool announce)
        {
            TcpListener listener = new(ipAddress, bindPort);
            listener.Start();

            if (announce)
            {
                Console.WriteLine($"SCPI-like server listening on {bindAddress}:{bindPort}");
                Console.WriteLine("Use SYST:HELP? for supported commands. Use EXIT to stop server.");
            }

            try
            {
                while (runInteractive && session.PowerSupply.Connected)
                {
                    if (!listener.Pending())
                    {
                        Thread.Sleep(sleepTime);
                        continue;
                    }

                    using TcpClient client = listener.AcceptTcpClient();
                    HandleScpiClient(session, client);
                }
            }
            finally
            {
                listener.Stop();
            }
        }

        private static void HandleScpiClient(ScpiServerSession session, TcpClient client)
        {
            using NetworkStream stream = client.GetStream();
            using StreamReader reader = new(stream, Encoding.ASCII, detectEncodingFromByteOrderMarks: false, bufferSize: 1024, leaveOpen: true);
            using StreamWriter writer = new(stream, Encoding.ASCII, bufferSize: 1024, leaveOpen: true)
            {
                NewLine = "\n",
                AutoFlush = true
            };

            writer.WriteLine("VICON-SCPI READY");

            while (runInteractive && client.Connected)
            {
                string? line = reader.ReadLine();
                if (line == null)
                {
                    break;
                }

                string response;
                bool closeClient;
                bool stopServer;
                bool stateChanged;

                lock (scpiCommandLock)
                {
                    response = ProcessScpiLine(session, line, out closeClient, out stopServer, out stateChanged);

                    if (stateChanged)
                    {
                        SynchronizeInteractiveState(session.PowerSupply);
                    }
                }

                writer.WriteLine(response);

                if (stopServer)
                {
                    runInteractive = false;
                    break;
                }

                if (closeClient)
                {
                    break;
                }
            }
        }

        private static string ProcessScpiLine(ScpiServerSession session, string line, out bool closeClient, out bool stopServer, out bool stateChanged)
        {
            closeClient = false;
            stopServer = false;
            stateChanged = false;

            string[] commands = line.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (commands.Length == 0)
            {
                return "ERR:EMPTY";
            }

            List<string> responses = new(commands.Length);
            foreach (var command in commands)
            {
                string response = ProcessScpiCommand(session.PowerSupply, command, out bool close, out bool stop, out bool changed);
                responses.Add(response);

                if (changed)
                {
                    if (!TryRefreshServerSession(session))
                    {
                        responses[^1] = "ERR:IO";
                        closeClient = true;
                        stopServer = true;
                        break;
                    }
                }

                stateChanged |= changed;

                if (close)
                {
                    closeClient = true;
                }

                if (stop)
                {
                    stopServer = true;
                }

                if (closeClient || stopServer)
                {
                    break;
                }
            }

            return string.Join(';', responses);
        }

        private static void SynchronizeServerMirrors(IPowerSupplyBackend inst)
        {
            sys = new PowerSupplySystemParams(inst.SystemParams);
            sp = new PowerSupplySetpoint(inst.Output.Setpoint);
        }

        private static bool TryRefreshServerSession(ScpiServerSession session)
        {
            IPowerSupplyBackend previous = session.PowerSupply;
            scpiServerRefreshInProgress = true;

            try
            {
                if (!session.IsHeadless)
                {
                    previous.StopWorkerThread();
                    previous.ActiveStateEvent -= ReceiveActiveState;
                }

                previous.Disconnect();
                Thread.Sleep(150);

                if (!TryCreateHeadlessServerPowerSupply(session.SerialNumber, session.DebugMode, out IPowerSupplyBackend replacement))
                {
                    return false;
                }

                if (!session.IsHeadless)
                {
                    replacement.DebugMode = false;
                    replacement.ActiveStateEvent += ReceiveActiveState;
                    replacement.StartWorkerThread(session.SleepTime);
                }

                session.PowerSupply = replacement;
                psu = replacement;
                SynchronizeServerMirrors(replacement);
                return true;
            }
            finally
            {
                scpiServerRefreshInProgress = false;
            }
        }

        private static bool TryCreateHeadlessServerPowerSupply(string serialNumber, bool debugMode, out IPowerSupplyBackend powerSupply)
        {
            UsbPowerSupply usbPowerSupply = new()
            {
                DebugMode = debugMode
            };

            powerSupply = new UsbPowerSupplyBackend(usbPowerSupply);

            if ((powerSupply.Connect(serialNumber) != PowerSupplyResult.OK) ||
                (powerSupply.GetDeviceInfo() != PowerSupplyResult.OK) ||
                (powerSupply.GetSystemParams() != PowerSupplyResult.OK) ||
                (powerSupply.Reload() != PowerSupplyResult.OK))
            {
                powerSupply.Disconnect();
                return false;
            }

            return true;
        }

        private static string ProcessScpiCommand(IPowerSupplyBackend inst, string command, out bool closeClient, out bool stopServer, out bool stateChanged)
        {
            closeClient = false;
            stopServer = false;
            stateChanged = false;

            string cmd = NormalizeScpiCommand(command.Trim());
            if (string.IsNullOrEmpty(cmd))
            {
                return "ERR:EMPTY";
            }

            if (cmd.Equals("QUIT", StringComparison.OrdinalIgnoreCase))
            {
                closeClient = true;
                return "BYE";
            }

            if (cmd.Equals("EXIT", StringComparison.OrdinalIgnoreCase))
            {
                stopServer = true;
                closeClient = true;
                return "BYE";
            }

            if (cmd.Equals("*IDN?", StringComparison.OrdinalIgnoreCase))
            {
                bool ok = inst.GetDeviceInfo() == PowerSupplyResult.OK;
                return ok
                    ? $"AlienTek,{inst.Device.Type},{inst.Device.SerialNumber},{inst.Device.SoftwareVersion}"
                    : "ERR:IO";
            }

            if (cmd.Equals("*RST", StringComparison.OrdinalIgnoreCase))
            {
                return inst.Reload() == PowerSupplyResult.OK ? "OK" : "ERR:IO";
            }

            if (cmd.Equals("SYST:HELP?", StringComparison.OrdinalIgnoreCase))
            {
                return "*IDN?,SYST:DEV?,OUTP?,OUTP <0|1|OFF|ON>,PRES?,PRES:READ? <0-9>,PRES:WRITE <0-9>,<mV>,<mA>,<ovp>,<ocp>,SOUR:VOLT?,SOUR:VOLT <mV>,SOUR:CURR?,SOUR:CURR <mA>,SOUR:VOLT:PROT?,SOUR:VOLT:PROT <mV>,SOUR:CURR:PROT?,SOUR:CURR:PROT <mA>,SYST:PROT:POW?,SYST:PROT:POW <0.1W>,SYST:PROT:TEMP?,SYST:PROT:TEMP <C>,SYST:RPP?,SYST:RPP <0|1>,SYST:AUTO?,SYST:AUTO <0|1>,SYST:VOL?,SYST:VOL <0-4>,SYST:BACK?,SYST:BACK <0-4>,MEAS:VOLT?,MEAS:CURR?,MEAS:POW?,MEAS:ALL?,PRES:USE <0-9>,PRES:RECALL <0-9>,QUIT,EXIT (long-form keywords such as SYSTEM, SOURCE, OUTPUT, PRESET, MEASURE, PROTECTION, VOLTAGE, CURRENT, POWER, TEMPERATURE, DEVICE, VOLUME, BACKLIGHT are also accepted)";
            }

            if (cmd.Equals("SYST:ERR?", StringComparison.OrdinalIgnoreCase))
            {
                return "0,No error";
            }

            if (cmd.Equals("SYST:DEV?", StringComparison.OrdinalIgnoreCase))
            {
                bool ok = inst.GetDeviceInfo() == PowerSupplyResult.OK;
                return ok
                    ? FormattableString.Invariant($"{inst.Device.Type},{inst.Device.SerialNumber},{inst.Device.MfgDate},{inst.Device.HardwareVersion},{inst.Device.SoftwareVersion},{inst.Device.BootloaderVersion},{inst.Device.SoftwareState}")
                    : "ERR:IO";
            }

            if (cmd.Equals("OUTP?", StringComparison.OrdinalIgnoreCase))
            {
                return inst.GetOutput() == PowerSupplyResult.OK ? (inst.Output.On ? "1" : "0") : "ERR:IO";
            }

            if (cmd.Equals("PRES?", StringComparison.OrdinalIgnoreCase))
            {
                return inst.GetOutput() == PowerSupplyResult.OK
                    ? inst.Output.Preset.ToString(CultureInfo.InvariantCulture)
                    : "ERR:IO";
            }

            if (TryParseByteSet(cmd, "PRES:READ?", out byte presetIndex) && presetIndex <= 9)
            {
                if (inst.GetPreset(presetIndex) != PowerSupplyResult.OK)
                {
                    return "ERR:IO";
                }

                var presetData = inst.Presets[presetIndex];
                return FormattableString.Invariant($"{presetData.Voltage},{presetData.Current},{presetData.OVP},{presetData.OCP}");
            }

            if (TryParsePresetWrite(cmd, out presetIndex, out ushort presetVoltage, out ushort presetCurrent, out ushort presetOvp, out ushort presetOcp) && presetIndex <= 9)
            {
                PowerSupplyResult result = inst.SetPreset(presetIndex, presetVoltage, presetCurrent, presetOvp, presetOcp);
                stateChanged = result == PowerSupplyResult.OK;
                return result == PowerSupplyResult.OK ? "OK" : "ERR:IO";
            }

            if (TryParseBooleanSet(cmd, "OUTP", out bool requestedOutputOn))
            {
                var result = requestedOutputOn ? inst.SetOutputOn() : inst.SetOutputOff();
                stateChanged = result == PowerSupplyResult.OK;
                return result == PowerSupplyResult.OK ? "OK" : "ERR:IO";
            }

            if (cmd.Equals("SOUR:VOLT?", StringComparison.OrdinalIgnoreCase))
            {
                return inst.GetOutput() == PowerSupplyResult.OK
                    ? inst.Output.Setpoint.Voltage.ToString(CultureInfo.InvariantCulture)
                    : "ERR:IO";
            }

            if (TryParseUShortSet(cmd, "SOUR:VOLT", out ushort millivolts))
            {
                if (inst.GetOutput() != PowerSupplyResult.OK)
                {
                    return "ERR:IO";
                }

                sp = new PowerSupplySetpoint(inst.Output.Setpoint)
                {
                    Voltage = millivolts
                };

                PowerSupplyResult result = inst.SetOutput(sp);

                if (result != PowerSupplyResult.OK)
                {
                    sp.Copy(inst.Output.Setpoint);
                    return "ERR:IO";
                }

                stateChanged = true;
                return "OK";
            }

            if (cmd.Equals("SOUR:CURR?", StringComparison.OrdinalIgnoreCase))
            {
                return inst.GetOutput() == PowerSupplyResult.OK
                    ? inst.Output.Setpoint.Current.ToString(CultureInfo.InvariantCulture)
                    : "ERR:IO";
            }

            if (TryParseUShortSet(cmd, "SOUR:CURR", out ushort milliamps))
            {
                if (inst.GetOutput() != PowerSupplyResult.OK)
                {
                    return "ERR:IO";
                }

                sp = new PowerSupplySetpoint(inst.Output.Setpoint)
                {
                    Current = milliamps
                };

                PowerSupplyResult result = inst.SetOutput(sp);

                if (result != PowerSupplyResult.OK)
                {
                    sp.Copy(inst.Output.Setpoint);
                    return "ERR:IO";
                }

                stateChanged = true;
                return "OK";
            }

            if (cmd.Equals("SOUR:VOLT:PROT?", StringComparison.OrdinalIgnoreCase))
            {
                return inst.GetOutput() == PowerSupplyResult.OK
                    ? inst.Output.Setpoint.OVP.ToString(CultureInfo.InvariantCulture)
                    : "ERR:IO";
            }

            if (TryParseUShortSet(cmd, "SOUR:VOLT:PROT", out ushort ovp))
            {
                if (inst.GetOutput() != PowerSupplyResult.OK)
                {
                    return "ERR:IO";
                }

                if (inst.GetPreset(inst.Output.Preset) != PowerSupplyResult.OK)
                {
                    return "ERR:IO";
                }

                sp = new PowerSupplySetpoint(inst.Presets[inst.Output.Preset]);
                sp.OVP = ovp;

                if (inst.SetPreset(inst.Output.Preset, sp) != PowerSupplyResult.OK)
                {
                    return "ERR:IO";
                }

                stateChanged = true;
                return "OK";
            }

            if (cmd.Equals("SOUR:CURR:PROT?", StringComparison.OrdinalIgnoreCase))
            {
                return inst.GetOutput() == PowerSupplyResult.OK
                    ? inst.Output.Setpoint.OCP.ToString(CultureInfo.InvariantCulture)
                    : "ERR:IO";
            }

            if (TryParseUShortSet(cmd, "SOUR:CURR:PROT", out ushort ocp))
            {
                if (inst.GetOutput() != PowerSupplyResult.OK)
                {
                    return "ERR:IO";
                }

                if (inst.GetPreset(inst.Output.Preset) != PowerSupplyResult.OK)
                {
                    return "ERR:IO";
                }

                sp = new PowerSupplySetpoint(inst.Presets[inst.Output.Preset]);
                sp.OCP = ocp;

                if (inst.SetPreset(inst.Output.Preset, sp) != PowerSupplyResult.OK)
                {
                    return "ERR:IO";
                }

                stateChanged = true;
                return "OK";
            }

            if (cmd.Equals("SYST:PROT:POW?", StringComparison.OrdinalIgnoreCase))
            {
                return inst.GetSystemParams() == PowerSupplyResult.OK
                    ? inst.SystemParams.OPP.ToString(CultureInfo.InvariantCulture)
                    : "ERR:IO";
            }

            if (TryParseUShortSet(cmd, "SYST:PROT:POW", out ushort opp))
            {
                sys.OPP = opp;
                if (inst.SetOPP(sys.OPP) != PowerSupplyResult.OK)
                {
                    sys.Copy(inst.SystemParams);
                    return "ERR:IO";
                }

                stateChanged = true;
                return "OK";
            }

            if (cmd.Equals("SYST:PROT:TEMP?", StringComparison.OrdinalIgnoreCase))
            {
                return inst.GetSystemParams() == PowerSupplyResult.OK
                    ? inst.SystemParams.OTP.ToString(CultureInfo.InvariantCulture)
                    : "ERR:IO";
            }

            if (TryParseUShortSet(cmd, "SYST:PROT:TEMP", out ushort otp))
            {
                sys.OTP = otp;
                if (inst.SetOTP(sys.OTP) != PowerSupplyResult.OK)
                {
                    sys.Copy(inst.SystemParams);
                    return "ERR:IO";
                }

                stateChanged = true;
                return "OK";
            }

            if (cmd.Equals("SYST:RPP?", StringComparison.OrdinalIgnoreCase))
            {
                return inst.GetSystemParams() == PowerSupplyResult.OK
                    ? (inst.SystemParams.RPP ? "1" : "0")
                    : "ERR:IO";
            }

            if (TryParseBooleanSet(cmd, "SYST:RPP", out bool rpp))
            {
                sys.RPP = rpp;
                if (inst.SetRPP(sys.RPP) != PowerSupplyResult.OK)
                {
                    sys.Copy(inst.SystemParams);
                    return "ERR:IO";
                }

                stateChanged = true;
                return "OK";
            }

            if (cmd.Equals("SYST:AUTO?", StringComparison.OrdinalIgnoreCase))
            {
                return inst.GetSystemParams() == PowerSupplyResult.OK
                    ? (inst.SystemParams.AutoOn ? "1" : "0")
                    : "ERR:IO";
            }

            if (TryParseBooleanSet(cmd, "SYST:AUTO", out bool autoOn))
            {
                sys.AutoOn = autoOn;
                if (inst.SetAutoOn(sys.AutoOn) != PowerSupplyResult.OK)
                {
                    sys.Copy(inst.SystemParams);
                    return "ERR:IO";
                }

                stateChanged = true;
                return "OK";
            }

            if (cmd.Equals("SYST:VOL?", StringComparison.OrdinalIgnoreCase))
            {
                return inst.GetSystemParams() == PowerSupplyResult.OK
                    ? inst.SystemParams.Volume.ToString(CultureInfo.InvariantCulture)
                    : "ERR:IO";
            }

            if (TryParseUShortSet(cmd, "SYST:VOL", out ushort volume) && volume <= byte.MaxValue)
            {
                sys.Volume = (byte)volume;
                if (inst.SetVolume(sys.Volume) != PowerSupplyResult.OK)
                {
                    sys.Copy(inst.SystemParams);
                    return "ERR:IO";
                }

                stateChanged = true;
                return "OK";
            }

            if (cmd.Equals("SYST:BACK?", StringComparison.OrdinalIgnoreCase))
            {
                return inst.GetSystemParams() == PowerSupplyResult.OK
                    ? inst.SystemParams.Backlight.ToString(CultureInfo.InvariantCulture)
                    : "ERR:IO";
            }

            if (TryParseUShortSet(cmd, "SYST:BACK", out ushort backlight) && backlight <= byte.MaxValue)
            {
                sys.Backlight = (byte)backlight;
                if (inst.SetBacklight(sys.Backlight) != PowerSupplyResult.OK)
                {
                    sys.Copy(inst.SystemParams);
                    return "ERR:IO";
                }

                stateChanged = true;
                return "OK";
            }

            if (cmd.Equals("MEAS:VOLT?", StringComparison.OrdinalIgnoreCase))
            {
                return inst.GetActiveStatus() == PowerSupplyResult.OK
                    ? inst.ActiveState.Voltage.ToString(CultureInfo.InvariantCulture)
                    : "ERR:IO";
            }

            if (cmd.Equals("MEAS:CURR?", StringComparison.OrdinalIgnoreCase))
            {
                return inst.GetActiveStatus() == PowerSupplyResult.OK
                    ? inst.ActiveState.Current.ToString(CultureInfo.InvariantCulture)
                    : "ERR:IO";
            }

            if (cmd.Equals("MEAS:POW?", StringComparison.OrdinalIgnoreCase))
            {
                if (inst.GetActiveStatus() != PowerSupplyResult.OK)
                {
                    return "ERR:IO";
                }

                double powerMilliwatts = (inst.ActiveState.Voltage * inst.ActiveState.Current) / 1000.0;
                return powerMilliwatts.ToString("F1", CultureInfo.InvariantCulture);
            }

            if (cmd.Equals("MEAS:ALL?", StringComparison.OrdinalIgnoreCase))
            {
                if (inst.GetActiveStatus() != PowerSupplyResult.OK)
                {
                    return "ERR:IO";
                }

                double powerMilliwatts = (inst.ActiveState.Voltage * inst.ActiveState.Current) / 1000.0;
                return FormattableString.Invariant($"{inst.ActiveState.VoltageInput},{inst.ActiveState.Voltage},{inst.ActiveState.Current},{powerMilliwatts:F1},{inst.ActiveState.VoltageOutputMax},{inst.ActiveState.Temperature1},{inst.ActiveState.Temperature2},{inst.ActiveState.VoltageUsb5V},{(int)inst.ActiveState.OutputMode},{(int)inst.ActiveState.FaultStatus}");
            }

            if (TryParseByteSet(cmd, "PRES:USE", out byte presetNumber) && presetNumber <= 9)
            {
                var result = inst.UsePreset(presetNumber, fromNonVolatile: true);
                stateChanged = result == PowerSupplyResult.OK;
                return result == PowerSupplyResult.OK ? "OK" : "ERR:IO";
            }

            if (TryParseByteSet(cmd, "PRES:RECALL", out presetNumber) && presetNumber <= 9)
            {
                var result = inst.UsePreset(presetNumber, fromNonVolatile: false);
                stateChanged = result == PowerSupplyResult.OK;
                return result == PowerSupplyResult.OK ? "OK" : "ERR:IO";
            }

            return "ERR:UNSUPPORTED";
        }

        private static void SynchronizeInteractiveState(IPowerSupplyBackend inst)
        {
            if (!interactiveMode)
            {
                return;
            }

            if (inst.GetOutput() != PowerSupplyResult.OK)
            {
                return;
            }

            if (inst.GetPreset(inst.Output.Preset) != PowerSupplyResult.OK)
            {
                return;
            }

            if (inst.GetSystemParams() != PowerSupplyResult.OK)
            {
                return;
            }

            if (inst.GetActiveStatus() != PowerSupplyResult.OK)
            {
                return;
            }

            sp = new PowerSupplySetpoint(inst.Output.Setpoint);
            sys = new PowerSupplySystemParams(inst.SystemParams);
            ReceiveActiveState(inst.ActiveState);
        }

        private static ProcessArgsResult ProcessScpiClientArgs(string[] args)
        {
            try
            {
                using TcpClient client = new();
                client.Connect(scpiEndpointAddress, scpiEndpointPort);

                using NetworkStream stream = client.GetStream();
                using StreamReader reader = new(stream, Encoding.ASCII, detectEncodingFromByteOrderMarks: false, bufferSize: 1024, leaveOpen: true);
                using StreamWriter writer = new(stream, Encoding.ASCII, bufferSize: 1024, leaveOpen: true)
                {
                    NewLine = "\n",
                    AutoFlush = true
                };

                _ = reader.ReadLine();

                for (int i = 0; i < args.Length; i++)
                {
                    bool writeOp = false;
                    bool readOp = false;
                    Operation op = Operation.None;
                    string arg = args[i].ToLowerInvariant();

                    switch (arg)
                    {
                        default:
                            if (arg.StartsWith('-'))
                            {
                                ShowError($"Unsupported argument '{arg}'.");
                                return ProcessArgsResult.UnsupportedOption;
                            }
                            break;

                        case "--json":
                        case "--json-list":
                        case "--debug":
                        case "--version":
                        case "-v":
                        case "--config":
                        case "--help":
                        case "-h":
                        case "-?":
                            break;

                        case "--client":
                            i++;
                            break;

                        case "--serial":
                        case "--sn":
                            ShowError("--serial is not supported when running in --client mode.");
                            return ProcessArgsResult.InvalidParameter;

                        case "--interactive":
                        case "--blink":
                        case "--server":
                        case "--save":
                        case "--load":
                        case "--check":
                        case "--enumerate":
                        case "--wavegen":
                        case "--awg":
                        case "--theme":
                            ShowError($"{arg} is not supported when running in --client mode.");
                            return ProcessArgsResult.InvalidParameter;

                        case "-d":
                        case "--delay":
                            if ((i + 1 < args.Length) && (!args[i + 1].StartsWith('-')))
                            {
                                if (!int.TryParse(args[i + 1], out int milliseconds))
                                {
                                    return ProcessArgsResult.InvalidParameter;
                                }

                                Thread.Sleep(milliseconds);
                                i++;
                                break;
                            }

                            ShowError($"Missing <DELAY_MS> parameter for '{args[i]}'.");
                            return ProcessArgsResult.MissingParameter;

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

                        case "--recall":
                        case "-r":
                            writeOp = true;
                            op = Operation.RecallPreset;
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
                        if (!CheckResult(op, ProcessScpiRead(op, args, i, reader, writer), args, ref i))
                        {
                            return ProcessArgsResult.ReadError;
                        }
                    }
                    else if (writeOp)
                    {
                        if (!CheckResult(op, ProcessScpiWrite(op, args, i, reader, writer), args, ref i))
                        {
                            return ProcessArgsResult.WriteError;
                        }
                    }
                }

                return ProcessArgsResult.Ok;
            }
            catch (SocketException ex)
            {
                ShowError($"Could not connect to SCPI server at {scpiEndpointAddress}:{scpiEndpointPort}." +
                    (debug ? Environment.NewLine + ex.Message : string.Empty));
                return ProcessArgsResult.Error;
            }
            catch (IOException ex)
            {
                ShowError($"SCPI client I/O failed." + (debug ? Environment.NewLine + ex.Message : string.Empty));
                return ProcessArgsResult.Error;
            }
        }

        private static int ProcessScpiRead(Operation op, string[] args, int index, StreamReader reader, StreamWriter writer)
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
                    return 0;
                }
            }
            else if (optionalArgCount != 0)
            {
                return 0;
            }

            argsToProcess = optionalArgCount + 1;

            if (loopCount == 0)
            {
                return 0;
            }

            numSerializedOutputs--;
            numSerializedOutputs += (int)loopCount;

            for (int l = 0; l < loopCount; l++)
            {
                switch (op)
                {
                    case Operation.ReadActState:
                        result = TryReadRemoteActiveState(reader, writer, out PowerSupplyActiveState activeState);
                        if (!result)
                        {
                            break;
                        }

                        if (serializeAsJson)
                        {
                            result = SerializeObject(new CommandResponse
                            {
                                Command = op,
                                Response = new { ActiveState = activeState }
                            });
                        }
                        else
                        {
                            Console.WriteLine();
                            activeState.Print();
                        }
                        break;

                    case Operation.ReadOutput:
                        result = TryReadRemoteOutput(reader, writer, out PowerSupplyOutput output);
                        if (!result)
                        {
                            break;
                        }

                        if (serializeAsJson)
                        {
                            result = SerializeObject(new CommandResponse
                            {
                                Command = op,
                                Response = new { Output = output }
                            });
                        }
                        else
                        {
                            Console.WriteLine();
                            output.Print();
                        }
                        break;

                    case Operation.ReadDevice:
                        result = TryReadRemoteDeviceInfo(reader, writer, out PowerSupplyInfo deviceInfo);
                        if (!result)
                        {
                            break;
                        }

                        if (serializeAsJson)
                        {
                            result = SerializeObject(new CommandResponse
                            {
                                Command = op,
                                Response = new { Device = deviceInfo }
                            });
                        }
                        else
                        {
                            Console.WriteLine();
                            deviceInfo.Print();
                        }
                        break;

                    case Operation.ReadSystem:
                        result = TryReadRemoteSystemParams(reader, writer, out PowerSupplySystemParams systemParams);
                        if (!result)
                        {
                            break;
                        }

                        if (serializeAsJson)
                        {
                            result = SerializeObject(new CommandResponse
                            {
                                Command = op,
                                Response = new { SystemParams = systemParams }
                            });
                        }
                        else
                        {
                            Console.WriteLine();
                            systemParams.Print();
                        }
                        break;

                    case Operation.ReadPreset:
                        result = TryReadRemotePreset(reader, writer, preset, out PowerSupplySetpoint presetData);
                        if (!result)
                        {
                            break;
                        }

                        if (serializeAsJson)
                        {
                            result = SerializeObject(new CommandResponse
                            {
                                Command = op,
                                Response = new
                                {
                                    Index = preset,
                                    Preset = presetData
                                }
                            });
                        }
                        else
                        {
                            Console.WriteLine();
                            presetData.Print();
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

                if (!result)
                {
                    break;
                }

                Thread.Sleep(loopDelay);
            }

            return result ? argsToProcess : 0;
        }

        private static int ProcessScpiWrite(Operation op, string[] args, int index, StreamReader reader, StreamWriter writer)
        {
            bool result = false;
            ushort parsedValue = 0;
            byte presetIndex = 0;
            int argsToProcess = 0;

            switch (op)
            {
                default:
                    return 0;

                case Operation.WriteOutputOn:
                    argsToProcess = 1;
                    result = TrySendScpiCommand(reader, writer, "OUTP ON", out _);
                    break;

                case Operation.WriteOutputOff:
                    argsToProcess = 1;
                    result = TrySendScpiCommand(reader, writer, "OUTP OFF", out _);
                    break;

                case Operation.UsePreset:
                    argsToProcess = 2;
                    if ((index + 1 >= args.Length) || !byte.TryParse(args[index + 1], out presetIndex))
                    {
                        break;
                    }

                    result = TrySendScpiCommand(reader, writer, FormattableString.Invariant($"PRES:USE {presetIndex}"), out _);
                    break;

                case Operation.RecallPreset:
                    argsToProcess = 2;
                    if ((index + 1 >= args.Length) || !byte.TryParse(args[index + 1], out presetIndex))
                    {
                        break;
                    }

                    result = TrySendScpiCommand(reader, writer, FormattableString.Invariant($"PRES:RECALL {presetIndex}"), out _);
                    break;

                case Operation.WriteVoltage:
                    argsToProcess = 2;
                    if ((index + 1 >= args.Length) || !ushort.TryParse(args[index + 1], out parsedValue))
                    {
                        break;
                    }

                    result = TrySendScpiCommand(reader, writer, FormattableString.Invariant($"SOUR:VOLT {parsedValue}"), out _);
                    break;

                case Operation.WriteCurrent:
                    argsToProcess = 2;
                    if ((index + 1 >= args.Length) || !ushort.TryParse(args[index + 1], out parsedValue))
                    {
                        break;
                    }

                    result = TrySendScpiCommand(reader, writer, FormattableString.Invariant($"SOUR:CURR {parsedValue}"), out _);
                    break;

                case Operation.WriteOVP:
                    argsToProcess = 2;
                    if ((index + 1 >= args.Length) || !ushort.TryParse(args[index + 1], out parsedValue))
                    {
                        break;
                    }

                    result = TrySendScpiCommand(reader, writer, FormattableString.Invariant($"SOUR:VOLT:PROT {parsedValue}"), out _);
                    break;

                case Operation.WriteOCP:
                    argsToProcess = 2;
                    if ((index + 1 >= args.Length) || !ushort.TryParse(args[index + 1], out parsedValue))
                    {
                        break;
                    }

                    result = TrySendScpiCommand(reader, writer, FormattableString.Invariant($"SOUR:CURR:PROT {parsedValue}"), out _);
                    break;

                case Operation.WriteOPP:
                    argsToProcess = 2;
                    if ((index + 1 >= args.Length) || !ushort.TryParse(args[index + 1], out parsedValue))
                    {
                        break;
                    }

                    result = TrySendScpiCommand(reader, writer, FormattableString.Invariant($"SYST:PROT:POW {parsedValue}"), out _);
                    break;

                case Operation.WriteOTP:
                    argsToProcess = 2;
                    if ((index + 1 >= args.Length) || !ushort.TryParse(args[index + 1], out parsedValue))
                    {
                        break;
                    }

                    result = TrySendScpiCommand(reader, writer, FormattableString.Invariant($"SYST:PROT:TEMP {parsedValue}"), out _);
                    break;

                case Operation.WriteRPP:
                    argsToProcess = 2;
                    if ((index + 1 >= args.Length) || !ushort.TryParse(args[index + 1], out parsedValue))
                    {
                        break;
                    }

                    result = TrySendScpiCommand(reader, writer, FormattableString.Invariant($"SYST:RPP {(parsedValue != 0 ? 1 : 0)}"), out _);
                    break;

                case Operation.WriteAutoOn:
                    argsToProcess = 2;
                    if ((index + 1 >= args.Length) || !ushort.TryParse(args[index + 1], out parsedValue))
                    {
                        break;
                    }

                    result = TrySendScpiCommand(reader, writer, FormattableString.Invariant($"SYST:AUTO {(parsedValue != 0 ? 1 : 0)}"), out _);
                    break;

                case Operation.WriteVolume:
                    argsToProcess = 2;
                    if ((index + 1 >= args.Length) || !ushort.TryParse(args[index + 1], out parsedValue))
                    {
                        break;
                    }

                    result = TrySendScpiCommand(reader, writer, FormattableString.Invariant($"SYST:VOL {parsedValue}"), out _);
                    break;

                case Operation.WriteBacklight:
                    argsToProcess = 2;
                    if ((index + 1 >= args.Length) || !ushort.TryParse(args[index + 1], out parsedValue))
                    {
                        break;
                    }

                    result = TrySendScpiCommand(reader, writer, FormattableString.Invariant($"SYST:BACK {parsedValue}"), out _);
                    break;
            }

            if (!result)
            {
                return 0;
            }

            switch (op)
            {
                case Operation.WriteOutputOn:
                case Operation.WriteOutputOff:
                    result = TryReadRemoteOutput(reader, writer, out PowerSupplyOutput outputState);
                    if (!result)
                    {
                        return 0;
                    }

                    if (serializeAsJson)
                    {
                        result = SerializeObject(new CommandResponse
                        {
                            Command = op,
                            Response = new { outputState.On }
                        });
                    }
                    else
                    {
                        Console.WriteLine($"Set Output : {(outputState.On ? "ON" : "OFF")}");
                    }
                    break;

                case Operation.UsePreset:
                case Operation.RecallPreset:
                case Operation.WriteVoltage:
                case Operation.WriteCurrent:
                case Operation.WriteOVP:
                case Operation.WriteOCP:
                    result = TryReadRemoteOutput(reader, writer, out PowerSupplyOutput output);
                    if (!result)
                    {
                        return 0;
                    }

                    if (serializeAsJson)
                    {
                        result = SerializeObject(new CommandResponse
                        {
                            Command = op,
                            Response = output
                        });
                    }
                    else
                    {
                        switch (op)
                        {
                            case Operation.UsePreset:
                            case Operation.RecallPreset:
                                Console.WriteLine($"Set Preset     : {output.Preset}");
                                Console.WriteLine($"  Voltage (mV) : {output.Setpoint.Voltage}");
                                Console.WriteLine($"  Current (mA) : {output.Setpoint.Current}");
                                Console.WriteLine($"  OVP (mV)     : {output.Setpoint.OVP}");
                                Console.WriteLine($"  OCP (mA)     : {output.Setpoint.OCP}");
                                break;
                            case Operation.WriteVoltage:
                                Console.WriteLine($"Set Voltage (mV) : {output.Setpoint.Voltage}");
                                break;
                            case Operation.WriteCurrent:
                                Console.WriteLine($"Set Current (mA) : {output.Setpoint.Current}");
                                break;
                            case Operation.WriteOVP:
                                Console.WriteLine($"Set OVP (mV) : {output.Setpoint.OVP}");
                                break;
                            case Operation.WriteOCP:
                                Console.WriteLine($"Set OCP (mA) : {output.Setpoint.OCP}");
                                break;
                        }
                    }
                    break;

                case Operation.WriteOPP:
                case Operation.WriteOTP:
                case Operation.WriteRPP:
                case Operation.WriteAutoOn:
                case Operation.WriteVolume:
                case Operation.WriteBacklight:
                    result = TryReadRemoteSystemParams(reader, writer, out PowerSupplySystemParams systemParams);
                    if (!result)
                    {
                        return 0;
                    }

                    if (serializeAsJson)
                    {
                        result = SerializeObject(new CommandResponse
                        {
                            Command = op,
                            Response = systemParams
                        });
                    }
                    else
                    {
                        switch (op)
                        {
                            case Operation.WriteOPP:
                                Console.WriteLine($"Set OPP (mW) : {systemParams.OPP}");
                                break;
                            case Operation.WriteOTP:
                                Console.WriteLine($"Set OTP (C) : {systemParams.OTP}");
                                break;
                            case Operation.WriteRPP:
                                Console.WriteLine($"Set RPP : {systemParams.RPP}");
                                break;
                            case Operation.WriteAutoOn:
                                Console.WriteLine($"Set AutoOn : {systemParams.AutoOn}");
                                break;
                            case Operation.WriteVolume:
                                Console.WriteLine($"Set Volume : {systemParams.Volume}");
                                break;
                            case Operation.WriteBacklight:
                                Console.WriteLine($"Set Backlight : {systemParams.Backlight}");
                                break;
                        }
                    }
                    break;
            }

            return result ? argsToProcess : 0;
        }

        private static bool TrySendScpiCommand(StreamReader reader, StreamWriter writer, string command, out string response)
        {
            writer.WriteLine(command);
            response = reader.ReadLine() ?? string.Empty;
            return !string.IsNullOrWhiteSpace(response) && !response.StartsWith("ERR", StringComparison.OrdinalIgnoreCase);
        }

        private static bool TryReadRemoteDeviceInfo(StreamReader reader, StreamWriter writer, out PowerSupplyInfo deviceInfo)
        {
            deviceInfo = new PowerSupplyInfo();

            if (TrySendScpiCommand(reader, writer, "SYST:DEV?", out string response))
            {
                string[] parts = response.Split(',', StringSplitOptions.TrimEntries);
                if (parts.Length == 7)
                {
                    deviceInfo.Type = parts[0];
                    deviceInfo.SerialNumber = parts[1];
                    deviceInfo.MfgDate = parts[2];
                    deviceInfo.HardwareVersion = parts[3];
                    deviceInfo.SoftwareVersion = parts[4];
                    deviceInfo.BootloaderVersion = parts[5];
                    deviceInfo.SoftwareState = parts[6];
                    return true;
                }
            }

            if (!TrySendScpiCommand(reader, writer, "*IDN?", out string idnResponse))
            {
                return false;
            }

            string[] fields = idnResponse.Split(',', StringSplitOptions.TrimEntries);
            if (fields.Length < 4)
            {
                return false;
            }

            deviceInfo.Type = fields[1];
            deviceInfo.SerialNumber = fields[2];
            deviceInfo.SoftwareVersion = fields[3];
            return true;
        }

        private static bool TryReadRemoteOutput(StreamReader reader, StreamWriter writer, out PowerSupplyOutput output)
        {
            output = new PowerSupplyOutput();

            if (!TrySendScpiCommand(reader, writer, "OUTP?", out string stateResponse) ||
                !TrySendScpiCommand(reader, writer, "PRES?", out string presetResponse) ||
                !TrySendScpiCommand(reader, writer, "SOUR:VOLT?", out string voltageResponse) ||
                !TrySendScpiCommand(reader, writer, "SOUR:CURR?", out string currentResponse) ||
                !TrySendScpiCommand(reader, writer, "SOUR:VOLT:PROT?", out string ovpResponse) ||
                !TrySendScpiCommand(reader, writer, "SOUR:CURR:PROT?", out string ocpResponse))
            {
                return false;
            }

            if (!TryParseScpiBool(stateResponse, out bool on) ||
                !byte.TryParse(presetResponse, NumberStyles.Integer, CultureInfo.InvariantCulture, out byte preset) ||
                !ushort.TryParse(voltageResponse, NumberStyles.Integer, CultureInfo.InvariantCulture, out ushort voltage) ||
                !ushort.TryParse(currentResponse, NumberStyles.Integer, CultureInfo.InvariantCulture, out ushort current) ||
                !ushort.TryParse(ovpResponse, NumberStyles.Integer, CultureInfo.InvariantCulture, out ushort ovp) ||
                !ushort.TryParse(ocpResponse, NumberStyles.Integer, CultureInfo.InvariantCulture, out ushort ocp))
            {
                return false;
            }

            output.On = on;
            output.Preset = preset;
            output.Setpoint = new PowerSupplySetpoint(preset)
            {
                Voltage = voltage,
                Current = current,
                OVP = ovp,
                OCP = ocp
            };

            return true;
        }

        private static bool TryReadRemoteSystemParams(StreamReader reader, StreamWriter writer, out PowerSupplySystemParams systemParams)
        {
            systemParams = new PowerSupplySystemParams();

            if (!TrySendScpiCommand(reader, writer, "SYST:PROT:POW?", out string oppResponse) ||
                !TrySendScpiCommand(reader, writer, "SYST:PROT:TEMP?", out string otpResponse) ||
                !TrySendScpiCommand(reader, writer, "SYST:RPP?", out string rppResponse) ||
                !TrySendScpiCommand(reader, writer, "SYST:AUTO?", out string autoOnResponse) ||
                !TrySendScpiCommand(reader, writer, "SYST:VOL?", out string volumeResponse) ||
                !TrySendScpiCommand(reader, writer, "SYST:BACK?", out string backlightResponse))
            {
                return false;
            }

            if (!ushort.TryParse(oppResponse, NumberStyles.Integer, CultureInfo.InvariantCulture, out ushort opp) ||
                !ushort.TryParse(otpResponse, NumberStyles.Integer, CultureInfo.InvariantCulture, out ushort otp) ||
                !TryParseScpiBool(rppResponse, out bool rpp) ||
                !TryParseScpiBool(autoOnResponse, out bool autoOn) ||
                !byte.TryParse(volumeResponse, NumberStyles.Integer, CultureInfo.InvariantCulture, out byte volume) ||
                !byte.TryParse(backlightResponse, NumberStyles.Integer, CultureInfo.InvariantCulture, out byte backlight))
            {
                return false;
            }

            systemParams.OPP = opp;
            systemParams.OTP = otp;
            systemParams.RPP = rpp;
            systemParams.AutoOn = autoOn;
            systemParams.Volume = volume;
            systemParams.Backlight = backlight;
            return true;
        }

        private static bool TryReadRemoteActiveState(StreamReader reader, StreamWriter writer, out PowerSupplyActiveState activeState)
        {
            activeState = new PowerSupplyActiveState
            {
                Timestamp = DateTime.Now
            };

            if (!TrySendScpiCommand(reader, writer, "MEAS:ALL?", out string response))
            {
                return false;
            }

            string[] parts = response.Split(',', StringSplitOptions.TrimEntries);
            if (parts.Length == 10 &&
                ushort.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out ushort voltageInput) &&
                ushort.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out ushort voltage) &&
                ushort.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out ushort current) &&
                double.TryParse(parts[3], NumberStyles.Float, CultureInfo.InvariantCulture, out _) &&
                ushort.TryParse(parts[4], NumberStyles.Integer, CultureInfo.InvariantCulture, out ushort voltageOutputMax) &&
                ushort.TryParse(parts[5], NumberStyles.Integer, CultureInfo.InvariantCulture, out ushort temp1) &&
                ushort.TryParse(parts[6], NumberStyles.Integer, CultureInfo.InvariantCulture, out ushort temp2) &&
                ushort.TryParse(parts[7], NumberStyles.Integer, CultureInfo.InvariantCulture, out ushort voltageUsb5V) &&
                byte.TryParse(parts[8], NumberStyles.Integer, CultureInfo.InvariantCulture, out byte outputMode) &&
                byte.TryParse(parts[9], NumberStyles.Integer, CultureInfo.InvariantCulture, out byte faultStatus))
            {
                activeState.VoltageInput = voltageInput;
                activeState.Voltage = voltage;
                activeState.Current = current;
                activeState.VoltageOutputMax = voltageOutputMax;
                activeState.Temperature1 = temp1;
                activeState.Temperature2 = temp2;
                activeState.VoltageUsb5V = voltageUsb5V;
                activeState.OutputMode = (PowerSupplyOutputMode)outputMode;
                activeState.FaultStatus = (PowerSupplyFaultStatus)faultStatus;
                return true;
            }

            if (parts.Length != 7 ||
                !ushort.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out ushort legacyVoltage) ||
                !ushort.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out ushort legacyCurrent) ||
                !double.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out _) ||
                !ushort.TryParse(parts[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out ushort legacyTemp1) ||
                !ushort.TryParse(parts[4], NumberStyles.Integer, CultureInfo.InvariantCulture, out ushort legacyTemp2) ||
                !byte.TryParse(parts[5], NumberStyles.Integer, CultureInfo.InvariantCulture, out byte legacyOutputMode) ||
                !byte.TryParse(parts[6], NumberStyles.Integer, CultureInfo.InvariantCulture, out byte legacyFaultStatus))
            {
                return false;
            }

            activeState.Voltage = legacyVoltage;
            activeState.Current = legacyCurrent;
            activeState.Temperature1 = legacyTemp1;
            activeState.Temperature2 = legacyTemp2;
            activeState.OutputMode = (PowerSupplyOutputMode)legacyOutputMode;
            activeState.FaultStatus = (PowerSupplyFaultStatus)legacyFaultStatus;
            return true;
        }

        private static bool TryReadRemotePreset(StreamReader reader, StreamWriter writer, byte preset, out PowerSupplySetpoint setpoint)
        {
            setpoint = new PowerSupplySetpoint(preset);

            if (!TrySendScpiCommand(reader, writer, FormattableString.Invariant($"PRES:READ? {preset}"), out string response))
            {
                return false;
            }

            string[] parts = response.Split(',', StringSplitOptions.TrimEntries);
            if (parts.Length != 4 ||
                !ushort.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out ushort voltage) ||
                !ushort.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out ushort current) ||
                !ushort.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out ushort ovp) ||
                !ushort.TryParse(parts[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out ushort ocp))
            {
                return false;
            }

            setpoint.Voltage = voltage;
            setpoint.Current = current;
            setpoint.OVP = ovp;
            setpoint.OCP = ocp;
            return true;
        }

        private static bool TryParseScpiBool(string response, out bool value)
        {
            value = false;

            if (response.Equals("1", StringComparison.OrdinalIgnoreCase) ||
                response.Equals("ON", StringComparison.OrdinalIgnoreCase) ||
                response.Equals("TRUE", StringComparison.OrdinalIgnoreCase))
            {
                value = true;
                return true;
            }

            if (response.Equals("0", StringComparison.OrdinalIgnoreCase) ||
                response.Equals("OFF", StringComparison.OrdinalIgnoreCase) ||
                response.Equals("FALSE", StringComparison.OrdinalIgnoreCase))
            {
                value = false;
                return true;
            }

            return false;
        }

        private static bool TryParseUShortSet(string command, string prefix, out ushort value)
        {
            value = 0;
            if (!TryParseCommandValue(command, prefix, out string raw))
            {
                return false;
            }

            return ushort.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
        }

        private static bool TryParseByteSet(string command, string prefix, out byte value)
        {
            value = 0;
            if (!TryParseCommandValue(command, prefix, out string raw))
            {
                return false;
            }

            return byte.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
        }

        private static bool TryParseBooleanSet(string command, string prefix, out bool value)
        {
            value = false;
            if (!TryParseCommandValue(command, prefix, out string raw))
            {
                return false;
            }

            if (raw.Equals("1", StringComparison.OrdinalIgnoreCase) ||
                raw.Equals("ON", StringComparison.OrdinalIgnoreCase) ||
                raw.Equals("TRUE", StringComparison.OrdinalIgnoreCase))
            {
                value = true;
                return true;
            }

            if (raw.Equals("0", StringComparison.OrdinalIgnoreCase) ||
                raw.Equals("OFF", StringComparison.OrdinalIgnoreCase) ||
                raw.Equals("FALSE", StringComparison.OrdinalIgnoreCase))
            {
                value = false;
                return true;
            }

            return false;
        }

        private static bool TryParseCommandValue(string command, string prefix, out string value)
        {
            value = string.Empty;

            if (!command.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            string remainder = command[prefix.Length..].Trim();
            if (remainder.Length == 0)
            {
                return false;
            }

            if (remainder.StartsWith("=", StringComparison.Ordinal))
            {
                remainder = remainder[1..].Trim();
            }

            if (remainder.Length == 0 || remainder.StartsWith("?", StringComparison.Ordinal))
            {
                return false;
            }

            value = remainder;
            return true;
        }

        private static bool TryParsePresetWrite(string command, out byte preset, out ushort voltage, out ushort current, out ushort ovp, out ushort ocp)
        {
            preset = 0;
            voltage = 0;
            current = 0;
            ovp = 0;
            ocp = 0;

            if (!TryParseCommandValue(command, "PRES:WRITE", out string raw))
            {
                return false;
            }

            string[] parts = raw.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 5)
            {
                return false;
            }

            return byte.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out preset) &&
                ushort.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out voltage) &&
                ushort.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out current) &&
                ushort.TryParse(parts[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out ovp) &&
                ushort.TryParse(parts[4], NumberStyles.Integer, CultureInfo.InvariantCulture, out ocp);
        }

        private static string NormalizeScpiCommand(string command)
        {
            if (string.IsNullOrWhiteSpace(command) || command[0] == '*')
            {
                return command;
            }

            int separatorIndex = command.IndexOfAny([' ', '=', '?']);
            string header = separatorIndex >= 0 ? command[..separatorIndex] : command;
            string suffix = separatorIndex >= 0 ? command[separatorIndex..] : string.Empty;

            string[] normalizedSegments = header
                .Split(':', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(NormalizeScpiKeyword)
                .ToArray();

            if (normalizedSegments.Length == 0)
            {
                return command;
            }

            return string.Join(':', normalizedSegments) + suffix;
        }

        private static string NormalizeScpiKeyword(string keyword)
        {
            return keyword.ToUpperInvariant() switch
            {
                "SYSTEM" => "SYST",
                "SOURCE" => "SOUR",
                "MEASURE" => "MEAS",
                "OUTPUT" => "OUTP",
                "PRESET" => "PRES",
                "PROTECTION" => "PROT",
                "VOLTAGE" => "VOLT",
                "CURRENT" => "CURR",
                "POWER" => "POW",
                "TEMPERATURE" => "TEMP",
                "DEVICE" => "DEV",
                "ERROR" => "ERR",
                "VOLUME" => "VOL",
                "BACKLIGHT" => "BACK",
                _ => keyword
            };
        }
    }
}
