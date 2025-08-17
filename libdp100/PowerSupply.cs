using HidSharp;
using System.Diagnostics;
using System.Threading;

namespace LibDP100
{
    /// <summary>
    /// The main power supply object.
    /// </summary>
    public class PowerSupply
    {
        /// <summary>
        /// Indicates if the device is currently connected.
        /// </summary>
        public bool Connected { get; private set; } = false;

        /// <summary>
        /// Contains device information.
        /// </summary>
        public PowerSupplyInfo Device { get; private set; } = new PowerSupplyInfo();

        /// <summary>
        /// Contains active status information.
        /// </summary>
        public PowerSupplyActiveState ActiveState { get; private set; } = new PowerSupplyActiveState();

        /// <summary>
        /// Contains the output state.
        /// </summary>
        public PowerSupplyOutput Output { get; private set; } = new PowerSupplyOutput();

        /// <summary>
        /// Contains the output presets.
        /// </summary>
        public PowerSupplySetpoint[] Presets { get; private set; } = new PowerSupplySetpoint[NumPresets];

        /// <summary>
        /// Contains the system parameters.
        /// </summary>
        public PowerSupplySystemParams SystemParams { get; private set; } = new PowerSupplySystemParams();

        /// <summary>
        /// Set by application to receive regular "ActiveState" updates.
        /// </summary>
        public ActiveStateDelegate? ActiveStateEvent { get; set; } = null;

        /// <summary>
        /// Set by application to receive disconnected events not initiated via Disconnect().
        /// </summary>
        public DisconnectedDelegate? DisconnectedEvent { get; set; } = null;

        /// <summary>
        /// Delegate for providing regular updates to the application.
        /// </summary>
        /// <param name="activeState"></param>
        public delegate void ActiveStateDelegate(PowerSupplyActiveState activeState);

        /// <summary>
        /// Delegate for device disconnected notification.
        /// </summary>
        public delegate void DisconnectedDelegate();

        /// <summary>
        /// Switch between normal and debug mode.
        /// When set to debug mode, the Raw HID report data will be printed to the console.
        /// </summary>
        public bool DebugMode { get; set; } = false;

        #region Private Members

        /// <summary>
        /// The USB Vendor ID to search for during enumeration.
        /// </summary>
        private const ushort VendorID = 0x2E3C;

        /// <summary>
        /// The USB Product ID to search for during enumeration.
        /// </summary>
        private const ushort ProductID = 0xAF01;

        /// <summary>
        /// The number of presets available on the device.
        /// </summary>
        private const byte NumPresets = 10;

        /// <summary>
        /// The index in the HID report where data begins.
        /// </summary>
        private const int ResponseDataIndex = 5;

        /// <summary>
        /// The index in the HID report where the length is specified.
        /// </summary>
        private const int ResponseLenIndex = 4;

        /// <summary>
        /// Indicates that the "Output" object has been populated with valid data.
        /// </summary>
        private bool outputValid = false;

        /// <summary>
        /// Indicates that the "Presets" object has been populated with valid data.
        /// </summary>
        private bool[] presetsValid = new bool[NumPresets];

        /// <summary>
        /// Indicates that the "Presets" object has been applied to the volatile preset's state.
        /// </summary>
        private bool[] presetsLoaded = new bool[NumPresets];

        /// <summary>
        /// Indicates that the "SystemParams" object has been populated with valid data.
        /// </summary>
        private bool systemParamsValid = false;

        /// <summary>
        /// The current listing of USB devices. Used to monitor changes in the device listing.
        /// </summary>
        private DeviceList list = DeviceList.Local;

        /// <summary>
        /// Indicates the number of bytes used for the HID input reports.
        /// </summary>
        private int hidInputReportLength;

        /// <summary>
        /// Indicates the HID serial number. This value may not be the same as the serial number
        /// obtained from the DeviceInfo command. This value is used to determine device-disconnect
        /// events.
        /// </summary>
        private string? hidSerialNumber;

        /// <summary>
        /// The HID stream in which all communications is performed through.
        /// </summary>
        private HidStream? hidStream;

        /// <summary>
        /// Mutex to ensure single-access to underlying HID stream.
        /// </summary>
        private Mutex hidStreamMutex = new();

        /// <summary>
        /// The timeout for HID responses.
        /// </summary>
        private TimeSpan ResponseTimeout { get; set; } = TimeSpan.FromMilliseconds(2000);

        /// <summary>
        /// The thread that processes reading actual voltage and current.
        /// </summary>
        private Thread workerThread;

        /// <summary>
        /// Indicates whether the worker thread should continue to run.
        /// </summary>
        private bool workerThreadRun;

        /// <summary>
        /// The time between each read performed by the worker thread.
        /// </summary>
        private TimeSpan workerThreadSleepTime;

        /// <summary>
        /// Token to cancel the delay to allow additional work to be done.
        /// </summary>
        private CancellationToken workerThreadSleepCancellation = new CancellationToken();

        /// <summary>
        /// Cancellation token source for the worker thread.
        /// </summary>
        CancellationTokenSource workerThreadSleepCts = new CancellationTokenSource();

        /// <summary>
        /// Delegate for processing input report buffer data.
        /// </summary>
        /// <param name="inputReportBuffer">The HID input report data.</param>
        /// <returns>The result from processing the data.</returns>
        private delegate PowerSupplyResult HandleResponseDelegate(byte[] inputReportBuffer);

        #endregion // Private Members

        /// <summary>
        /// The constructor.
        /// </summary>
        public PowerSupply()
        {
            list.Changed += ListChangedEvent;
            workerThread = new Thread(new ThreadStart(WorkerThread));
        }

        #region API

