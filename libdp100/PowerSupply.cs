using ATK_DP100DLL;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;

namespace LibDP100
{
    /// <summary>
    /// A wrapper for the DP100 providing basic helper functions and an overall cleaner,
    /// more C# centric API. Most notably nulls the output of the API's console writes.
    /// </summary>
    public class PowerSupply : IComparable
    {
        const byte NumPresets = 10;
        const ushort InvalidVoltage = 0xFFFF;
        const ushort InvalidByte= 0xFFFF;

        // The instance of the DP100 API.
        private ATKDP100API ApiInstance;

        // Switch between normal and debug mode.
        public bool DebugMode
        {
            get
            {
                return debugMode;
            }
            set
            {
                disableOutputLevel = 0;
                debugMode = value;
            }
        }

        // Indicates whether the device is currently connected.
        private bool Connected { get; set; } = false;

        // The configured output state information.
        public PowerSupplyOutput Output { get; private set; } = new PowerSupplyOutput();

        // The actual outputs sensed by the power supply.
        public PowerSupplyActuals ActualOutput { get; private set; } = new PowerSupplyActuals();

        // The stored presets on teh device.
        public PowerSupplySetpoint[] PresetParams { get; private set; } = new PowerSupplySetpoint[NumPresets];

        // The stored system parameters.
        public PowerSupplySystemParams SystemParams { get; private set; } = new PowerSupplySystemParams();

        // The connected device's information.
        public PowerSupplyInfo Device { get; private set; } = new PowerSupplyInfo();

        #region Private fields
        // The actual debug mode state.
        private bool debugMode = false;

        // Used for handling nested calls that may call NullStdOutput/RestoreStdOutput multiple times.
        private int disableOutputLevel = 0;

        // Indicates whether the supply output state and setpoint information is valid.
        private bool outputParamsValid = false;

        // Indicates whether the preset information is valid.
        private bool[] presetParamsValid = new bool[NumPresets];

        // Indicates whether the system parameters are valid.
        private bool systemParamsValid = false;

        // Indicates whether the device information is valid.
        private bool infoValid = false;
        #endregion Private fields

        // Set to application routine to receive updates when new V/I values are received.
        public ActualOutputDelegate ActualOutputEvent { get; set; } = null;

        public delegate void ActualOutputDelegate(PowerSupplyActuals output);

        // The thread that processes reading actual voltage and current.
        Thread workerThread;

        // Indicates whether the worker thread should continue to run.
        bool workerThreadRun;

        // The time between each read performed by the worker thread.
        TimeSpan workerThreadSleepTime;

        // A temporary stringwriter instance for suppressing and/or selectively
        // parsing stdout for data not exposed via the API.
        StringWriter stdoutReceiver = new StringWriter();

        /// <summary>
        /// Constructor for the power supply.
        /// </summary>
        public PowerSupply()
        {
            ApiInstance = new ATKDP100API();
            ApiInstance.ReceBasicInfoEvent += ReceiveActualVI;
            // This API does not appear to be functional.
            //ApiInstance.DevStateChanageEvent += DevStateChange;
        }

        // Mutex to ensure single-access to underlying API.
        private static Mutex apiMutex = new Mutex();

        private void DevStateChange(bool state)
        {
            Output.On = state;
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
            NullStdOutput();

            lock (ApiInstance)
            {
                while (workerThreadRun)
                {
                    sw.Start();
                    apiMutex.WaitOne();
                    ApiInstance.GetBasicInfo();
                    Monitor.Wait(ApiInstance);
                    apiMutex.ReleaseMutex();
                    sw.Stop();
                    milliseconds = workerThreadSleepTime.Milliseconds - (int)sw.ElapsedMilliseconds;
                    if (milliseconds > 0)
                    {
                        Thread.Sleep(milliseconds);
                    }
                }
            }

            RestoreStdOutput();
        }

        private void ReceiveActualVI(ushort voltage, ushort current)
        {
            lock (ApiInstance)
            {
                PowerSupplyActuals output = new PowerSupplyActuals
                {
                    Timestamp = DateTime.Now,
                    Current = current,
                    Voltage = voltage
                };

                ActualOutput = output;
                ParseSupplyData(output);

                if (ActualOutputEvent != null)
                {
                    // Give STDOUT back to the application during callback.
                    // During this time, no PSU calls should be made.
                    RestoreStdOutput();
                    ActualOutputEvent(output);
                    NullStdOutput();
                }

                Monitor.PulseAll(ApiInstance);
            }
        }

