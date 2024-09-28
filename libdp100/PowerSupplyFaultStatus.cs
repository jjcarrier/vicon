namespace LibDP100
{
    // Derived from DP100's "Work_St".
    // Note: ATK-DP100's Official GUI v1.0.10.0 has OPP and OTP backwards!
    public enum PowerSupplyFaultStatus
    {
        OK = 0, // Normal Status
        OVP = 1, // Over-Voltage Protection.
        OCP = 2, // Over-Current Protection.
        OPP = 3, // Over-Power Protection.
        OTP = 4, // Over-Temperature Protection.
        REP = 5, // Reverse-Polarity Protection.
        UVP = 6, // Likely indicates USB-C power is not present (only powered via USB-A).
        Invalid = 255
    }
}