        /// <summary>
        /// Connect to the first enumerated DP100.
        /// </summary>
        /// <returns>
        /// On success, returns <see cref="PowerSupplyResult.OK"/>.
        /// On failure, a relevant error result will be returned.
        /// </returns>
        public PowerSupplyResult Connect()
        {
            PowerSupplyResult status = PowerSupplyResult.DeviceNotConnected;
            var hidDeviceList = list.GetHidDevices(VendorID, ProductID).ToArray();
            OpenConfiguration openConfig = new OpenConfiguration();
            openConfig.SetOption(OpenOption.Exclusive, true);

            if (hidDeviceList.Length == 0)
            {
                return status;
            }
            else
            {
                // At least one device is present. If connection is not successful,
                // report an error to the caller.
                status = PowerSupplyResult.Error;
            }

            foreach (HidDevice dev in hidDeviceList)
            {
                if (dev.TryOpen(openConfig, out hidStream))
                {
                    Connected = true;
                    hidStream.ReadTimeout = Timeout.Infinite;
                    hidInputReportLength = dev.GetMaxInputReportLength();
                    hidSerialNumber = dev.GetSerialNumber();
                    status = PowerSupplyResult.OK;
                    break;
                }
            }

            return status;
        }

        /// <summary>
        /// Connect to the DP100 that matches the specified serial number.
        /// </summary>
        /// <param name="serialNumber">The serial number to connect to.</param>
        /// <returns>
        /// On success, returns <see cref="PowerSupplyResult.OK"/>.
        /// On failure, a relevant error result will be returned.
        /// </returns>
        public PowerSupplyResult Connect(string serialNumber)
        {
            PowerSupplyResult status = PowerSupplyResult.DeviceNotConnected;
            var hidDeviceList = list.GetHidDevices(VendorID, ProductID).ToArray();
            OpenConfiguration openConfig = new OpenConfiguration();
            openConfig.SetOption(OpenOption.Exclusive, true);

            if (hidDeviceList.Length == 0)
            {
                return status;
            }
            else
            {
                // At least one device is present. If connection is not successful,
                // report an error to the caller.
                status = PowerSupplyResult.Error;
            }

            foreach (HidDevice dev in hidDeviceList)
            {
                if (dev.TryOpen(out hidStream))
                {
                    Connected = true;
                    hidStream.ReadTimeout = Timeout.Infinite;
                    hidInputReportLength = dev.GetMaxInputReportLength();

                    if (GetDeviceInfo() == PowerSupplyResult.OK &&
                        Device.SerialNumber == serialNumber)
                    {
                        Connected = true;
                        hidSerialNumber = dev.GetSerialNumber();
                        status = PowerSupplyResult.OK;
                        break;
                    }

                    Connected = false;
                }
            }

            return status;
        }

        /// <summary>
        /// Disconnects from the DP100 and disposes of managed resources.
        /// </summary>
        public void Disconnect()
        {
            Connected = false;
            Dispose();
        }

        /// <summary>
        /// Starts the worker thread to cyclically read the active status information.
        /// If the application has set <see cref="ActiveStateEvent"/> the worker will call
        /// this event handler on each successful response.
        /// </summary>
        /// <param name="sleepTime">The time between each cyclic request.</param>
        public void StartWorkerThread(TimeSpan sleepTime)
        {
            if (workerThreadRun)
            {
                return;
            }

            if (workerThread.ThreadState == System.Threading.ThreadState.Stopped)
            {
                workerThread = new Thread(new ThreadStart(WorkerThread));
            }

            workerThreadRun = true;
            workerThread.Start();
            workerThreadSleepTime = sleepTime;
        }

        /// <summary>
        /// Stops the worker thread.
        /// </summary>
        public void StopWorkerThread()
        {
            workerThreadRun = false;

            while (workerThread.ThreadState == System.Threading.ThreadState.Running)
            {
                Thread.Yield();
            }
        }

        #region Write Operations
        /// <summary>
        /// Switch the power supply output ON.
        /// </summary>
        /// <returns>
        /// On success, returns <see cref="PowerSupplyResult.OK"/>.
        /// On failure, a relevant error result will be returned.
        /// </returns>
        public PowerSupplyResult SetOutputOn()
        {
            PowerSupplyResult result = PowerSupplyResult.OK;

            if (!outputValid)
            {
                result = GetOutput();
            }

#if false
            // This seems to be the most reliable way to reset the fault status,
            // but it would be preferred to allow the user to reset the fault status
            // by explicitly turning the output off. Also not the delay is rather
            // important.
            if (ActiveState.FaultStatus != PowerSupplyFaultStatus.OK)
            {
                SetOutputOff();
                Thread.Sleep(150);
            }
#endif

            if (result == PowerSupplyResult.OK)
            {
                result = SetOutput(outputOn: true, Output.Setpoint.Voltage, Output.Setpoint.Current);
            }

            return result;
        }

        /// <summary>
        /// Switch the power supply output OFF.
        /// </summary>
        /// <returns>
        /// On success, returns <see cref="PowerSupplyResult.OK"/>.
        /// On failure, a relevant error result will be returned.
        /// </returns>
        public PowerSupplyResult SetOutputOff()
        {
            PowerSupplyResult result = PowerSupplyResult.OK;

            if (!outputValid)
            {
                result = GetOutput();
            }

            if (result == PowerSupplyResult.OK)
            {
                result = SetOutput(outputOn: false, Output.Setpoint.Voltage, Output.Setpoint.Current);
            }

            return result;
        }

        /// <summary>
        /// Toggle the power supply output state.
        /// </summary>
        /// <returns>
        /// On success, returns <see cref="PowerSupplyResult.OK"/>.
        /// On failure, a relevant error result will be returned.
        /// </returns>
        public PowerSupplyResult ToggleOutput()
        {
            PowerSupplyResult result = PowerSupplyResult.OK;

            if (!outputValid)
            {
                result = GetOutput();
            }

            if (result == PowerSupplyResult.OK)
            {

                result = SetOutput(!Output.On, Output.Setpoint.Voltage, Output.Setpoint.Current);
            }

            return result;
        }

        /// <summary>
        /// Set the power supply output voltage.
        /// </summary>
        /// <param name="millivolts">The voltage in millivolts to set.</param>
        /// <returns>
        /// On success, returns <see cref="PowerSupplyResult.OK"/>.
        /// On failure, a relevant error result will be returned.
        /// </returns>
        public PowerSupplyResult SetOutputVoltage(ushort millivolts)
        {
            PowerSupplyResult result = PowerSupplyResult.OK;

            if (!outputValid)
            {
                result = GetOutput();
            }

            if (result == PowerSupplyResult.OK)
            {

                result = SetOutput(Output.On, millivolts, Output.Setpoint.Current);
            }

            return result;
        }