        public bool Connect()
        {
            bool connected = ApiInstance.ConnState;

            if (!connected)
            {
                apiMutex.WaitOne();
                NullStdOutput();
                connected = ApiInstance.DevOpenOrClose();
                apiMutex.ReleaseMutex();
                RestoreStdOutput();
            }

            return connected;
        }

        public bool ConnectAndRefresh()
        {
            bool result = Connect();

            if (result)
            {
                result = RefreshOutputParams();
            }

            if (result)
            {
                result = RefreshDevInfo();
            }

            return result;
        }

        public bool Disconnect()
        {
            bool connected = ApiInstance.ConnState;

            if (connected)
            {
                apiMutex.WaitOne();
                NullStdOutput();
                connected = ApiInstance.DevOpenOrClose();
                apiMutex.ReleaseMutex();
                RestoreStdOutput();
            }

            return connected;
        }

        #region Set Operations

        public bool SetOutputOn()
        {
            bool result = true;

            if (!Output.On)
            {
                apiMutex.WaitOne();
                NullStdOutput();
                result = ApiInstance.OpenOut(Output.Preset, Output.Setpoint.Current, Output.Setpoint.Voltage);
                apiMutex.ReleaseMutex();
                RestoreStdOutput();

                if (result)
                {
                    Output.On = true;
                }
            }

            return result;
        }

        public bool SetOutputOff()
        {
            bool result = true;

            if (Output.On)
            {
                apiMutex.WaitOne();
                NullStdOutput();
                result = ApiInstance.CloseOut(Output.Preset, Output.Setpoint.Current, Output.Setpoint.Voltage);
                apiMutex.ReleaseMutex();
                RestoreStdOutput();

                if (result)
                {
                    Output.On = false;
                }
            }

            return result;
        }

        public bool SetSetpoint(PowerSupplySetpoint setpoint)
        {
            bool result;

            apiMutex.WaitOne();
            NullStdOutput();
            // Maintain the output state of the device, only change the setpoint(s).
            // NOTE: DP100 API used seemingly backwards terminology (close = OFF; open = ON).
            if (Output.On)
            {
                result = ApiInstance.OpenOut(Output.Preset, setpoint.Current, setpoint.Voltage);
            }
            else
            {
                result = ApiInstance.CloseOut(Output.Preset, setpoint.Current, setpoint.Voltage);
            }
            apiMutex.ReleaseMutex();
            RestoreStdOutput();

            if (result)
            {
                Output.Setpoint.Copy(setpoint);
            }

            return result;
        }

        public bool SavePreset(byte index, PowerSupplySetpoint setpoint)
        {
            apiMutex.WaitOne();
            NullStdOutput();
            bool result = ApiInstance.SetBasicInfo(
                index,
                setpoint.Current,
                setpoint.Voltage,
                setpoint.OCP,
                setpoint.OVP);
            apiMutex.ReleaseMutex();
            RestoreStdOutput();

            if (result)
            {
                if (PresetParams[index] == null)
                {
                    PresetParams[index] = new PowerSupplySetpoint();
                }

                PresetParams[index].Copy(setpoint);
            }

            return result;
        }

        public bool SetSetpointPreset(byte index, PowerSupplySetpoint setpoint)
        {
            apiMutex.WaitOne();
            NullStdOutput();
            bool result = ApiInstance.SetGroupInfo(
                index,
                setpoint.Current,
                setpoint.Voltage,
                setpoint.OCP,
                setpoint.OVP);
            apiMutex.ReleaseMutex();
            RestoreStdOutput();

            if (result)
            {
                if (PresetParams[index] == null)
                {
                    PresetParams[index] = new PowerSupplySetpoint();
                }

                PresetParams[index].Copy(setpoint);
            }

            return result;
        }

