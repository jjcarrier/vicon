namespace PowerSupplyApp
{
    public enum Operation
    {
        None,
        ReadOutput,
        ReadActState,
        ReadSystem,
        ReadDevice,
        ReadPreset,
        WriteOutputOn,
        WriteOutputOff,
        UsePreset,
        WriteVoltage,
        WriteCurrent,
        WriteSetpoint,
        WriteOVP,
        WriteOCP,
        WriteOTP,
        WriteOPP,
        WriteRPP,
        WriteAutoOn,
        WriteVolume,
        WriteBacklight
    };
}
