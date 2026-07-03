using LibDP100;
using System.Globalization;
using System.Net.Sockets;
using System.Text;

namespace LibDP100Soc
{
    /// <summary>
    /// Socket-backed DP100 facade. This initially mirrors the libdp100 public API
    /// so the application can swap transports without changing its higher-level behavior.
    /// </summary>
    public class PowerSupply
    {
        public bool Connected { get; private set; } = false;

        public PowerSupplyInfo Device { get; private set; } = new PowerSupplyInfo();

        public PowerSupplyActiveState ActiveState { get; private set; } = new PowerSupplyActiveState();

        public PowerSupplyOutput Output { get; private set; } = new PowerSupplyOutput();

        public PowerSupplySetpoint[] Presets { get; private set; } = CreatePresetArray();

        public PowerSupplySystemParams SystemParams { get; private set; } = new PowerSupplySystemParams();

        public ActiveStateDelegate? ActiveStateEvent { get; set; } = null;

        public DisconnectedDelegate? DisconnectedEvent { get; set; } = null;

        public delegate void ActiveStateDelegate(PowerSupplyActiveState activeState);

        public delegate void DisconnectedDelegate();

        public bool DebugMode { get; set; } = false;

        public string EndpointAddress { get; private set; } = "127.0.0.1";

        public int EndpointPort { get; private set; } = 5025;

        private const byte NumPresets = 10;

        private readonly object clientLock = new();
        private readonly bool[] presetsValid = new bool[NumPresets];
        private bool outputValid;
        private bool systemParamsValid;
        private Thread workerThread;
        private bool workerThreadRun;
        private TimeSpan workerThreadSleepTime;
        private CancellationTokenSource workerThreadSleepCts = new();
        private TcpClient? client;
        private StreamReader? reader;
        private StreamWriter? writer;

        public PowerSupply()
        {
            workerThread = new Thread(WorkerThread);
        }

        public PowerSupply(string endpointAddress, int endpointPort)
            : this()
        {
            SetEndpoint(endpointAddress, endpointPort);
        }

        public void SetEndpoint(string endpointAddress, int endpointPort)
        {
            EndpointAddress = endpointAddress;
            EndpointPort = endpointPort;
        }

        public PowerSupplyResult Connect()
        {
            return ConnectCore(expectedSerialNumber: null);
        }

        public PowerSupplyResult Connect(string serialNumber)
        {
            return ConnectCore(expectedSerialNumber: serialNumber);
        }

        public void Disconnect()
        {
            Connected = false;
            outputValid = false;
            systemParamsValid = false;
            Array.Fill(presetsValid, false);
            StopWorkerThread();

            lock (clientLock)
            {
                reader?.Dispose();
                writer?.Dispose();
                client?.Dispose();
                reader = null;
                writer = null;
                client = null;
            }
        }

        public void StartWorkerThread(TimeSpan sleepTime)
        {
            if (workerThreadRun)
            {
                return;
            }

            if (workerThread.ThreadState == ThreadState.Stopped)
            {
                workerThread = new Thread(WorkerThread);
            }

            workerThreadSleepTime = sleepTime;
            workerThreadRun = true;
            workerThread.Start();
        }

        public void StopWorkerThread()
        {
            if (!workerThreadRun && !workerThread.IsAlive)
            {
                return;
            }

            workerThreadRun = false;
            workerThreadSleepCts.Cancel();

            if (workerThread.IsAlive && Thread.CurrentThread != workerThread)
            {
                workerThread.Join();
            }

            workerThreadSleepCts.Dispose();
            workerThreadSleepCts = new CancellationTokenSource();
        }

        public PowerSupplyResult SetOutputOn() => SendWriteCommand("OUTP ON", () => Output.On = true);

        public PowerSupplyResult SetOutputOff() => SendWriteCommand("OUTP OFF", () => Output.On = false);

        public PowerSupplyResult ToggleOutput() => Output.On ? SetOutputOff() : SetOutputOn();

