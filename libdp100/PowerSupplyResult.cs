namespace LibDP100
{
    /// <summary>
    /// The power supply result. Used by the library for status reporting.
    /// </summary>
    public enum PowerSupplyResult
    {
        OK,
        Error,
        Timeout,
        DeviceNotPresent,
        DeviceNotConnected,
        OutOfRange,
        InvalidState,
        InvalidParameter
    };
}
