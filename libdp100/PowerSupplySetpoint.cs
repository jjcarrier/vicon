namespace LibDP100
{
    public class PowerSupplySetpoint
    {
        // Output voltage. Unit: mV
        public ushort Voltage { get; set; } = 0;

        // Output current. Unit: mA
        public ushort Current { get; set; } = 0;

        // Over voltage protection. Unit: mV.
        public ushort OVP { get; set; } = 0;

        // Over current protection. Unit: mA.
        public ushort OCP { get; set; } = 0;

        // Default constructor.
        public PowerSupplySetpoint() { }

        // Construct the setpoint as a copy of another.
        public PowerSupplySetpoint(PowerSupplySetpoint sp)
        {
            Copy(sp);
        }

        public PowerSupplySetpoint(ushort voltage, ushort current, ushort ovp, ushort ocp)
        {
            Voltage = voltage;
            Current = current;
            OVP = ovp;
            OCP = ocp;
        }

        // Make a copy of the provided setpoint object.
        public void Copy(PowerSupplySetpoint sp)
        {
            Voltage = sp.Voltage;
            Current = sp.Current;
            OVP = sp.OVP;
            OCP = sp.OCP;
        }

        public override bool Equals(object obj)
        {
            if (obj == null || !(obj is PowerSupplySetpoint setpoint))
                return false;
            else
                return Voltage == setpoint.Voltage &&
                       Current == setpoint.Current &&
                       OVP == setpoint.OVP &&
                       OCP == setpoint.OCP;
        }

        public override int GetHashCode()
        {
            int hashCode = -1467819804;
            hashCode = hashCode * -1521134295 + Voltage.GetHashCode();
            hashCode = hashCode * -1521134295 + Current.GetHashCode();
            hashCode = hashCode * -1521134295 + OVP.GetHashCode();
            hashCode = hashCode * -1521134295 + OCP.GetHashCode();
            return hashCode;
        }
    }
}