        public PowerSupplyResult SetOutputVoltage(ushort millivolts)
        {
            if (GetOutput() != PowerSupplyResult.OK)
            {
                return PowerSupplyResult.Error;
            }

            return SetOutput(Output.On, millivolts, Output.Setpoint.Current);
        }

        public PowerSupplyResult SetOutputCurrent(ushort milliamps)
        {
            if (GetOutput() != PowerSupplyResult.OK)
            {
                return PowerSupplyResult.Error;
            }

            return SetOutput(Output.On, Output.Setpoint.Voltage, milliamps);
        }

        public PowerSupplyResult SetOutput(PowerSupplySetpoint setpoint)
        {
            return SetOutput(Output.On, setpoint.Voltage, setpoint.Current);
        }

        public PowerSupplyResult SetOutput(bool outputOn, ushort millivolts, ushort milliamps)
        {
            if (!Connected)
            {
                return PowerSupplyResult.DeviceNotConnected;
            }

            if (TrySendCommand(FormattableString.Invariant($"SOUR:VOLT {millivolts}"), out _) != PowerSupplyResult.OK ||
                TrySendCommand(FormattableString.Invariant($"SOUR:CURR {milliamps}"), out _) != PowerSupplyResult.OK)
            {
                return PowerSupplyResult.Error;
            }

            PowerSupplyResult result = outputOn ? SetOutputOn() : SetOutputOff();
            if (result == PowerSupplyResult.OK)
            {
                Output.Setpoint.Voltage = millivolts;
                Output.Setpoint.Current = milliamps;
                outputValid = true;
            }

            return result;
        }

        public PowerSupplyResult SetBacklight(byte brightness) => SendSystemWrite(FormattableString.Invariant($"SYST:BACK {brightness}"), () => SystemParams.Backlight = brightness);

        public PowerSupplyResult SetVolume(byte volume) => SendSystemWrite(FormattableString.Invariant($"SYST:VOL {volume}"), () => SystemParams.Volume = volume);

        public PowerSupplyResult SetAutoOn(bool enable) => SendSystemWrite(FormattableString.Invariant($"SYST:AUTO {(enable ? 1 : 0)}"), () => SystemParams.AutoOn = enable);

        public PowerSupplyResult SetRPP(bool enable) => SendSystemWrite(FormattableString.Invariant($"SYST:RPP {(enable ? 1 : 0)}"), () => SystemParams.RPP = enable);

        public PowerSupplyResult SetOPP(ushort deciWatts) => SendSystemWrite(FormattableString.Invariant($"SYST:PROT:POW {deciWatts}"), () => SystemParams.OPP = deciWatts);

        public PowerSupplyResult SetOTP(ushort celsius) => SendSystemWrite(FormattableString.Invariant($"SYST:PROT:TEMP {celsius}"), () => SystemParams.OTP = celsius);

        public PowerSupplyResult SetSystemParams(PowerSupplySystemParams systemParams)
        {
            PowerSupplyResult result = SetOTP(systemParams.OTP);
            if (result != PowerSupplyResult.OK) return result;
            result = SetOPP(systemParams.OPP);
            if (result != PowerSupplyResult.OK) return result;
            result = SetBacklight(systemParams.Backlight);
            if (result != PowerSupplyResult.OK) return result;
            result = SetVolume(systemParams.Volume);
            if (result != PowerSupplyResult.OK) return result;
            result = SetRPP(systemParams.RPP);
            if (result != PowerSupplyResult.OK) return result;
            result = SetAutoOn(systemParams.AutoOn);
            if (result != PowerSupplyResult.OK) return result;
            SystemParams = new PowerSupplySystemParams(systemParams);
            systemParamsValid = true;
            return PowerSupplyResult.OK;
        }