        /// <summary>
        /// Set the power supply output current.
        /// </summary>
        /// <param name="milliamps">The current in milliamps to set.</param>
        /// <returns>
        /// On success, returns <see cref="PowerSupplyResult.OK"/>.
        /// On failure, a relevant error result will be returned.
        /// </returns>
        public PowerSupplyResult SetOutputCurrent(ushort milliamps)
        {
            PowerSupplyResult result = PowerSupplyResult.OK;

            if (!outputValid)
            {
                result = GetOutput();
            }

            if (result == PowerSupplyResult.OK)
            {

                result = SetOutput(Output.On, Output.Setpoint.Voltage, milliamps);
            }

            return result;
        }

        /// <summary>
        /// Set the output setpoint consisting of an output-voltage, output-current.
        /// The current output state will be maintained.
        /// </summary>
        /// <param name="setpoint">
        /// The setpoint to apply. Only the voltage/current values will be used.
        /// </param>
        /// <returns>
        /// On success, returns <see cref="PowerSupplyResult.OK"/>.
        /// On failure, a relevant error result will be returned.
        /// </returns>
        public PowerSupplyResult SetOutput(PowerSupplySetpoint setpoint)
        {
            if (!Connected)
            {
                return PowerSupplyResult.DeviceNotConnected;
            }

            PowerSupplyResult result = PowerSupplyResult.OK;

            if (!outputValid)
            {
                result = GetOutput();
            }

            if (result == PowerSupplyResult.OK)
            {
                result = SetOutput(Output.On, setpoint.Voltage, setpoint.Current);
            }

            return result;
        }

        /// <summary>
        /// Write Setpoint.
        /// Use to switch the output ON/OFF.
        /// Use to change setpoint voltage/current.
        /// Does not affect OCP/OCP or configured preset state.
        /// </summary>
        /// <param name="outputOn">The output state.</param>
        /// <param name="millivolts">The output voltage in millivolts.</param>
        /// <param name="milliamps">The output current in milliamps.</param>
        /// <returns>
        /// On success, returns <see cref="PowerSupplyResult.OK"/>.
        /// On failure, a relevant error result will be returned.
        /// </returns>
        public PowerSupplyResult SetOutput(bool outputOn, ushort millivolts, ushort milliamps)
        {
            if (!Connected)
            {
                return PowerSupplyResult.DeviceNotConnected;
            }

            const byte preset = 0; // Not applicable
            const ushort ovp = 0; // Not applicable
            const ushort ocp = 0; // Not applicable
            byte[] outputReport = SetBasicSetCommand(BasicSetSubOpCode.SetCurrentBasic, preset, outputOn, millivolts, milliamps, ovp, ocp);
            PrintOutputReportHex(outputReport);
            PowerSupplyResult result = ProcessTransaction(outputReport, new HandleResponseDelegate(HandleAckResponse));

            if (result == PowerSupplyResult.OK)
            {
                Output.On = outputOn;
                Output.Setpoint.Voltage = millivolts;
                Output.Setpoint.Current = milliamps;

                if (Presets[Output.Preset] == null)
                {
                    Presets[Output.Preset] = new PowerSupplySetpoint(Output.Preset);
                    result = GetPreset(Output.Preset);
                }
            }

            if (result == PowerSupplyResult.OK)
            {
                // Output state affects the volatile state of a preset (group).
                // This does not include OVP/OCP.
                Presets[Output.Preset].Voltage = Output.Setpoint.Voltage;
                Presets[Output.Preset].Current = Output.Setpoint.Current;
            }

            return result;
        }

        /// <summary>
        /// Set the power supply display backlight brightness.
        /// </summary>
        /// <param name="brightness">The brightness to set (range: 0-4).</param>
        /// <returns>
        /// On success, returns <see cref="PowerSupplyResult.OK"/>.
        /// On failure, a relevant error result will be returned.
        /// </returns>
        public PowerSupplyResult SetBacklight(byte brightness)
        {
            PowerSupplyResult result = PowerSupplyResult.OK;

            if (!systemParamsValid)
            {
                result = GetSystemParams();
            }

            if (result == PowerSupplyResult.OK)
            {
                result = SetSystemParams(SystemParams.OTP, SystemParams.OPP, brightness, SystemParams.Volume, SystemParams.RPP, SystemParams.AutoOn);
            }

            return result;
        }

        /// <summary>
        /// Set the power supply piezoelectric buzzer volume.
        /// </summary>
        /// <param name="volume">The volume to set (range: 0-4).</param>
        /// <returns>
        /// On success, returns <see cref="PowerSupplyResult.OK"/>.
        /// On failure, a relevant error result will be returned.
        /// </returns>
        public PowerSupplyResult SetVolume(byte volume)
        {
            PowerSupplyResult result = PowerSupplyResult.OK;

            if (!systemParamsValid)
            {
                result = GetSystemParams();
            }

            if (result == PowerSupplyResult.OK)
            {
                result = SetSystemParams(SystemParams.OTP, SystemParams.OPP, SystemParams.Backlight, volume, SystemParams.RPP, SystemParams.AutoOn);
            }

            return result;
        }

        /// <summary>
        /// Set the power supply automatic output-on state.
        /// </summary>
        /// <param name="enable">The automatic output-on state.</param>
        /// <returns>
        /// On success, returns <see cref="PowerSupplyResult.OK"/>.
        /// On failure, a relevant error result will be returned.
        /// </returns>
        public PowerSupplyResult SetAutoOn(bool enable)
        {
            PowerSupplyResult result = PowerSupplyResult.OK;

            if (!systemParamsValid)
            {
                result = GetSystemParams();
            }

            if (result == PowerSupplyResult.OK)
            {
                result = SetSystemParams(SystemParams.OTP, SystemParams.OPP, SystemParams.Backlight, SystemParams.Volume, SystemParams.RPP, enable);
            }

            return result;
        }

