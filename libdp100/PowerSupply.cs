using HidSharp;
using System.Diagnostics;

namespace LibDP100
{
    public class PowerSupply
    {
        // Indicates if the device is currently connected.
        public bool Connected { get; private set; } = false;

        // Contains device information.
        public PowerSupplyInfo Device { get; private set; } = new PowerSupplyInfo();

        // Contains active status information.
        public PowerSupplyActiveState ActiveState { get; private set; } = new PowerSupplyActiveState();

        // Contains the output state.
        public PowerSupplyOutput Output { get; private set; } = new PowerSupplyOutput();

        // Contains the output presets.
        public PowerSupplySetpoint[] Presets { get; private set; } = new PowerSupplySetpoint[NumPresets];

        // Contains the system parameters.
        public PowerSupplySystemParams SystemParams { get; private set; } = new PowerSupplySystemParams();

        // Set by application to receive regular "ActiveState" updates.
        public ActiveStateDelegate? ActiveStateEvent { get; set; } = null;

        // Set by application to receive disconnected events not initiated via Disconnect().
        public DisconnectedDelegate? DisconnectedEvent { get; set; } = null;

        // Delegate for providing regular updates to the application.
        public delegate void ActiveStateDelegate(PowerSupplyActiveState activeState);

        // Delegate for device disconnected notification.
        public delegate void DisconnectedDelegate();

        // Switch between normal and debug mode.
        // When set to debug mode, the Raw HID report data will be printed to the console.
        public bool DebugMode { get; set; } = false;

        #region Private Members

        // The USB Vendor ID to search for during enumeration.
        const ushort VendorID = 0x2E3C;

        // The USB Product ID to search for during enumeration.
        const ushort ProductID = 0xAF01;

        // The number of presets available on the device.
        const byte NumPresets = 10;

        // The index in the HID report where data begins.
        const int ResponseDataIndex = 5;

        // The index in the HID report where the length is specified.
        const int ResponseLenIndex = 4;

        // Indicates that the "Device" object has been populated with valid data.
        bool deviceValid = false;

        // Indicates that the "ActiveState" object has been populated with valid data.
        bool activeStateValid = false;

        // Indicates that the "Output" object has been populated with valid data.
        bool outputValid = false;

        // Indicates that the "Presets" object has been populated with valid data.
        bool[] presetsValid = new bool[NumPresets];

        // Indicates that the "SystemParams" object has been populated with valid data.
        bool systemParamsValid = false;

        // The current listing of USB devices. Used to monitor changes in the device listing.
        DeviceList list = DeviceList.Local;

        // Indicates the number of bytes used for the HID input reports.
        int hidInputReportLength;

        // Indicates the HID serial number. This value may not be the same as the serial number
        // obtained from the DeviceInfo command. This value is used to determine device-disconnect
        // events.
        string hidSerialNumber;

        // The HID stream in which all communications is performed through.
        HidStream hidStream;

        // Mutex to ensure single-access to underlying HID stream.
        Mutex hidStreamMutex = new();

        // The timeout for HID responses.
        TimeSpan ResponseTimeout { get; set; } = TimeSpan.FromMilliseconds(2000);

        // The actual debug mode state.
        bool debugMode = false;

        // The thread that processes reading actual voltage and current.
        Thread workerThread;

        // Indicates whether the worker thread should continue to run.
        bool workerThreadRun;

        // The time between each read performed by the worker thread.
        TimeSpan workerThreadSleepTime;

        delegate PowerSupplyResult HandleResponseDelegate(byte[] inputReportBuffer);

        #endregion // Private Members

        public PowerSupply()
        {
            list.Changed += ListChangedEvent;
        }

        #region Core Utils

