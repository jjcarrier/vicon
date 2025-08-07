namespace LibDP100
{
    internal enum OperationCode
    {
        None = 0x00,
        DeviceInfo = 0x10,
        FirmwareInfo = 0x11,
        StartTransfer = 0x12,
        DataTransfer = 0x13,
        EndTransfer = 0x14,
        DeviceUpgrade = 0x15,
        BasicInfo = 0x30,
        BasicSet = 0x35,
        SystemInfo = 0x40,
        SystemSet = 0x45,
        ScanOut = 0x50,
        SerialOut = 0x55,
        Disconnect = 0x80
    };
}
