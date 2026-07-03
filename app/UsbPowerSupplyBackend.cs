using LibDP100;
using UsbPowerSupply = LibDP100.PowerSupply;

namespace PowerSupplyApp
{
    internal sealed class UsbPowerSupplyBackend : IPowerSupplyBackend
    {
        private readonly UsbPowerSupply inner;
        private ActiveStateHandler? activeStateEvent;
        private DisconnectedHandler? disconnectedEvent;

        public UsbPowerSupplyBackend(UsbPowerSupply inner)
        {
            this.inner = inner;
        }

        public bool Connected => inner.Connected;

        public PowerSupplyInfo Device => inner.Device;

        public PowerSupplyActiveState ActiveState => inner.ActiveState;

        public PowerSupplyOutput Output => inner.Output;

        public PowerSupplySetpoint[] Presets => inner.Presets;

        public PowerSupplySystemParams SystemParams => inner.SystemParams;

        public ActiveStateHandler? ActiveStateEvent
        {
            get => activeStateEvent;
            set
            {
                activeStateEvent = value;
                inner.ActiveStateEvent = activeStateEvent == null
                    ? null
                    : state => activeStateEvent(state);
            }
        }

        public DisconnectedHandler? DisconnectedEvent
        {
            get => disconnectedEvent;
            set
            {
                disconnectedEvent = value;
                inner.DisconnectedEvent = disconnectedEvent == null
                    ? null
                    : () => disconnectedEvent();
            }
        }

        public bool DebugMode
        {
            get => inner.DebugMode;
            set => inner.DebugMode = value;
        }

        public PowerSupplyResult Connect() => inner.Connect();

        public PowerSupplyResult Connect(string serialNumber) => inner.Connect(serialNumber);

        public void Disconnect() => inner.Disconnect();

        public void StartWorkerThread(TimeSpan sleepTime) => inner.StartWorkerThread(sleepTime);

        public void StopWorkerThread() => inner.StopWorkerThread();

        public PowerSupplyResult SetOutputOn() => inner.SetOutputOn();

        public PowerSupplyResult SetOutputOff() => inner.SetOutputOff();

        public PowerSupplyResult ToggleOutput() => inner.ToggleOutput();

        public PowerSupplyResult SetOutputVoltage(ushort millivolts) => inner.SetOutputVoltage(millivolts);

        public PowerSupplyResult SetOutputCurrent(ushort milliamps) => inner.SetOutputCurrent(milliamps);

        public PowerSupplyResult SetOutput(PowerSupplySetpoint setpoint) => inner.SetOutput(setpoint);

        public PowerSupplyResult SetOutput(bool outputOn, ushort millivolts, ushort milliamps) => inner.SetOutput(outputOn, millivolts, milliamps);

        public PowerSupplyResult SetBacklight(byte brightness) => inner.SetBacklight(brightness);

        public PowerSupplyResult SetVolume(byte volume) => inner.SetVolume(volume);

        public PowerSupplyResult SetAutoOn(bool enable) => inner.SetAutoOn(enable);

        public PowerSupplyResult SetRPP(bool enable) => inner.SetRPP(enable);

        public PowerSupplyResult SetOPP(ushort deciWatts) => inner.SetOPP(deciWatts);

        public PowerSupplyResult SetOTP(ushort celsius) => inner.SetOTP(celsius);

        public PowerSupplyResult SetSystemParams(PowerSupplySystemParams systemParams) => inner.SetSystemParams(systemParams);

        public PowerSupplyResult SetSystemParams(ushort otp, ushort opp, byte backlight, byte volume, bool rpp, bool autoOn) => inner.SetSystemParams(otp, opp, backlight, volume, rpp, autoOn);

        public PowerSupplyResult SetPresetOVP(byte preset, ushort ovp) => inner.SetPresetOVP(preset, ovp);

        public PowerSupplyResult SetPresetOCP(byte preset, ushort ocp) => inner.SetPresetOCP(preset, ocp);

        public PowerSupplyResult SetPreset(byte preset, PowerSupplySetpoint setpoint) => inner.SetPreset(preset, setpoint);

        public PowerSupplyResult SetPreset(byte preset, ushort millivolts, ushort milliamps, ushort ovp, ushort ocp) => inner.SetPreset(preset, millivolts, milliamps, ovp, ocp);

        public PowerSupplyResult UsePreset(byte preset, bool fromNonVolatile) => inner.UsePreset(preset, fromNonVolatile);

        public PowerSupplyResult GetDeviceInfo() => inner.GetDeviceInfo();

        public PowerSupplyResult GetSystemParams() => inner.GetSystemParams();

        public PowerSupplyResult GetActiveStatus() => inner.GetActiveStatus();

        public PowerSupplyResult GetOutput() => inner.GetOutput();

        public PowerSupplyResult GetPreset(byte preset) => inner.GetPreset(preset);

        public PowerSupplyResult Reload() => inner.Reload();

        public void SignalRunWorker() => inner.SignalRunWorker();
    }
}