        /// <summary>
        /// Process the device list change event and conditionally call the application
        /// event handler when it has been determined the device has been disconnected.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void ListChangedEvent(object sender, EventArgs e)
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
        /// <param name="direction"></param>
        /// <param name="opCode"></param>
        /// <param name="dataLen"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        static byte[] ConstructOutputReport(Direction direction, OperationCode opCode, byte dataLen, byte[] data)
        {
            if (data == null)
            {
                dataLen = 0;
            }

            int offset = 1;
            byte[] byteArray = new byte[6 + dataLen + offset];
            byteArray[0] = 0;
            byteArray[0 + offset] = (byte)direction;
            byteArray[1 + offset] = (byte)opCode;
            byteArray[2 + offset] = 0;
            byteArray[3 + offset] = dataLen;
            Array.Copy(data, 0, byteArray, 4 + offset, dataLen);

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
        static ushort ComputeCrc16(byte[] data, int len = -1)
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
        void PrintOutputReportHex(byte[] data)
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
        void PrintInputReportHex(byte[] data)
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
        static void PrintReportHex(byte[] data)
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
        PowerSupplyResult ProcessTransaction(byte[] outputReport, HandleResponseDelegate handler)
        {
            // Setup transaction details.
            PowerSupplyResult result = PowerSupplyResult.Error;
            var inputReportBuffer = new byte[hidInputReportLength];
            bool done = false;
            IAsyncResult? ar = null;

            hidStreamMutex.WaitOne();

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
        ///
        /// </summary>
        /// <param name="response"></param>
        /// <returns>
        /// On success, returns <see cref="PowerSupplyResult.OK"/>.
        /// On failure, a relevant error result will be returned.
        /// </returns>
        PowerSupplyResult HandleDeviceInfoResponse(byte[] response)
        {
            if (Device.Parse(response))
            {
                deviceValid = true;
                return PowerSupplyResult.OK;
            }
            else
            {
                return PowerSupplyResult.Error;
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="response"></param>
        /// <returns>
        /// On success, returns <see cref="PowerSupplyResult.OK"/>.
        /// On failure, a relevant error result will be returned.
        /// </returns>
        PowerSupplyResult HandleSystemParamsResponse(byte[] response)
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
        ///
        /// </summary>
        /// <param name="response"></param>
        /// <returns>
        /// On success, returns <see cref="PowerSupplyResult.OK"/>.
        /// On failure, a relevant error result will be returned.
        /// </returns>
        PowerSupplyResult HandleCurrentOutputResponse(byte[] response)
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
        ///
        /// </summary>
        /// <param name="response"></param>
        /// <returns>
        /// On success, returns <see cref="PowerSupplyResult.OK"/>.
        /// On failure, a relevant error result will be returned.
        /// </returns>
        PowerSupplyResult HandleGroupInfoResponse(byte[] response)
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
        ///
        /// </summary>
        /// <param name="response"></param>
        /// <returns>
        /// On success, returns <see cref="PowerSupplyResult.OK"/>.
        /// On failure, a relevant error result will be returned.
        /// </returns>
        PowerSupplyResult HandleAckResponse(byte[] response)
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
        ///
        /// </summary>
        /// <param name="response"></param>
        /// <returns>
        /// On success, returns <see cref="PowerSupplyResult.OK"/>.
        /// On failure, a relevant error result will be returned.
        /// </returns>
        PowerSupplyResult HandleActiveStateResponse(byte[] response)
        {
            if (ActiveState.Parse(response))
            {
                activeStateValid = true;
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
        static byte[] GetDeviceInfoCommand()
        {
            byte[] buf = new byte[1];
            byte[] data = ConstructOutputReport(Direction.HostToDevice, OperationCode.DeviceInfo, 0, buf);
            ComputeCrc16(data);

            return data;
        }

        /// <summary>
        /// Creates the command payload for getting system information.
        /// </summary>
        /// <returns>The output report payload for the command.</returns>
        static byte[] GetSystemInfoCommand()
        {
            byte[] buf = new byte[1];
            byte[] data = ConstructOutputReport(Direction.HostToDevice, OperationCode.SystemInfo, 0, buf);
            ComputeCrc16(data);

            return data;
        }

        /// <summary>
        /// Creates the command payload for getting basic information.
        /// </summary>
        /// <returns>The output report payload for the command.</returns>
        static byte[] GetBasicInfoCommand()
        {
            byte[] buf =
            [
                0x00
            ];
            byte[] data = ConstructOutputReport(Direction.HostToDevice, OperationCode.BasicInfo, 1, buf);
            ComputeCrc16(data);

            return data;
        }

        /// <summary>
        /// Creates the command payload for getting basic information.
        /// </summary>
        /// <param name="opcode">The opcode.</param>
        /// <param name="preset">The preset index.</param>
        /// <returns>The output report payload for the command.</returns>
        static byte[] GetBasicSetCommand(BasicSetSubOpCode opcode, byte preset)
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

            byte[] data = ConstructOutputReport(Direction.HostToDevice, OperationCode.BasicSet, (byte)buf.Length, buf);
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
        static byte[] SetBasicSetCommand(BasicSetSubOpCode opcode, byte preset, bool outputOn, ushort v_set, ushort i_set, ushort ovp, ushort ocp)
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
            byte[] data = ConstructOutputReport(Direction.HostToDevice, OperationCode.BasicSet, (byte)buf.Length, buf);
            ComputeCrc16(data);

            return data;
        }

        static byte[] SetSystemParamsCommand(ushort otp, ushort opp, byte backlight, byte volume, bool rpp, bool autoOn)
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
            byte[] data = ConstructOutputReport(Direction.HostToDevice, OperationCode.SystemInfo, (byte)buf.Length, buf);
            ComputeCrc16(data);

            return data;
        }
        #endregion Commands

        #region API

        /// <summary>
        /// Switch the power supply output ON.
        /// </summary>
        /// <returns></returns>
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

            if (result == PowerSupplyResult.OK)
            {
                Output.On = true;
            }

            return result;
        }

        /// <summary>
        /// Switch the power supply output OFF.
        /// </summary>
        /// <returns></returns>
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

            if (result == PowerSupplyResult.OK)
            {
                Output.On = false;
            }

            return result;
        }

        /// <summary>
        /// Toggle the power supply output state.
        /// </summary>
        /// <returns></returns>
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

            if (result == PowerSupplyResult.OK)
            {
                Output.On = !Output.On;
            }

            return result;
        }

        /// <summary>
        /// Set the power supply output voltage.
        /// </summary>
        /// <returns></returns>
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

            if (result == PowerSupplyResult.OK)
            {
                Output.Setpoint.Voltage = millivolts;
            }

            return result;
        }

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

