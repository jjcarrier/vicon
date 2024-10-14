namespace PowerSupplyApp
{
    public enum Operation
    {
        None,
        ReadOutput,
        ReadActuals,
        ReadSystem,
        ReadDevice,
        ReadPreset,
        WriteOutputOn,
        WriteOutputOff,
        WritePreset,
        WriteVoltage,
        WriteCurrent,
        WriteSetpoint,
        WriteOVP,
        WriteOCP,
        WriteOTP,
        WriteOPP,
        // TODO WriteAWG
    };
}
