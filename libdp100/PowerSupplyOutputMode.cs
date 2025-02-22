namespace LibDP100
{
    /// <summary>
    /// The power supply output mode.
    /// </summary>
    public enum PowerSupplyOutputMode
    {
        /// <summary>
        /// Constant Current.
        /// </summary>
        CC = 0,

        /// <summary>
        /// Constant Voltage.
        /// </summary>
        CV = 1,

        /// <summary>
        /// Output Off.
        /// </summary>
        OFF = 2,

        /// <summary>
        /// No Input Present (Output not possible).
        /// </summary>
        NoInput = 130,

        /// <summary>
        /// Invalid/unsupported Output Mode.
        /// </summary>
        Invalid = 255
    }
}