        /// <summary>
        /// Set the power supply reverse polarity protection state.
        /// </summary>
        /// <param name="enable">The reverse polarity protection state.</param>
        /// <returns>
        /// On success, returns <see cref="PowerSupplyResult.OK"/>.
        /// On failure, a relevant error result will be returned.
        /// </returns>
        public PowerSupplyResult SetRPP(bool enable)
        {
            PowerSupplyResult result = PowerSupplyResult.OK;

            if (!systemParamsValid)
            {
                result = GetSystemParams();
            }

            if (result == PowerSupplyResult.OK)
            {
                result = SetSystemParams(SystemParams.OTP, SystemParams.OPP, SystemParams.Backlight, SystemParams.Volume, enable, SystemParams.AutoOn);
            }

            return result;
        }

        /// <summary>
        /// Set the power supply over-power protection level.
        /// </summary>
        /// <param name="deciWatts">The power limit in 0.1 Watts.</param>
        /// <returns>
        /// On success, returns <see cref="PowerSupplyResult.OK"/>.
        /// On failure, a relevant error result will be returned.
        /// </returns>
        public PowerSupplyResult SetOPP(ushort deciWatts)
        {
            PowerSupplyResult result = PowerSupplyResult.OK;

            if (!systemParamsValid)
            {
                result = GetSystemParams();
            }

            if (result == PowerSupplyResult.OK)
            {
                result = SetSystemParams(SystemParams.OTP, deciWatts, SystemParams.Backlight, SystemParams.Volume, SystemParams.RPP, SystemParams.AutoOn);
            }

            return result;
        }

        /// <summary>
        /// Set the power supply over-temperature protection level.
        /// </summary>
        /// <param name="celsius">The power limit in degrees Celsius.</param>
        /// <returns>
        /// On success, returns <see cref="PowerSupplyResult.OK"/>.
        /// On failure, a relevant error result will be returned.
        /// </returns>
        public PowerSupplyResult SetOTP(ushort celsius)
        {
            PowerSupplyResult result = PowerSupplyResult.OK;

            if (!systemParamsValid)
            {
                result = GetSystemParams();
            }

            if (result == PowerSupplyResult.OK)
            {
                result = SetSystemParams(celsius, SystemParams.OPP, SystemParams.Backlight, SystemParams.Volume, SystemParams.RPP, SystemParams.AutoOn);
            }

            return result;
        }

        /// <summary>
        /// Saves the specified system parameters to non-volatile memory.
        /// </summary>
        /// <param name="systemParams">The system parameters to apply.</param>
        /// <returns>
        /// On success, returns <see cref="PowerSupplyResult.OK"/>.
        /// On failure, a relevant error result will be returned.
        /// </returns>
        public PowerSupplyResult SetSystemParams(PowerSupplySystemParams systemParams)
        {
            return SetSystemParams(systemParams.OTP, systemParams.OPP, systemParams.Backlight, systemParams.Volume, systemParams.RPP, systemParams.AutoOn);
        }

        /// <summary>
        /// Saves the specified system parameters to non-volatile memory.
        /// </summary>
        /// <param name="otp">The over-temperature protection limit in degrees Celsius (range: 50-80).</param>
        /// <param name="opp">The over-power protection limit in 0.1 Watts (max: 1050).</param>
        /// <param name="backlight">The display backlight brightness (range: 0-4).</param>
        /// <param name="volume">The piezoelectric buzzer volume (range: 0-4).</param>
        /// <param name="rpp">The reverse polarity protection state.</param>
        /// <param name="autoOn">The output auto-on state.</param>
        /// <returns>
        /// On success, returns <see cref="PowerSupplyResult.OK"/>.
        /// On failure, a relevant error result will be returned.
        /// </returns>
        public PowerSupplyResult SetSystemParams(ushort otp, ushort opp, byte backlight, byte volume, bool rpp, bool autoOn)
        {
            if (!Connected)
            {
                return PowerSupplyResult.DeviceNotConnected;
            }

            byte[] outputReport = PowerSupply.SetSystemParamsCommand(otp, opp, backlight, volume, rpp, autoOn);
            PrintOutputReportHex(outputReport);
            PowerSupplyResult result = ProcessTransaction(outputReport,
                new HandleResponseDelegate(HandleAckResponse));

            if (result == PowerSupplyResult.OK)
            {
                SystemParams.OTP = otp;
                SystemParams.OPP = opp;
                SystemParams.RPP = rpp;
                SystemParams.AutoOn = autoOn;
                SystemParams.Backlight = backlight;
                SystemParams.Volume = volume;
            }

            return result;
        }

        /// <summary>
        /// Saves the specified over-voltage protection limit to the specified preset.
        /// </summary>
        /// <param name="preset">The preset to update (range: 0-9).</param>
        /// <param name="ovp">The over-voltage protection limit in millivolts.</param>
        /// <returns>
        /// On success, returns <see cref="PowerSupplyResult.OK"/>.
        /// On failure, a relevant error result will be returned.
        /// </returns>
        public PowerSupplyResult SetPresetOVP(byte preset, ushort ovp)
        {
            if (!Connected)
            {
                return PowerSupplyResult.DeviceNotConnected;
            }

            PowerSupplyResult result = PowerSupplyResult.OK;

            if (!presetsValid[preset])
            {
                result = GetPreset(preset);
            }

            if (result == PowerSupplyResult.OK)
            {
                result = SetPreset(preset, Presets[preset].Voltage, Presets[preset].Current, ovp, Presets[preset].OCP);
            }

            return result;
        }

        /// <summary>
        /// Saves the specified over-current protection limit to the specified preset.
        /// </summary>
        /// <param name="preset">The preset to update (range: 0-9).</param>
        /// <param name="ocp">The over-current protection limit in milliamps.</param>
        /// <returns>
        /// On success, returns <see cref="PowerSupplyResult.OK"/>.
        /// On failure, a relevant error result will be returned.
        /// </returns>
        public PowerSupplyResult SetPresetOCP(byte preset, ushort ocp)
        {
            if (!Connected)
            {
                return PowerSupplyResult.DeviceNotConnected;
            }

            PowerSupplyResult result = PowerSupplyResult.OK;

            if (!presetsValid[preset])
            {
                result = GetPreset(preset);
            }

            if (result == PowerSupplyResult.OK)
            {
                result = SetPreset(preset, Presets[preset].Voltage, Presets[preset].Current, Presets[preset].OVP, ocp);
            }

            return result;
        }

