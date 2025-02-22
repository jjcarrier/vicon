namespace LibDP100
{
    internal enum BasicSetSubOpCode
    {
        GetGroupInfo = 0x00,
        SetCurrentBasic = 0x20,
        SaveGroup = 0x60,
        GetCurrentBasic = 0x80,
        UseGroup = 0xA0
    };
}