        public PowerSupplyResult SetSystemParams(ushort otp, ushort opp, byte backlight, byte volume, bool rpp, bool autoOn)
        {
            return SetSystemParams(new PowerSupplySystemParams
            {
                OTP = otp,
                OPP = opp,
                Backlight = backlight,
                Volume = volume,
                RPP = rpp,
                AutoOn = autoOn
            });
        }

        public PowerSupplyResult SetPresetOVP(byte preset, ushort ovp)
        {
            if (GetPreset(preset) != PowerSupplyResult.OK)
            {
                return PowerSupplyResult.Error;
            }

            return SetPreset(preset, Presets[preset].Voltage, Presets[preset].Current, ovp, Presets[preset].OCP);
        }

        public PowerSupplyResult SetPresetOCP(byte preset, ushort ocp)
        {
            if (GetPreset(preset) != PowerSupplyResult.OK)
            {
                return PowerSupplyResult.Error;
            }

            return SetPreset(preset, Presets[preset].Voltage, Presets[preset].Current, Presets[preset].OVP, ocp);
        }

        public PowerSupplyResult SetPreset(byte preset, PowerSupplySetpoint setpoint)
        {
            return SetPreset(preset, setpoint.Voltage, setpoint.Current, setpoint.OVP, setpoint.OCP);
        }

        public PowerSupplyResult SetPreset(byte preset, ushort millivolts, ushort milliamps, ushort ovp, ushort ocp)
        {
            if (!Connected)
            {
                return PowerSupplyResult.DeviceNotConnected;
            }

            if (preset >= NumPresets)
            {
                return PowerSupplyResult.OutOfRange;
            }

            PowerSupplyResult result = TrySendCommand(
                FormattableString.Invariant($"PRES:WRITE {preset},{millivolts},{milliamps},{ovp},{ocp}"),
                out _);

            if (result == PowerSupplyResult.OK)
            {
                Presets[preset].Voltage = millivolts;
                Presets[preset].Current = milliamps;
                Presets[preset].OVP = ovp;
                Presets[preset].OCP = ocp;
                Presets[preset].SetIndex(preset);
                presetsValid[preset] = true;
            }

            return result;
        }

        public PowerSupplyResult UsePreset(byte preset, bool fromNonVolatile)
        {
            if (!Connected)
            {
                return PowerSupplyResult.DeviceNotConnected;
            }

            string command = fromNonVolatile
                ? FormattableString.Invariant($"PRES:USE {preset}")
                : FormattableString.Invariant($"PRES:RECALL {preset}");

            PowerSupplyResult result = TrySendCommand(command, out _);
            if (result == PowerSupplyResult.OK)
            {
                outputValid = false;
                result = GetOutput();
            }

            return result;
        }

        public PowerSupplyResult GetDeviceInfo()
        {
            if (!Connected)
            {
                return PowerSupplyResult.DeviceNotConnected;
            }

            if (TrySendCommand("SYST:DEV?", out string response) == PowerSupplyResult.OK)
            {
                string[] parts = response.Split(',', StringSplitOptions.TrimEntries);
                if (parts.Length == 7)
                {
                    Device.Type = parts[0];
                    Device.SerialNumber = parts[1];
                    Device.MfgDate = parts[2];
                    Device.HardwareVersion = parts[3];
                    Device.SoftwareVersion = parts[4];
                    Device.BootloaderVersion = parts[5];
                    Device.SoftwareState = parts[6];
                    return PowerSupplyResult.OK;
                }
            }

            if (TrySendCommand("*IDN?", out string idnResponse) != PowerSupplyResult.OK)
            {
                return PowerSupplyResult.Error;
            }

            string[] idnParts = idnResponse.Split(',', StringSplitOptions.TrimEntries);
            if (idnParts.Length < 4)
            {
                return PowerSupplyResult.Error;
            }

            Device.Type = idnParts[1];
            Device.SerialNumber = idnParts[2];
            Device.SoftwareVersion = idnParts[3];
            return PowerSupplyResult.OK;
        }