        /// <summary>
        /// Saves the specified parameters to a non-volatile preset.
        /// </summary>
        /// <param name="preset">The preset to update.</param>
        /// <param name="setpoint">The setpoint values to apply.</param>
        /// <returns>
        /// On success, returns <see cref="PowerSupplyResult.OK"/>.
        /// On failure, a relevant error result will be returned.
        /// </returns>
        public PowerSupplyResult SetPreset(byte preset, PowerSupplySetpoint setpoint)
        {
            return SetPreset(preset, setpoint.Voltage, setpoint.Current, setpoint.OVP, setpoint.OCP);
        }

        /// <summary>
        /// Saves the specified parameters to a non-volatile preset.
        /// </summary>
        /// <param name="preset">The preset to update.</param>
        /// <param name="millivolts">The voltage in millivolts.</param>
        /// <param name="milliamps">The current in milliamps.</param>
        /// <param name="ovp">The over-voltage protection level in millivolts.</param>
        /// <param name="ocp">The over-current protection level in milliamps.</param>
        /// <returns>
        /// On success, returns <see cref="PowerSupplyResult.OK"/>.
        /// On failure, a relevant error result will be returned.
        /// </returns>
        public PowerSupplyResult SetPreset(byte preset, ushort millivolts, ushort milliamps, ushort ovp, ushort ocp)
        {
            if (!Connected)
            {
                return PowerSupplyResult.DeviceNotConnected;
            }

            PowerSupplyResult result = PowerSupplyResult.OK;

            if (!presetsValid[preset])
            {
                result = GetPreset(preset);
            }

            bool outputOn = false; // Not applicable
            byte[] outputReport = SetBasicSetCommand(BasicSetSubOpCode.SaveGroup, preset, outputOn, millivolts, milliamps, ovp, ocp);
            PrintOutputReportHex(outputReport);
            result = ProcessTransaction(outputReport, new HandleResponseDelegate(HandleAckResponse));

            if (result == PowerSupplyResult.OK)
            {
                Presets[preset].Voltage = millivolts;
                Presets[preset].Current = milliamps;
                Presets[preset].OVP = ovp;
                Presets[preset].OCP = ocp;
            }

            return result;
        }

        /// <summary>
        /// Used to switch to a preconfigured preset.
        /// </summary>
        /// <param name="preset">The preset index (range 0-9).</param>
        /// <returns>
        /// On success, returns <see cref="PowerSupplyResult.OK"/>.
        /// On failure, a relevant error result will be returned.
        /// </returns>
        public PowerSupplyResult UsePreset(byte preset)
        {
            if (!Connected)
            {
                return PowerSupplyResult.DeviceNotConnected;
            }

            PowerSupplyResult result = PowerSupplyResult.OK;

            if (Output.On)
            {
                result = SetOutputOff();
                // Sleep to provide time for outputs to fully turn off and avoid potential faults.
                Thread.Sleep(100);
            }

            if (result == PowerSupplyResult.OK)
            {
                byte[] outputReport = GetBasicSetCommand(BasicSetSubOpCode.UseGroup, preset);
                PrintOutputReportHex(outputReport);
                result = ProcessTransaction(outputReport, new HandleResponseDelegate(HandleAckResponse));
            }

            if (result == PowerSupplyResult.OK)
            {
                Output.On = false;
                Output.Preset = preset;

                // Make sure the local copy of the preset is valid before proceeding to copy.
                if (!presetsValid[preset])
                {
                    result = GetPreset(preset);
                }
            }

            if (result == PowerSupplyResult.OK)
            {
                if (!presetsLoaded[preset])
                {
                    result = SetOutput(Presets[preset]);
                }
            }

            if (result == PowerSupplyResult.OK)
            {
                Output.Setpoint.Copy(Presets[preset]);
            }

            return result;
        }

        #endregion // Write Operations

        #region Read Operations

        /// <summary>
        /// Read the device information.
        /// </summary>
        /// <returns>
        /// On success, returns <see cref="PowerSupplyResult.OK"/>.
        /// On failure, a relevant error result will be returned.
        /// </returns>
        public PowerSupplyResult GetDeviceInfo()
        {
            if (!Connected)
            {
                return PowerSupplyResult.DeviceNotConnected;
            }

            byte[] outputReport = GetDeviceInfoCommand();
            PrintOutputReportHex(outputReport);
            return ProcessTransaction(outputReport,
                new HandleResponseDelegate(HandleDeviceInfoResponse));
        }

        /// <summary>
        /// Read the system parameters.
        /// </summary>
        /// <returns>
        /// On success, returns <see cref="PowerSupplyResult.OK"/>.
        /// On failure, a relevant error result will be returned.
        /// </returns>
        public PowerSupplyResult GetSystemParams()
        {
            if (!Connected)
            {
                return PowerSupplyResult.DeviceNotConnected;
            }

            byte[] outputReport = GetSystemInfoCommand();
            PrintOutputReportHex(outputReport);
            return ProcessTransaction(outputReport,
                new HandleResponseDelegate(HandleSystemParamsResponse));
        }

        /// <summary>
        /// Read the active status of the device.
        /// The active status provides the actual sensed data.
        /// The application should call this at regular intervals to maintain lock status.
        /// </summary>
        /// <returns>
        /// On success, returns <see cref="PowerSupplyResult.OK"/>.
        /// On failure, a relevant error result will be returned.
        /// </returns>
        public PowerSupplyResult GetActiveStatus()
        {
            if (!Connected)
            {
                return PowerSupplyResult.DeviceNotConnected;
            }

            byte[] outputReport = GetBasicInfoCommand();
            PrintOutputReportHex(outputReport);
            return ProcessTransaction(outputReport,
                new HandleResponseDelegate(HandleActiveStatusResponse));
        }

        /// <summary>
        /// Read the output state information.
        /// </summary>
        /// <returns>
        /// On success, returns <see cref="PowerSupplyResult.OK"/>.
        /// On failure, a relevant error result will be returned.
        /// </returns>
        public PowerSupplyResult GetOutput()
        {
            if (!Connected)
            {
                return PowerSupplyResult.DeviceNotConnected;
            }

            byte[] outputReport = GetBasicSetCommand(BasicSetSubOpCode.GetCurrentBasic, 0);
            PrintOutputReportHex(outputReport);
            return ProcessTransaction(outputReport,
                new HandleResponseDelegate(HandleCurrentOutputResponse));
        }