            if (result == PowerSupplyResult.OK)
            {
                Output.Setpoint.Current = milliamps;
            }

            return result;
        }

        public PowerSupplyResult SetBacklight(byte brightness)
        {
            PowerSupplyResult result = PowerSupplyResult.OK;

            if (!systemParamsValid)
            {
                result = GetSystemParams();
            }

            if (result == PowerSupplyResult.OK)
            {
                result = this.SetSystemParams(SystemParams.OTP, SystemParams.OPP, brightness, SystemParams.Volume, SystemParams.RPP, SystemParams.AutoOn);
            }

            if (result == PowerSupplyResult.OK)
            {
                SystemParams.Backlight = brightness;
            }

            return result;
        }

        public PowerSupplyResult SetVolume(byte volume)
        {
            PowerSupplyResult result = PowerSupplyResult.OK;

            if (!systemParamsValid)
            {
                result = GetSystemParams();
            }

            if (result == PowerSupplyResult.OK)
            {
                result = this.SetSystemParams(SystemParams.OTP, SystemParams.OPP, SystemParams.Backlight, volume, SystemParams.RPP, SystemParams.AutoOn);
            }

            if (result == PowerSupplyResult.OK)
            {
                SystemParams.Volume = volume;
            }

            return result;
        }

        public PowerSupplyResult SetAutoOn(bool enable)
        {
            PowerSupplyResult result = PowerSupplyResult.OK;

            if (!systemParamsValid)
            {
                result = GetSystemParams();
            }

            if (result == PowerSupplyResult.OK)
            {
                result = this.SetSystemParams(SystemParams.OTP, SystemParams.OPP, SystemParams.Backlight, SystemParams.Volume, SystemParams.RPP, enable);
            }

            if (result == PowerSupplyResult.OK)
            {
                SystemParams.AutoOn = enable;
            }

            return result;
        }