        public bool SetOutputToPreset(byte index)
        {
            bool result = RefreshPreset(index);

            if (result)
            {
                apiMutex.WaitOne();
                NullStdOutput();
                result = ApiInstance.UseGroup(
                    index,
                    PresetParams[index].Current,
                    PresetParams[index].Voltage,
                    PresetParams[index].OCP,
                    PresetParams[index].OVP);
                apiMutex.ReleaseMutex();
                RestoreStdOutput();
            }

            if (result)
            {
                if (index != Output.Preset)
                {
                    // The DP100 turns output off when switching presets.
                    // Do the same on the host side, to keep things in sync.
                    Output.On = false;
                    Output.Preset = index;
                }

                SetSetpoint(PresetParams[index]);
            }

            return result;
        }

        public bool SetSystemParams(PowerSupplySystemParams sysParams)
        {
            bool result;

            apiMutex.WaitOne();
            NullStdOutput();
            result = ApiInstance.SetSysPar(
                sysParams.Backlight,
                sysParams.Volume,
                sysParams.OPP,
                sysParams.OTP);
            apiMutex.ReleaseMutex();
            RestoreStdOutput();

            if (result)
            {
                SystemParams.Copy(sysParams);
            }

            return result;
        }

        #endregion Set Operations

        #region Refresh Operations

        private ushort ParseUInt16(string[] elems, int offset)
        {
            // Convert the hexadecimal bytes to integers
            byte loByte = Convert.ToByte(elems[offset], 16);
            byte hiByte = Convert.ToByte(elems[offset + 1], 16);

            // Combine the bytes into a 16-bit integer (little-endian)
            ushort result = (ushort)((hiByte << 8) | loByte);

            return result;
        }

        private void ParseSystemData(PowerSupplySystemParams system)
        {
            StringBuilder sb = stdoutReceiver.GetStringBuilder();
            string[] messages = sb.ToString().Split('\n');
            int sysParamsIndex = -1;
            for (int i = 0; i < messages.Length; i++)
            {
                // 00
                // FA - D2H (Response)
                // 30 - Basic Info
                // 00
                // 10 - Length (16 bytes)
                if (messages[i].StartsWith("收：00 FA 40 00 08"))
                {
                    sysParamsIndex = i;
                }
            }

            if (sysParamsIndex == -1)
            {
                return;
            }

            string[] elems = messages[sysParamsIndex].Split('：'); // This is not the normal colon character!

            if (elems.Length != 2)
            {
                return;
            }

            elems = elems[1].Split();

            if (elems.Length < 23)
            {
                return;
            }

            system.OTP = ParseUInt16(elems, 5);
            system.OPP = ParseUInt16(elems, 7);
            system.Backlight = Convert.ToByte(elems[9], 16);
            system.Volume = Convert.ToByte(elems[10], 16);
            system.RPP = Convert.ToByte(elems[11], 16) != 0;
            system.AutoOn = Convert.ToByte(elems[12], 16) != 0;
        }

        private void ParseSupplyData(PowerSupplyActuals output)
        {
            StringBuilder sb = stdoutReceiver.GetStringBuilder();
            string[] messages = sb.ToString().Split('\n');
            int basicInfoIndex = -1;
            for (int i = 0; i < messages.Length; i++)
            {
                // 00
                // FA - D2H (Response)
                // 30 - Basic Info
                // 00
                // 10 - Length (16 bytes)
                if (messages[i].StartsWith("收：00 FA 30 00 10"))
                {
                    basicInfoIndex = i;
                }
            }

            if (basicInfoIndex == -1)
            {
                return;
            }

            string[] elems = messages[basicInfoIndex].Split('：'); // This is not the normal colon character!

            if (elems.Length != 2)
            {
                return;
            }

            elems = elems[1].Split();

            if (elems.Length < 23)
            {
                return;
            }

            output.VoltageInput = ParseUInt16(elems, 5);
            output.VoltageOutputMax = ParseUInt16(elems, 11);
            output.VoltageUsb5V = ParseUInt16(elems, 17);
            output.OutputMode = (PowerSupplyOutputMode)Convert.ToByte(elems[19], 16);
            output.FaultStatus = (PowerSupplyFaultStatus)Convert.ToByte(elems[20], 16);
        }

        public bool RefreshActualOutput()
        {
            bool result;

            lock (ApiInstance)
            {
                apiMutex.WaitOne();
                NullStdOutput();
                result = ApiInstance.GetBasicInfo();
                Monitor.Wait(ApiInstance);
                apiMutex.ReleaseMutex();
                RestoreStdOutput();
            }

            return result;
        }

