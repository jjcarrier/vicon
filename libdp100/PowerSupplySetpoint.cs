using System.Text.Json.Serialization;

namespace LibDP100
{
    /// <summary>
    /// The power supply setpoint. Provides data related to the output state and presets stored on the device.
    /// </summary>
    public class PowerSupplySetpoint
    {
        /// <summary>
        /// Output voltage. Unit: mV
        /// </summary>
        [JsonPropertyName("voltage")]
        public ushort Voltage { get; set; } = 0;

        /// <summary>
        /// Output current. Unit: mA
        /// </summary>
        [JsonPropertyName("current")]
        public ushort Current { get; set; } = 0;

        /// <summary>
        /// Over voltage protection. Unit: mV.
        /// </summary>
        [JsonPropertyName("ovp")]
        public ushort OVP { get; set; } = 0;

        /// <summary>
        /// Over current protection. Unit: mA.
        /// </summary>
        [JsonPropertyName("ocp")]
        public ushort OCP { get; set; } = 0;

        /// <summary>
        /// The index associated with the preset.
        /// </summary>
        private byte Index { get; set; } = 0;

        /// <summary>
        /// Default constructor. Not to be used except for deserialization.
        /// </summary>
        public PowerSupplySetpoint() { }

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="index">
        /// The preset index related to this instance.
        /// The preset is only applicable for referencing stored presets, it has not meaning for the current output setpoint.
        /// </param>
        public PowerSupplySetpoint(byte index)
        {
            Index = index;
        }

        /// <summary>
        /// Construct the setpoint as a copy of another.
        /// </summary>
        /// <param name="sp">The setpoint to copy data from.</param>
        public PowerSupplySetpoint(PowerSupplySetpoint sp)
        {
            Index = sp.Index;
            Copy(sp);
        }

        /// <summary>
        /// Constructs an instance using the supplied parameters.
        /// </summary>
        /// <param name="voltage">The voltage in millivolts.</param>
        /// <param name="current">The current in milliamps.</param>
        /// <param name="ovp">The OVP in millivolts.</param>
        /// <param name="ocp">The OCP in milliamps.</param>
        public PowerSupplySetpoint(ushort voltage, ushort current, ushort ovp, ushort ocp)
        {
            Voltage = voltage;
            Current = current;
            OVP = ovp;
            OCP = ocp;
        }

        /// <summary>
        /// Gets the index of the preset.
        /// </summary>
        /// <returns>The index.</returns>
        public byte GetIndex()
        {
            return Index;
        }

        /// <summary>
        /// Sets the index of the preset.
        /// This is primarily intended for deserialization logic.
        /// </summary>
        /// <param name="index">The index to set.</param>
        public void SetIndex(byte index)
        {
            Index = index;
        }

        /// <summary>
        /// Make a copy of the provided setpoint object. The <see cref="Index"/> is not modified.
        /// </summary>
        /// <param name="sp">The setpoint to copy data from.</param>
        public void Copy(PowerSupplySetpoint sp)
        {
            Voltage = sp.Voltage;
            Current = sp.Current;
            OVP = sp.OVP;
            OCP = sp.OCP;
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            if (obj == null || obj is not PowerSupplySetpoint setpoint)
                return false;
            else
                return Voltage == setpoint.Voltage &&
                       Current == setpoint.Current &&
                       OVP == setpoint.OVP &&
                       OCP == setpoint.OCP;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            int hashCode = -1467819804;
            hashCode = hashCode * -1521134295 + Voltage.GetHashCode();
            hashCode = hashCode * -1521134295 + Current.GetHashCode();
            hashCode = hashCode * -1521134295 + OVP.GetHashCode();
            hashCode = hashCode * -1521134295 + OCP.GetHashCode();
            return hashCode;
        }

        /// <summary>
        /// Parses/deserializes the HID response data, updating the related
        /// object members.
        /// </summary>
        /// <param name="response">The data to parse.</param>
        /// <returns>
        /// <see cref="true"/> when parsed successfully.
        /// <see cref="false"/> when parsing failed.
        /// </returns>
        public bool Parse(byte[] response)
        {
            const int expectedNumBytes = 10;
            const int responseLenIndex = 4;
            const int responseDataIndex = 5;

            if (response[responseLenIndex] != expectedNumBytes)
            {
                return false;
            }

            Voltage = BitConverter.ToUInt16(response, responseDataIndex + 2);
            Current = BitConverter.ToUInt16(response, responseDataIndex + 4);
            OVP = BitConverter.ToUInt16(response, responseDataIndex + 6);
            OCP = BitConverter.ToUInt16(response, responseDataIndex + 8);

            return true;
        }

        /// <summary>
        /// Prints the object members to standard output.
        /// </summary>
        public void Print()
        {
            Console.WriteLine("[ PRESET " + Index + " ]");
            Console.WriteLine("  Voltage (mV) : " + Voltage);
            Console.WriteLine("  Current (mA) : " + Current);
            Console.WriteLine("  OVP (mV)     : " + OVP);
            Console.WriteLine("  OCP (mA)     : " + OCP);
            Console.WriteLine();
        }
    }
}