        public PowerSupplyResult GetSystemParams()
        {
            if (!Connected)
            {
                return PowerSupplyResult.DeviceNotConnected;
            }

            if (TrySendCommand("SYST:PROT:POW?", out string oppResponse) != PowerSupplyResult.OK ||
                TrySendCommand("SYST:PROT:TEMP?", out string otpResponse) != PowerSupplyResult.OK ||
                TrySendCommand("SYST:RPP?", out string rppResponse) != PowerSupplyResult.OK ||
                TrySendCommand("SYST:AUTO?", out string autoOnResponse) != PowerSupplyResult.OK ||
                TrySendCommand("SYST:VOL?", out string volumeResponse) != PowerSupplyResult.OK ||
                TrySendCommand("SYST:BACK?", out string backlightResponse) != PowerSupplyResult.OK)
            {
                return PowerSupplyResult.Error;
            }

            if (!ushort.TryParse(oppResponse, NumberStyles.Integer, CultureInfo.InvariantCulture, out ushort opp) ||
                !ushort.TryParse(otpResponse, NumberStyles.Integer, CultureInfo.InvariantCulture, out ushort otp) ||
                !TryParseScpiBool(rppResponse, out bool rpp) ||
                !TryParseScpiBool(autoOnResponse, out bool autoOn) ||
                !byte.TryParse(volumeResponse, NumberStyles.Integer, CultureInfo.InvariantCulture, out byte volume) ||
                !byte.TryParse(backlightResponse, NumberStyles.Integer, CultureInfo.InvariantCulture, out byte backlight))
            {
                return PowerSupplyResult.Error;
            }

            SystemParams.OPP = opp;
            SystemParams.OTP = otp;
            SystemParams.RPP = rpp;
            SystemParams.AutoOn = autoOn;
            SystemParams.Volume = volume;
            SystemParams.Backlight = backlight;
            systemParamsValid = true;
            return PowerSupplyResult.OK;
        }

