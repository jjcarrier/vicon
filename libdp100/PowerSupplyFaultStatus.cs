namespace LibDP100
{
    /// <summary>
    /// The fault status information.
    /// </summary>
    /// <remarks>
    /// Derived from DP100's "Work_St".
    /// Note: ATK-DP100's Official GUI v1.0.10.0 has OPP and OTP backwards!
    /// </remarks>
    public enum PowerSupplyFaultStatus
    {
        /// <summary>
        /// Normal Status.
        /// </summary>
        OK = 0,

        /// <summary>
        /// Over-Voltage Protection.
        /// </summary>
        OVP = 1,

        /// <summary>
        /// Over-Current Protection.
        /// </summary>
        OCP = 2,

        /// <summary>
        /// Over-Power Protection.
        /// </summary>
        OPP = 3,

        /// <summary>
        /// Over-Temperature Protection.
        /// </summary>
        OTP = 4,

        /// <summary>
        /// Reverse-Polarity Protection.
        /// </summary>
        REP = 5,

        /// <summary>
        /// Under-Voltage Protection. Likely indicates USB-C power is not present (only powered via USB-A).
        /// </summary>
        UVP = 6,

        /// <summary>
        /// Invalid/unknown Fault Status.
        /// </summary>
        Invalid = 255
    }
}
