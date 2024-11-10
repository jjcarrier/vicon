using LibDP100;
using System.Collections.Generic;

namespace PowerSupplyApp
{
    // Assists in connecting to a specific PowerSupply by offering
    // enumeration mechanisms.
    public static class Enumerator
    {
        static List<PowerSupply> supplies = new List<PowerSupply>();


        /// <summary>
        /// Obtain list of device instances which may be used to present connection
        /// options to a user. These devices are sorted according to their serial
        /// number in order to provide a more predictable ordering to the application
        /// and user.
        /// </summary>
        /// <returns>The number of power supplies enumerated.</returns>
        public static int Enumerate()
        {
            while (true)
            {
                PowerSupply psu = new PowerSupply();
                if (!psu.Connect())
                {
                    break;
                }

                if (psu.RefreshDevInfo())
                {
                    supplies.Add(psu);
                }
            }

            supplies.Sort();

            return supplies.Count;
        }

        /// <summary>
        /// Gets the number of enumerated power supplies.
        /// </summary>
        /// <returns>The number of power supplies enumerated.</returns>
        public static int GetDeviceCount()
        {
            return supplies.Count;
        }

        /// <summary>
        /// Get the power supply that matches the specified serial number.
        /// </summary>
        /// <param name="serial">The serial number to search for.</param>
        /// <returns>
        /// The matching PowerSupply instance. If no match found, this method
        /// will return null.
        /// </returns>
        public static PowerSupply GetDeviceBySerial(string serial)
        {
            foreach (var psu in supplies)
            {
                if (psu.Device.SerialNumber == serial)
                {
                    return psu;
                }
            }

            return null;
        }

        /// <summary>
        /// Gets a PowerSupply at the specified index.
        /// </summary>
        /// <param name="index">The index of the PowerSupply.</param>
        /// <returns>The PowerSupply at the specified index.</returns>
        public static PowerSupply GetDeviceByIndex(int index)
        {
            if (index >= supplies.Count)
            {
                return null;
            }

            return supplies[index];
        }

        /// <summary>
        /// Gets a list of the enumerated power supplies' serial numbers.
        /// </summary>
        /// <returns>The list of serial numbers</returns>
        public static List<string> GetSerialNumbers()
        {
            List<string> list = new List<string>();

            foreach (var psu in supplies)
            {
                list.Add(psu.Device.SerialNumber);
            }

            return list;
        }
    }
}