        /// <summary>
        /// Read a preconfigured preset's non-volatile state.
        /// </summary>
        /// <param name="preset">The preset to get (0-9).</param>
        /// <returns>
        /// On success, returns <see cref="PowerSupplyResult.OK"/>.
        /// On failure, a relevant error result will be returned.
        /// </returns>
        public PowerSupplyResult GetPreset(byte preset)
        {
            if (!Connected)
            {
                return PowerSupplyResult.DeviceNotConnected;
            }

            byte[] outputReport = GetBasicSetCommand(BasicSetSubOpCode.GetGroupInfo, preset);
            PrintOutputReportHex(outputReport);
            return ProcessTransaction(outputReport,
                new HandleResponseDelegate(HandleGroupInfoResponse));
        }

        /// <summary>
        /// Reloads configured state.
        /// This includes getting the current output state.Resets the volatile preset states to their corresponding non-volatile states.
        /// </summary>
        /// <returns>
        /// On success, returns <see cref="PowerSupplyResult.OK"/>.
        /// On failure, a relevant error result will be returned.
        /// </returns>
        public PowerSupplyResult Reload()
        {
            if (!Connected)
            {
                return PowerSupplyResult.DeviceNotConnected;
            }

            PowerSupplyResult result = GetOutput();
            if (result != PowerSupplyResult.OK)
            {
                return result;
            }

            byte activePreset = Output.Preset;
            byte startPreset = (byte)((activePreset + 1) % NumPresets);

            for (int count = 0; count < NumPresets; count++)
            {
                byte i = (byte)((startPreset + count) % NumPresets);

                result = GetPreset(i);
                if (result != PowerSupplyResult.OK)
                {
                    return result;
                }
            }

            return result;
        }

        #endregion // Read Operations

        #endregion // API

        /// <summary>
        /// The worker thread responsible for cyclically polling the active status information
        /// and notifying the application on successful responses.
        /// </summary>
        private void WorkerThread()
        {
            Stopwatch sw = new Stopwatch();
            int milliseconds;
            workerThreadSleepCancellation = workerThreadSleepCts.Token;

            while (workerThreadRun)
            {
                sw.Restart();
                if (GetActiveStatus() == PowerSupplyResult.OK)
                {
                    if (ActiveState.FaultStatus != PowerSupplyFaultStatus.OK)
                    {
                        Output.On = false;
                    }

                    ActiveStateEvent?.Invoke(ActiveState);
                }
                sw.Stop();

                milliseconds = (int)(workerThreadSleepTime.TotalMilliseconds - (int)sw.ElapsedMilliseconds);
                if (milliseconds > 0)
                {
                    try
                    {
                        Task.Delay(milliseconds, workerThreadSleepCancellation).Wait(workerThreadSleepCancellation);
                    }
                    catch (OperationCanceledException)
                    {
                    }
                    finally
                    {
                        // Reset the cancellation token source.
                        workerThreadSleepCts.Dispose();
                        workerThreadSleepCts = new CancellationTokenSource();
                        workerThreadSleepCancellation = workerThreadSleepCts.Token;
                    }
                }
            }
        }

        /// <summary>
        /// To be called to unblock the worker thread and process the next loop.
        /// This is mainly intended to be called by the user related events.
        /// </summary>
        public void SignalRunWorker()
        {
            workerThreadSleepCts.Cancel();
        }

        /// <summary>
        /// Dispose of managed resources.
        /// </summary>
        private void Dispose()
        {
            if (hidStream != null)
            {
                hidStream.Dispose();
            }
        }

        #region Core Utils

        /// <summary>
        /// Process the device list change event and conditionally call the application
        /// event handler when it has been determined the device has been disconnected.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">Event arguments.</param>
        private void ListChangedEvent(object? sender, EventArgs e)
        {
            // Check if the device is still present.
            var hidDevice = list.GetHidDeviceOrNull(VendorID, ProductID, serialNumber: hidSerialNumber);
            if (hidDevice is null)
            {
                Connected = false;
                DisconnectedEvent?.Invoke();
            }
        }

        /// <summary>
        /// Helper routine for constructing an HID output report for sending commands
        /// to the power supply.
        /// </summary>
        /// <param name="opCode">The operation code.</param>
        /// <param name="dataLen">The number of bytes in <see cref="data"/> to send.</param>
        /// <param name="data">The data to send.</param>
        /// <returns>The HID output report data to send to the device.</returns>
        private static byte[] ConstructOutputReport(OperationCode opCode, byte dataLen, byte[] data)
        {
            if (data == null)
            {
                dataLen = 0;
            }

            int offset = 1;
            byte[] byteArray = new byte[6 + dataLen + offset];
            byteArray[0] = 0;
            byteArray[0 + offset] = (byte)Direction.HostToDevice;
            byteArray[1 + offset] = (byte)opCode;
            byteArray[2 + offset] = 0;
            byteArray[3 + offset] = dataLen;

            if (data != null)
            {
                Array.Copy(data, 0, byteArray, 4 + offset, dataLen);
            }

            ComputeCrc16(byteArray);
            return byteArray;
        }

        /// <summary>
        /// Compute the CRC16 for the provided data buffer.
        /// The computation includes all byte except the last two.
        /// The buffer must greater than 2 bytes.
        /// The CRC16 will be applied to the last 2 bytes of the buffer.
        /// </summary>
        /// <param name="data">The data to compute the CRC for.</param>
        /// <param name="len">The length of the data buffer up to and including the CRC16.</param>
        /// <returns>The CRC16. On error the value will be 0.</returns>
        private static ushort ComputeCrc16(byte[] data, int len = -1)
        {
            const ushort polynomial = 0xA001;
            ushort crc = 0xFFFF;

            if (len < 0)
            {
                len = data.Length;
            }

            if (len <= sizeof(ushort))
            {
                return 0;
            }

            for (var b = 1; b < len - sizeof(ushort); b++)
            {
                crc ^= data[b];
                for (int i = 0; i < 8; i++)
                {
                    if ((crc & 1) != 0)
                    {
                        crc = (ushort)((crc >> 1) ^ polynomial);
                    }
                    else
                    {
                        crc >>= 1;
                    }
                }
            }

            data[len - 2] = (byte)((crc >> 0) & 0xFF);
            data[len - 1] = (byte)((crc >> 8) & 0xFF);

            return crc;
        }