        public PowerSupplyResult GetActiveStatus()
        {
            if (!Connected)
            {
                return PowerSupplyResult.DeviceNotConnected;
            }

            if (TrySendCommand("MEAS:ALL?", out string response) != PowerSupplyResult.OK)
            {
                return PowerSupplyResult.Error;
            }

            string[] parts = response.Split(',', StringSplitOptions.TrimEntries);
            if (parts.Length == 10 &&
                ushort.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out ushort voltageInput) &&
                ushort.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out ushort voltage) &&
                ushort.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out ushort current) &&
                ushort.TryParse(parts[4], NumberStyles.Integer, CultureInfo.InvariantCulture, out ushort voltageOutputMax) &&
                ushort.TryParse(parts[5], NumberStyles.Integer, CultureInfo.InvariantCulture, out ushort temp1) &&
                ushort.TryParse(parts[6], NumberStyles.Integer, CultureInfo.InvariantCulture, out ushort temp2) &&
                ushort.TryParse(parts[7], NumberStyles.Integer, CultureInfo.InvariantCulture, out ushort voltageUsb5V) &&
                byte.TryParse(parts[8], NumberStyles.Integer, CultureInfo.InvariantCulture, out byte outputMode) &&
                byte.TryParse(parts[9], NumberStyles.Integer, CultureInfo.InvariantCulture, out byte faultStatus))
            {
                ActiveState.Timestamp = DateTime.Now;
                ActiveState.VoltageInput = voltageInput;
                ActiveState.Voltage = voltage;
                ActiveState.Current = current;
                ActiveState.VoltageOutputMax = voltageOutputMax;
                ActiveState.Temperature1 = temp1;
                ActiveState.Temperature2 = temp2;
                ActiveState.VoltageUsb5V = voltageUsb5V;
                ActiveState.OutputMode = (PowerSupplyOutputMode)outputMode;
                ActiveState.FaultStatus = (PowerSupplyFaultStatus)faultStatus;
                return PowerSupplyResult.OK;
            }

            return PowerSupplyResult.Error;
        }

        public PowerSupplyResult GetOutput()
        {
            if (!Connected)
            {
                return PowerSupplyResult.DeviceNotConnected;
            }

            if (TrySendCommand("OUTP?", out string stateResponse) != PowerSupplyResult.OK ||
                TrySendCommand("PRES?", out string presetResponse) != PowerSupplyResult.OK ||
                TrySendCommand("SOUR:VOLT?", out string voltageResponse) != PowerSupplyResult.OK ||
                TrySendCommand("SOUR:CURR?", out string currentResponse) != PowerSupplyResult.OK ||
                TrySendCommand("SOUR:VOLT:PROT?", out string ovpResponse) != PowerSupplyResult.OK ||
                TrySendCommand("SOUR:CURR:PROT?", out string ocpResponse) != PowerSupplyResult.OK)
            {
                return PowerSupplyResult.Error;
            }

            if (!TryParseScpiBool(stateResponse, out bool on) ||
                !byte.TryParse(presetResponse, NumberStyles.Integer, CultureInfo.InvariantCulture, out byte preset) ||
                !ushort.TryParse(voltageResponse, NumberStyles.Integer, CultureInfo.InvariantCulture, out ushort voltage) ||
                !ushort.TryParse(currentResponse, NumberStyles.Integer, CultureInfo.InvariantCulture, out ushort current) ||
                !ushort.TryParse(ovpResponse, NumberStyles.Integer, CultureInfo.InvariantCulture, out ushort ovp) ||
                !ushort.TryParse(ocpResponse, NumberStyles.Integer, CultureInfo.InvariantCulture, out ushort ocp))
            {
                return PowerSupplyResult.Error;
            }

            Output.On = on;
            Output.Preset = preset;
            Output.Setpoint = new PowerSupplySetpoint(preset)
            {
                Voltage = voltage,
                Current = current,
                OVP = ovp,
                OCP = ocp
            };
            outputValid = true;
            return PowerSupplyResult.OK;
        }

        public PowerSupplyResult GetPreset(byte preset)
        {
            if (!Connected)
            {
                return PowerSupplyResult.DeviceNotConnected;
            }

            if (preset >= NumPresets)
            {
                return PowerSupplyResult.OutOfRange;
            }

            if (TrySendCommand(FormattableString.Invariant($"PRES:READ? {preset}"), out string response) != PowerSupplyResult.OK)
            {
                return PowerSupplyResult.Error;
            }

            string[] parts = response.Split(',', StringSplitOptions.TrimEntries);
            if (parts.Length != 4 ||
                !ushort.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out ushort voltage) ||
                !ushort.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out ushort current) ||
                !ushort.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out ushort ovp) ||
                !ushort.TryParse(parts[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out ushort ocp))
            {
                return PowerSupplyResult.Error;
            }

            Presets[preset].Voltage = voltage;
            Presets[preset].Current = current;
            Presets[preset].OVP = ovp;
            Presets[preset].OCP = ocp;
            Presets[preset].SetIndex(preset);
            presetsValid[preset] = true;
            return PowerSupplyResult.OK;
        }

        public PowerSupplyResult Reload()
        {
            if (GetOutput() != PowerSupplyResult.OK)
            {
                return PowerSupplyResult.Error;
            }

            for (byte preset = 0; preset < NumPresets; preset++)
            {
                PowerSupplyResult result = GetPreset(preset);
                if (result != PowerSupplyResult.OK)
                {
                    return result;
                }
            }

            return GetSystemParams();
        }

        public void SignalRunWorker()
        {
            workerThreadSleepCts.Cancel();
        }

        private static PowerSupplySetpoint[] CreatePresetArray()
        {
            PowerSupplySetpoint[] presets = new PowerSupplySetpoint[NumPresets];
            for (byte i = 0; i < NumPresets; i++)
            {
                presets[i] = new PowerSupplySetpoint(i);
            }

            return presets;
        }

        private PowerSupplyResult ConnectCore(string? expectedSerialNumber)
        {
            if (Connected)
            {
                return PowerSupplyResult.OK;
            }

            try
            {
                TcpClient nextClient = new();
                nextClient.Connect(EndpointAddress, EndpointPort);
                NetworkStream stream = nextClient.GetStream();
                StreamReader nextReader = new(stream, Encoding.ASCII, detectEncodingFromByteOrderMarks: false, bufferSize: 1024, leaveOpen: true);
                StreamWriter nextWriter = new(stream, Encoding.ASCII, bufferSize: 1024, leaveOpen: true)
                {
                    NewLine = "\n",
                    AutoFlush = true
                };

                string banner = nextReader.ReadLine() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(banner))
                {
                    nextWriter.Dispose();
                    nextReader.Dispose();
                    nextClient.Dispose();
                    return PowerSupplyResult.Timeout;
                }

                lock (clientLock)
                {
                    client = nextClient;
                    reader = nextReader;
                    writer = nextWriter;
                    Connected = true;
                }

                PowerSupplyResult result = GetDeviceInfo();
                if (result != PowerSupplyResult.OK)
                {
                    Disconnect();
                    return result;
                }

                if (!string.IsNullOrEmpty(expectedSerialNumber) && !string.Equals(Device.SerialNumber, expectedSerialNumber, StringComparison.OrdinalIgnoreCase))
                {
                    Disconnect();
                    return PowerSupplyResult.DeviceNotPresent;
                }

                return Reload();
            }
            catch (SocketException)
            {
                Disconnect();
                return PowerSupplyResult.DeviceNotConnected;
            }
            catch (IOException)
            {
                Disconnect();
                return PowerSupplyResult.Error;
            }
        }

        private void WorkerThread()
        {
            while (workerThreadRun)
            {
                if (GetActiveStatus() == PowerSupplyResult.OK)
                {
                    ActiveStateEvent?.Invoke(ActiveState);
                }
                else if (Connected)
                {
                    Disconnect();
                    DisconnectedEvent?.Invoke();
                    return;
                }

                try
                {
                    Task.Delay(workerThreadSleepTime, workerThreadSleepCts.Token).Wait();
                }
                catch (AggregateException)
                {
                }
                catch (OperationCanceledException)
                {
                }
                finally
                {
                    workerThreadSleepCts.Dispose();
                    workerThreadSleepCts = new CancellationTokenSource();
                }
            }
        }

        private PowerSupplyResult SendWriteCommand(string command, Action onSuccess)
        {
            PowerSupplyResult result = TrySendCommand(command, out _);
            if (result == PowerSupplyResult.OK)
            {
                onSuccess();
                outputValid = false;
            }

            return result;
        }

        private PowerSupplyResult SendSystemWrite(string command, Action onSuccess)
        {
            PowerSupplyResult result = TrySendCommand(command, out _);
            if (result == PowerSupplyResult.OK)
            {
                onSuccess();
                systemParamsValid = true;
            }

            return result;
        }

        private PowerSupplyResult TrySendCommand(string command, out string response)
        {
            response = string.Empty;

            if (!Connected)
            {
                return PowerSupplyResult.DeviceNotConnected;
            }

            lock (clientLock)
            {
                if (writer == null || reader == null)
                {
                    return PowerSupplyResult.DeviceNotConnected;
                }

                try
                {
                    writer.WriteLine(command);
                    response = reader.ReadLine() ?? string.Empty;
                    if (string.IsNullOrWhiteSpace(response))
                    {
                        return PowerSupplyResult.Timeout;
                    }

                    if (response.StartsWith("ERR", StringComparison.OrdinalIgnoreCase))
                    {
                        return PowerSupplyResult.Error;
                    }

                    return PowerSupplyResult.OK;
                }
                catch (IOException)
                {
                    Connected = false;
                    return PowerSupplyResult.Error;
                }
            }
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
                return true;
            }

            return false;
        }
    }
}