        public PowerSupplyResult SetRPP(bool enable)
        {
            PowerSupplyResult result = PowerSupplyResult.OK;

            if (!systemParamsValid)
            {
                result = GetSystemParams();
            }

            if (result == PowerSupplyResult.OK)
            {
                result = this.SetSystemParams(SystemParams.OTP, SystemParams.OPP, SystemParams.Backlight, SystemParams.Volume, enable, SystemParams.AutoOn);
            }

            if (result == PowerSupplyResult.OK)
            {
                SystemParams.RPP = enable;
            }

            return result;
        }

        public PowerSupplyResult SetOPP(ushort deciwatts)
        {
            PowerSupplyResult result = PowerSupplyResult.OK;

            if (!systemParamsValid)
            {
                result = GetSystemParams();
            }

            if (result == PowerSupplyResult.OK)
            {
                result = this.SetSystemParams(SystemParams.OTP, deciwatts, SystemParams.Backlight, SystemParams.Volume, SystemParams.RPP, SystemParams.AutoOn);
            }

            if (result == PowerSupplyResult.OK)
            {
                SystemParams.OPP = deciwatts;
            }

            return result;
        }

        public PowerSupplyResult SetOTP(ushort decicelcius)
        {
            PowerSupplyResult result = PowerSupplyResult.OK;

            if (!systemParamsValid)
            {
                result = GetSystemParams();
            }

            if (result == PowerSupplyResult.OK)
            {
                result = this.SetSystemParams(decicelcius, SystemParams.OPP, SystemParams.Backlight, SystemParams.Volume, SystemParams.RPP, SystemParams.AutoOn);
            }

            if (result == PowerSupplyResult.OK)
            {
                SystemParams.OTP = decicelcius;
            }

            return result;
        }

        // FaultNotification()
        // ConnectionNotification()
        //
        // Do these make sense to define?
        // SetOVP()
        // SetOCP()

        // TODO: for each of the Write operations below...make sure to update the object state values!!!!

        public PowerSupplyResult SetOutput(PowerSupplySetpoint setpoint)
        {
            return SetOutput(Output.On, setpoint.Voltage, setpoint.Current);
        }

        /// <summary>
        /// Write Setpoint.
        /// Use to switch the output ON/OFF.
        /// Use to change setpoint voltage/current.
        /// Does not affect OCP/OCP or configured preset state.
        /// </summary>
        /// <param name="outputOn"></param>
        /// <param name="millivolts"></param>
        /// <param name="milliamps"></param>
        /// <returns></returns>
        public PowerSupplyResult SetOutput(bool outputOn, ushort millivolts, ushort milliamps)
        {
            if (!Connected)
            {
                return PowerSupplyResult.DeviceNotConnected;
            }

            const byte preset = 0; // Not applicable
            const byte ovp = 0; // Not applicable
            const byte ocp = 0; // Not applicable
            byte[] outputReport = SetBasicSetCommand(BasicSetSubOpCode.SetCurrentBasic, preset, outputOn, millivolts, milliamps, ovp, ocp);
            PrintOutputReportHex(outputReport);
            PowerSupplyResult result = ProcessTransaction(outputReport,
                new HandleResponseDelegate(HandleAckResponse));

            if (result == PowerSupplyResult.OK)
            {
                Output.On = outputOn;
                Output.Setpoint.Voltage = millivolts;
                Output.Setpoint.Current = milliamps;
            }

            return result;
        }

        public PowerSupplyResult SetPreset(byte preset, PowerSupplySetpoint setpoint)
        {
            return SetPreset(preset, setpoint.Voltage, setpoint.Current, setpoint.OVP, setpoint.OCP);
        }

        /// <summary>
        /// Saves the specified parameters to a non-volatile preset.
        /// </summary>
        /// <param name="preset"></param>
        /// <param name="millivolts"></param>
        /// <param name="milliamps"></param>
        /// <param name="ovp"></param>
        /// <param name="ocp"></param>
        /// <returns></returns>
        public PowerSupplyResult SetPreset(byte preset, ushort millivolts, ushort milliamps, ushort ovp, ushort ocp)
        {
            if (!Connected)
            {
                return PowerSupplyResult.DeviceNotConnected;
            }

            bool outputOn = false; // Not applicable
            byte[] outputReport = SetBasicSetCommand(BasicSetSubOpCode.SaveGroup, preset, outputOn, millivolts, milliamps, ovp, ocp);
            PrintOutputReportHex(outputReport);
            PowerSupplyResult result = ProcessTransaction(outputReport,
                new HandleResponseDelegate(HandleAckResponse));

            if (result == PowerSupplyResult.OK)
            {
                Presets[preset].Voltage = millivolts;
                Presets[preset].Current = milliamps;
                Presets[preset].OVP = ovp;
                Presets[preset].OCP = ocp;
            }

            return result;
        }

