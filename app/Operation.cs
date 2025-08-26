namespace PowerSupplyApp
{
    public enum Operation
    {
        None,
        Delay,
        ReadOutput,
        ReadActState,
        ReadSystem,
        ReadDevice,
        ReadPreset,
        WriteOutputOn,
        WriteOutputOff,
        UsePreset,
        RecallPreset,
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