        public bool RefreshOutputParams()
        {
            bool result;
            byte index = 0;
            byte state = 0;
            ushort vset = 0;
            ushort iset = 0;
            ushort ovp = 0;
            ushort ocp = 0;

            apiMutex.WaitOne();
            NullStdOutput();
            result = ApiInstance.GetCurrentBasic(ref index, ref state, ref vset, ref iset, ref ovp, ref ocp);
            apiMutex.ReleaseMutex();
            RestoreStdOutput();

            if (result)
            {
                Output.Preset = index;
                Output.On = (state != 0);
                Output.Setpoint.Voltage = vset;
                Output.Setpoint.Current = iset;
                Output.Setpoint.OVP = ovp;
                Output.Setpoint.OCP = ocp;

                outputParamsValid = true;
            }

            return result;
        }

        public bool RefreshSystemParams()
        {
            bool result;
            byte blk = 0; // backlight level value 0-4
            byte vol = 0; // Volume level ranges from 0-4
            ushort opp = 0; // unit 0.1W
            ushort otp = 0;

            apiMutex.WaitOne();
            NullStdOutput();
            result = ApiInstance.GetSysPar(ref blk, ref vol, ref opp, ref otp);//, ref en_rep, ref en_auto_out);
            apiMutex.ReleaseMutex();
            RestoreStdOutput();

            if (result)
            {
                SystemParams.Backlight = blk;
                SystemParams.Volume = vol;
                SystemParams.OPP = opp;
                SystemParams.OTP = otp;
                ParseSystemData(SystemParams);
            }

            return result;
        }

        public bool RefreshPreset(byte index)
        {
            bool result = false;
            ushort vset = 0;
            ushort iset = 0;
            ushort ovp = 0;
            ushort ocp = 0;

            if (index >= PresetParams.Length)
            {
                return result;
            }

            apiMutex.WaitOne();
            NullStdOutput();
            result = ApiInstance.GetGroupInfo(index, ref iset, ref vset, ref ocp, ref ovp);
            apiMutex.ReleaseMutex();
            RestoreStdOutput();

            if (result)
            {
                if (PresetParams[index] == null)
                {
                    PresetParams[index] = new PowerSupplySetpoint();
                }

                PresetParams[index].Voltage = vset;
                PresetParams[index].Current = iset;
                PresetParams[index].OVP = ovp;
                PresetParams[index].OCP = ocp;
                presetParamsValid[index] = true;
            }

            return result;
        }

        public bool RefreshDevInfo()
        {
            bool result;
            string devtype = string.Empty;
            string hdwVer = string.Empty;
            string appver = string.Empty;
            string devsn = string.Empty;
            string devState = string.Empty;

            apiMutex.WaitOne();
            NullStdOutput();
            result = ApiInstance.GetDevInfo(ref devtype, ref hdwVer, ref appver, ref devsn, ref devState);
            apiMutex.ReleaseMutex();
            RestoreStdOutput();

            if (result)
            {
                var nullTerm = devtype.IndexOf('\0');
                if (nullTerm > 0)
                {
                    devtype = devtype.Substring(0, nullTerm);
                }

                Device.Type = devtype;
                Device.HardwareVersion = hdwVer;
                Device.SoftwareVersion = appver;
                Device.SerialNumber = devsn;
                Device.SoftwareState = devState;
                infoValid = true;
            }

            return result;
        }

        #endregion Refresh Operations

        #region Print Operations

        public bool PrintPreset(byte index)
        {
            if (!ApiInstance.ConnState)
            {
                Console.WriteLine("ERROR: Not connected!");
                return false;
            }

            if (presetParamsValid[index] || RefreshPreset(index))
            {
                Console.WriteLine("[ PRESET " + index + " ]");
                Console.WriteLine("  V (mV)   : " + PresetParams[index].Voltage);
                Console.WriteLine("  I (mA)   : " + PresetParams[index].Current);
                Console.WriteLine("  OVP (mV) : " + PresetParams[index].OVP);
                Console.WriteLine("  OCP (mA) : " + PresetParams[index].OCP);
            }
            else
            {
                Console.WriteLine("ERROR: Could not get preset[" + index + "] information!");
            }

            return true;
        }