        public PowerSupplyResult SetSystemParams(PowerSupplySystemParams systemParams)
        {
            return SetSystemParams(systemParams.OTP, systemParams.OPP, systemParams.Backlight, systemParams.Volume, systemParams.RPP, systemParams.AutoOn);
        }

        /// <summary>
        /// Saves the specified system parameters to non-volatile memory.
        /// </summary>
        /// <param name="otp"></param>
        /// <param name="opp"></param>
        /// <param name="backlight"></param>
        /// <param name="volume"></param>
        /// <param name="rpp"></param>
        /// <param name="autoOn"></param>
        /// <returns></returns>
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
        /// Used to switch to a preconfigured preset.
        /// </summary>
        /// <param name="preset">The preset index (range 0-9).</param>
        /// <returns></returns>
        public PowerSupplyResult UsePreset(byte preset)
        {
            if (!Connected)
            {
                return PowerSupplyResult.DeviceNotConnected;
            }

            byte[] outputReport = GetBasicSetCommand(BasicSetSubOpCode.UseGroup, preset);
            PrintOutputReportHex(outputReport);
            PowerSupplyResult result = ProcessTransaction(outputReport,
                new HandleResponseDelegate(HandleAckResponse));

            if (result == PowerSupplyResult.OK)
            {
                Output.On = false;
                Output.Preset = preset;
                Output.Setpoint.Copy(Presets[preset]);
            }

            return result;
        }

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
        /// <returns></returns>
        public PowerSupplyResult GetActiveStatus()
        {
            if (!Connected)
            {
                return PowerSupplyResult.DeviceNotConnected;
            }

            byte[] outputReport = GetBasicInfoCommand();
            PrintOutputReportHex(outputReport);
            return ProcessTransaction(outputReport,
                new HandleResponseDelegate(HandleActiveStateResponse));
        }

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
        /// Read a preconfigured preset.
        /// </summary>
        /// <param name="preset"></param>
        /// <returns></returns>
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

        #endregion

        /// <summary>
        /// Connect to the first enumerated DP100.
        /// </summary>
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
        /// <param name="serialNumber"></param>
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

        public void Disconnect()
        {
            Connected = false;
            Dispose();
        }

        public void Dispose()
        {
            if (hidStream != null)
            {
                hidStream.Dispose();
            }
        }

        public void StartWorkerThread(TimeSpan sleepTime)
        {
            if (workerThreadRun)
            {
                return;
            }

            workerThread = new Thread(new ThreadStart(WorkerThread));
            workerThreadRun = true;
            workerThread.Start();
            workerThreadSleepTime = sleepTime;
        }

        public void StopWorkerThread()
        {
            workerThreadRun = false;
        }

        private void WorkerThread()
        {
            Stopwatch sw = new Stopwatch();
            int milliseconds;

            while (workerThreadRun)
            {
                sw.Start();
                if (GetActiveStatus() == PowerSupplyResult.OK)
                {
                    if (ActiveState.FaultStatus != PowerSupplyFaultStatus.OK)
                    {
                        Output.On = false;
                    }

                    ActiveStateEvent?.Invoke(ActiveState);
                }
                sw.Stop();
                milliseconds = workerThreadSleepTime.Milliseconds - (int)sw.ElapsedMilliseconds;
                if (milliseconds > 0)
                {
                    Thread.Sleep(milliseconds);
                }
            }
        }

        #region Overloads

        public int CompareTo(object obj)
        {
            var otherPsu = (PowerSupply)obj;
            return string.Compare(Device.SerialNumber, otherPsu.Device.SerialNumber);
        }

        #endregion
    }
}
