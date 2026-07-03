using LibDP100;

namespace PowerSupplyApp
{
    internal delegate void ActiveStateHandler(PowerSupplyActiveState activeState);

    internal delegate void DisconnectedHandler();

    internal interface IPowerSupplyBackend
    {
        bool Connected { get; }

        PowerSupplyInfo Device { get; }

        PowerSupplyActiveState ActiveState { get; }

        PowerSupplyOutput Output { get; }

        PowerSupplySetpoint[] Presets { get; }

        PowerSupplySystemParams SystemParams { get; }

        ActiveStateHandler? ActiveStateEvent { get; set; }

        DisconnectedHandler? DisconnectedEvent { get; set; }

        bool DebugMode { get; set; }

        PowerSupplyResult Connect();

        PowerSupplyResult Connect(string serialNumber);

        void Disconnect();

        void StartWorkerThread(TimeSpan sleepTime);

        void StopWorkerThread();

        PowerSupplyResult SetOutputOn();

        PowerSupplyResult SetOutputOff();

        PowerSupplyResult ToggleOutput();

        PowerSupplyResult SetOutputVoltage(ushort millivolts);

        PowerSupplyResult SetOutputCurrent(ushort milliamps);

        PowerSupplyResult SetOutput(PowerSupplySetpoint setpoint);

        PowerSupplyResult SetOutput(bool outputOn, ushort millivolts, ushort milliamps);

        PowerSupplyResult SetBacklight(byte brightness);

        PowerSupplyResult SetVolume(byte volume);

        PowerSupplyResult SetAutoOn(bool enable);

        PowerSupplyResult SetRPP(bool enable);

        PowerSupplyResult SetOPP(ushort deciWatts);

        PowerSupplyResult SetOTP(ushort celsius);

        PowerSupplyResult SetSystemParams(PowerSupplySystemParams systemParams);

        PowerSupplyResult SetSystemParams(ushort otp, ushort opp, byte backlight, byte volume, bool rpp, bool autoOn);

        PowerSupplyResult SetPresetOVP(byte preset, ushort ovp);

        PowerSupplyResult SetPresetOCP(byte preset, ushort ocp);

        PowerSupplyResult SetPreset(byte preset, PowerSupplySetpoint setpoint);

        PowerSupplyResult SetPreset(byte preset, ushort millivolts, ushort milliamps, ushort ovp, ushort ocp);

        PowerSupplyResult UsePreset(byte preset, bool fromNonVolatile);

        PowerSupplyResult GetDeviceInfo();

        PowerSupplyResult GetSystemParams();

        PowerSupplyResult GetActiveStatus();

        PowerSupplyResult GetOutput();

        PowerSupplyResult GetPreset(byte preset);

        PowerSupplyResult Reload();

        void SignalRunWorker();
    }
}
