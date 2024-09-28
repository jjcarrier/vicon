namespace LibDP100
{
    public class PowerSupplyOutput
    {
        // Indicates whether the output is ON or OFF. NOTE: DP100 API used seemingly backwards terminology (close = OFF; open = ON).
        public bool On { get; set; } = false;

        // The output index (group number). Value range 0-9 Number. 0 is read-only.
        public byte Preset { get; set; } = 0;

        // The active setpoint and limits of the power supply.
        public PowerSupplySetpoint Setpoint { get; set; } = new PowerSupplySetpoint();
    }
}