        public bool PrintOutputParams()
        {
            if (!ApiInstance.ConnState)
            {
                Console.WriteLine("ERROR: Not connected!");
                return false;
            }

            if (outputParamsValid || RefreshOutputParams())
            {
                Console.WriteLine("[ OUT_PARAMS ]");
                Console.WriteLine("  State          : " + (Output.On ? "ON" : "OFF"));
                Console.WriteLine("  Preset         : " + Output.Preset);
                Console.WriteLine("  Setpoint");
                Console.WriteLine("    Voltage (mV) : " + Output.Setpoint.Voltage);
                Console.WriteLine("    Current (mA) : " + Output.Setpoint.Current);
                Console.WriteLine("    OVP (mV)     : " + Output.Setpoint.OVP);
                Console.WriteLine("    OCP (mA)     : " + Output.Setpoint.OCP);
            }
            else
            {
                Console.WriteLine("ERROR: Could not get output parameters!");
            }

            return true;
        }

        public bool PrintActualOutput()
        {
            if (!ApiInstance.ConnState)
            {
                Console.WriteLine("ERROR: Not connected!");
                return false;
            }

            if (outputParamsValid || RefreshOutputParams())
            {
                Console.WriteLine("[ ACTUAL_OUT ]");
                Console.WriteLine($"  Time (ns)    : {ActualOutput.Timestamp.Ticks:X08}");
                Console.WriteLine($"  Voltage (mV) : {ActualOutput.Voltage}");
                Console.WriteLine($"  Current (mA) : {ActualOutput.Current}");
            }
            else
            {
                Console.WriteLine("ERROR: Could not get output parameters!");
            }

            return true;
        }

        public bool PrintDevInfo()
        {
            if (!ApiInstance.ConnState)
            {
                Console.WriteLine("ERROR: Not connected!");
                return false;
            }

            if (infoValid || RefreshDevInfo())
            {
                Console.WriteLine("[ DEV_INFO ] ");
                Console.WriteLine("  Type   : " + Device.Type);
                Console.WriteLine("  Status : " + Device.SoftwareState);
                Console.WriteLine("  SN     : " + Device.SerialNumber);
                Console.WriteLine("  HW     : " + Device.HardwareVersion);
                Console.WriteLine("  SW     : " + Device.SoftwareVersion);
            }
            else
            {
                Console.WriteLine("ERROR: Could not get device information!");
            }

            return true;
        }

        public bool PrintSystemParams()
        {
            if (!ApiInstance.ConnState)
            {
                Console.WriteLine("ERROR: Not connected!");
                return false;
            }

            if (systemParamsValid || RefreshSystemParams())
            {
                Console.WriteLine("[ SYS_PARAMS ] ");
                Console.WriteLine("  OTP (C)   : " + SystemParams.OTP);
                Console.WriteLine("  OPP (W)   : " + SystemParams.OPP / 10);
                Console.WriteLine("  Backlight : " + SystemParams.Backlight);
                Console.WriteLine("  Volume    : " + SystemParams.Volume);
                Console.WriteLine("  RPP       : " + (SystemParams.RPP ? "ON" : "OFF"));
                Console.WriteLine("  AUTO-ON   : " + (SystemParams.AutoOn ? "ON" : "OFF"));
            }
            else
            {
                Console.WriteLine("ERROR: Could not get system parameters!");
            }

            return true;
        }

        #endregion Print Operations

        #region Private methods

        private void NullStdOutput()
        {
            if (DebugMode)
            {
                return;
            }

            disableOutputLevel++;
            stdoutReceiver.Flush();
            Console.SetOut(stdoutReceiver);
        }

        private void RestoreStdOutput()
        {
            if (DebugMode)
            {
                return;
            }

            if (disableOutputLevel > 0)
            {
                disableOutputLevel--;
            }

            if (disableOutputLevel == 0)
            {
                var standardOutput = new StreamWriter(Console.OpenStandardOutput());
                standardOutput.AutoFlush = true;
                Console.SetOut(standardOutput);
            }
        }

        #endregion Private methods

        #region Overloads

        public int CompareTo(object obj)
        {
            var otherPsu = (PowerSupply)obj;
            return string.Compare(Device.SerialNumber, otherPsu.Device.SerialNumber);
        }

        #endregion
    }
}
