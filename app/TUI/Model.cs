namespace PowerSupplyApp
{
    internal partial class Program
    {
        private const ushort voltageOutputMax = 30500;
        private const ushort voltageOutputMin = 0;
        private const ushort currentOutputMax = 5050;
        private const ushort currentOutputMin = 0;
        private const ushort powerOutputMax = 1050; // 1050 * 0.1 = 105W
        private const ushort powerOutputMin = 0;
        private const ushort tempOutputMax = 80; // 50 = 50C
        private const ushort tempOutputMin = 0;

        private static int VoltageOutput
        {
            get
            {
                return sp.Voltage;
            }
            set
            {
                if (value > voltageOutputMax)
                {
                    value = voltageOutputMax;
                }
                else if (value < voltageOutputMin)
                {
                    value = voltageOutputMin;
                }
                sp.Voltage = (ushort)value;
            }
        }

        private static int CurrentOutput
        {
            get
            {
                return sp.Current;
            }
            set
            {
                if (value > currentOutputMax)
                {
                    value = currentOutputMax;
                }
                else if (value < currentOutputMin)
                {
                    value = currentOutputMin;
                }
                sp.Current = (ushort)value;
            }
        }

        private static int OVPOutput
        {
            get
            {
                return sp.OVP;
            }
            set
            {
                if (value > voltageOutputMax)
                {
                    value = voltageOutputMax;
                }
                else if (value < voltageOutputMin)
                {
                    value = voltageOutputMin;
                }
                sp.OVP = (ushort)value;
            }
        }

        private static int OCPOutput
        {
            get
            {
                return sp.OCP;
            }
            set
            {
                if (value > currentOutputMax)
                {
                    value = currentOutputMax;
                }
                else if (value < currentOutputMin)
                {
                    value = currentOutputMin;
                }
                sp.OCP = (ushort)value;
            }
        }

        private static int OPPOutput
        {
            get
            {
                return sys.OPP;
            }
            set
            {
                if (value > powerOutputMax)
                {
                    value = powerOutputMax;
                }
                else if (value < powerOutputMin)
                {
                    value = powerOutputMin;
                }
                sys.OPP = (ushort)value;
            }
        }

        private static int OTPOutput
        {
            get
            {
                return sys.OTP;
            }
            set
            {
                if (value > tempOutputMax)
                {
                    value = tempOutputMax;
                }
                else if (value < tempOutputMin)
                {
                    value = tempOutputMin;
                }
                sys.OTP = (ushort)value;
            }
        }
    }
}
