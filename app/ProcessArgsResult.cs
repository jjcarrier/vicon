namespace PowerSupplyApp
{
    public enum ProcessArgsResult
    {
        OkExitNow = -1,
        Ok = 0,
        Error,
        ReadError,
        WriteError,
        NotImplemented,
        MissingParameter,
        InvalidParameter,
        UnsupportedOption
    }
}