        /// <summary>
        /// Print the output report data to console.
        /// </summary>
        /// <param name="data">The data bytes from the output report.</param>
        private void PrintOutputReportHex(byte[] data)
        {
            if (DebugMode)
            {
                Console.WriteLine("Output Report:\n");
                PrintReportHex(data);
            }
        }

        /// <summary>
        /// Print the input report data to console.
        /// </summary>
        /// <param name="data">The data bytes from the input report.</param>
        private void PrintInputReportHex(byte[] data)
        {
            if (DebugMode)
            {
                Console.WriteLine("Input Report:\n");
                PrintReportHex(data);
            }
        }

        /// <summary>
        /// Prints an array of bytes in hexadecimal form.
        /// </summary>
        /// <param name="data">The data to print.</param>
        private static void PrintReportHex(byte[] data)
        {
            byte dataLen = data[4];
            byte minLen = 8;
            for (int i = 1; i < minLen + dataLen; i++)
            {
                // Print the hex value with space
                Console.Write($"{data[i - 1]:X2} ");

                // Print a new line every 16 bytes
                if (i % 16 == 0)
                {
                    Console.WriteLine("");
                }
            }

            // Print a final new line if the last line was not completed
            if ((minLen + dataLen) % 16 != 0)
            {
                Console.WriteLine("");
            }

            Console.WriteLine("");
        }

        /// <summary>
        /// The main handler for facilitating USB HID transactions.
        /// </summary>
        /// <param name="outputReport">The output report data to send to the device.</param>
        /// <param name="handler">
        /// The handler which will be called upon successful receipt of an input report from the device.
        /// </param>
        /// <returns>
        /// On success, returns <see cref="PowerSupplyResult.OK"/>.
        /// On failure, a relevant error result will be returned.
        /// </returns>
        private PowerSupplyResult ProcessTransaction(byte[] outputReport, HandleResponseDelegate handler)
        {
            // Setup transaction details.
            PowerSupplyResult result = PowerSupplyResult.Error;
            var inputReportBuffer = new byte[hidInputReportLength];
            bool done = false;
            IAsyncResult? ar = null;

            hidStreamMutex.WaitOne();

            if (hidStream == null)
            {
                return result;
            }

            try
            {
                hidStream.Write(outputReport);
            }
            catch (IOException)
            {
                Disconnect();
                hidStreamMutex.ReleaseMutex();
                return PowerSupplyResult.DeviceNotConnected;
            }

            var stopwatch = Stopwatch.StartNew();

            while (!done)
            {
                ar ??= hidStream.BeginRead(inputReportBuffer, 0, inputReportBuffer.Length, null, null);

                if (ar != null)
                {
                    if (ar.IsCompleted)
                    {
                        int byteCount = hidStream.EndRead(ar);
                        ar = null;

                        if (byteCount > 0)
                        {
                            PrintInputReportHex(inputReportBuffer);
                            result = handler.Invoke(inputReportBuffer);
                            done = true;
                        }
                    }
                }

                if (stopwatch.Elapsed >= ResponseTimeout) { break; }
            }

            if (!done)
            {
                Debug.WriteLine("Timeout!\n");
                result = PowerSupplyResult.Timeout;
            }

            hidStreamMutex.ReleaseMutex();
            return result;
        }

        #endregion Core Utils

        #region Response Handlers

        /// <summary>
        /// Handle the device information response data.
        /// </summary>
        /// <param name="response">The raw response data.</param>
        /// <returns>
        /// On success, returns <see cref="PowerSupplyResult.OK"/>.
        /// On failure, a relevant error result will be returned.
        /// </returns>
        private PowerSupplyResult HandleDeviceInfoResponse(byte[] response)
        {
            if (Device.Parse(response))
            {
                return PowerSupplyResult.OK;
            }
            else
            {
                return PowerSupplyResult.Error;
            }
        }

        /// <summary>
        /// Handle the system parameter response data.
        /// </summary>
        /// <param name="response">The raw response data.</param>
        /// <returns>
        /// On success, returns <see cref="PowerSupplyResult.OK"/>.
        /// On failure, a relevant error result will be returned.
        /// </returns>
        private PowerSupplyResult HandleSystemParamsResponse(byte[] response)
        {
            if (SystemParams.Parse(response))
            {
                systemParamsValid = true;
                return PowerSupplyResult.OK;
            }
            else
            {
                return PowerSupplyResult.Error;
            }
        }

        /// <summary>
        /// Handle the current output response data.
        /// </summary>
        /// <param name="response">The raw response data.</param>
        /// <returns>
        /// On success, returns <see cref="PowerSupplyResult.OK"/>.
        /// On failure, a relevant error result will be returned.
        /// </returns>
        private PowerSupplyResult HandleCurrentOutputResponse(byte[] response)
        {
            if (Output.Parse(response))
            {
                outputValid = true;
                return PowerSupplyResult.OK;
            }
            else
            {
                return PowerSupplyResult.Error;
            }
        }

        /// <summary>
        /// Handle the group information response data.
        /// </summary>
        /// <param name="response">The raw response data.</param>
        /// <returns>
        /// On success, returns <see cref="PowerSupplyResult.OK"/>.
        /// On failure, a relevant error result will be returned.
        /// </returns>
        private PowerSupplyResult HandleGroupInfoResponse(byte[] response)
        {
            byte index = (byte)(response[ResponseDataIndex] & 0xF);

            if (Presets[index] == null)
            {
                Presets[index] = new PowerSupplySetpoint(index);
            }

            if (Presets[index].Parse(response))
            {
                presetsValid[index] = true;
                return PowerSupplyResult.OK;
            }
            else
            {
                return PowerSupplyResult.Error;
            }
        }

        /// <summary>
        /// Handle the ack/nack response data.
        /// </summary>
        /// <param name="response">The raw response data.</param>
        /// <returns>
        /// On success, returns <see cref="PowerSupplyResult.OK"/>.
        /// On failure, a relevant error result will be returned.
        /// </returns>
        private PowerSupplyResult HandleAckResponse(byte[] response)
        {
            bool ack = false;

            if (response[ResponseLenIndex] == 1)
            {
                if (response[ResponseDataIndex] == 1)
                {
                    ack = true;
                }
            }

            if (ack)
            {
                return PowerSupplyResult.OK;
            }
            else
            {
                return PowerSupplyResult.Error;
            }
        }

        /// <summary>
        /// Handle the active status response data.
        /// </summary>
        /// <param name="response">The raw response data.</param>
        /// <returns>
        /// On success, returns <see cref="PowerSupplyResult.OK"/>.
        /// On failure, a relevant error result will be returned.
        /// </returns>
        private PowerSupplyResult HandleActiveStatusResponse(byte[] response)
        {
            if (ActiveState.Parse(response))
            {
                return PowerSupplyResult.OK;
            }
            else
            {
                return PowerSupplyResult.Error;
            }
        }

        #endregion Response Handlers

        #region Commands

        /// <summary>
        /// Creates the command payload for getting device information.
        /// </summary>
        /// <returns>The output report payload for the command.</returns>
        private static byte[] GetDeviceInfoCommand()
        {
            byte[] buf = new byte[1];
            byte[] data = ConstructOutputReport(OperationCode.DeviceInfo, 0, buf);
            ComputeCrc16(data);

            return data;
        }

        /// <summary>
        /// Creates the command payload for getting system information.
        /// </summary>
        /// <returns>The output report payload for the command.</returns>
        private static byte[] GetSystemInfoCommand()
        {
            byte[] buf = new byte[1];
            byte[] data = ConstructOutputReport(OperationCode.SystemInfo, 0, buf);
            ComputeCrc16(data);

            return data;
        }

        /// <summary>
        /// Creates the command payload for getting basic information.
        /// </summary>
        /// <returns>The output report payload for the command.</returns>
        private static byte[] GetBasicInfoCommand()
        {
            byte[] buf =
            [
                0x00
            ];
            byte[] data = ConstructOutputReport(OperationCode.BasicInfo, 1, buf);
            ComputeCrc16(data);

            return data;
        }

        /// <summary>
        /// Creates the command payload for getting basic information.
        /// </summary>
        /// <param name="opcode">The opcode.</param>
        /// <param name="preset">The preset index.</param>
        /// <returns>The output report payload for the command.</returns>
        private static byte[] GetBasicSetCommand(BasicSetSubOpCode opcode, byte preset)
        {
            byte[] buf;
            if (opcode == BasicSetSubOpCode.UseGroup)
            {
                // UseGroup does not use the other data but requires
                // the length to be 10 bytes.
                buf = [
                    (byte)((byte)opcode | (preset & 0xF)),
                    0, 0, 0, 0, 0, 0, 0, 0, 0
                ];
            }
            else if (opcode == BasicSetSubOpCode.GetCurrentBasic)
            {
                buf = [(byte)opcode];
            }
            else
            {
                buf = [
                    (byte)((byte)opcode | (preset & 0xF))
                ];
            }

            byte[] data = ConstructOutputReport(OperationCode.BasicSet, (byte)buf.Length, buf);
            ComputeCrc16(data);

            return data;
        }

        /// <summary>
        /// Creates the command payload for getting basic information.
        /// </summary>
        /// <param name="opcode">The opcode.</param>
        /// <param name="preset">The preset index.</param>
        /// <param name="outputOn">The output state.</param>
        /// <param name="v_set">The voltage setpoint (in millivolts).</param>
        /// <param name="i_set">The current setpoint (in milliamps).</param>
        /// <param name="ovp">The over-voltage protection (in millivolts).</param>
        /// <param name="ocp">The over-current protection (in milliamps).</param>
        /// <returns>The output report payload for the command.</returns>
        private static byte[] SetBasicSetCommand(BasicSetSubOpCode opcode, byte preset, bool outputOn, ushort v_set, ushort i_set, ushort ovp, ushort ocp)
        {
            byte[] buf =
            [
                (byte)((byte)opcode | (preset & 0xF)),
                (byte)((outputOn == true) ? 1 : 0),
                (byte)((v_set >> 0) & 0xFF),
                (byte)((v_set >> 8) & 0xFF),
                (byte)((i_set >> 0) & 0xFF),
                (byte)((i_set >> 8) & 0xFF),
                (byte)((ovp >> 0) & 0xFF),
                (byte)((ovp >> 8) & 0xFF),
                (byte)((ocp >> 0) & 0xFF),
                (byte)((ocp >> 8) & 0xFF)
            ];
            byte[] data = ConstructOutputReport(OperationCode.BasicSet, (byte)buf.Length, buf);
            ComputeCrc16(data);

            return data;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="otp"></param>
        /// <param name="opp"></param>
        /// <param name="backlight"></param>
        /// <param name="volume"></param>
        /// <param name="rpp"></param>
        /// <param name="autoOn"></param>
        /// <returns></returns>
        private static byte[] SetSystemParamsCommand(ushort otp, ushort opp, byte backlight, byte volume, bool rpp, bool autoOn)
        {
            const byte maxVolume = 4;
            const byte maxBrightness = 4;
            if (volume > maxVolume)
            {
                volume = maxVolume;
            }

            if (backlight > maxBrightness)
            {
                backlight = maxBrightness;
            }

            byte[] buf =
            [
                (byte)((otp >> 0) & 0xFF),
                (byte)((otp >> 8) & 0xFF),
                (byte)((opp >> 0) & 0xFF),
                (byte)((opp >> 8) & 0xFF),
                backlight,
                volume,
                (byte)((rpp == true) ? 1 : 0),
                (byte)((autoOn == true) ? 1 : 0)
            ];

            // NOTE: OperationCode.SystemSet does not work here.
            byte[] data = ConstructOutputReport(OperationCode.SystemInfo, (byte)buf.Length, buf);
            ComputeCrc16(data);

            return data;
        }
        #endregion Commands
    }
}
