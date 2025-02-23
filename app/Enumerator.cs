using LibDP100;

namespace PowerSupplyApp
{
    // Assists in connecting to a specific PowerSupply by offering
    // enumeration mechanisms.
    public static class Enumerator
    {
        private class EnumeratedSupply(PowerSupply psu, bool selected) : IComparable
        {
            public PowerSupply Instance { get; set; } = psu;
            public bool Selected { get; set; } = selected;

            /// <inheritdoc/>
            public int CompareTo(object? obj)
            {
                var other = (EnumeratedSupply?)obj;
                return string.Compare(Instance.Device.SerialNumber, other?.Instance.Device.SerialNumber);
            }
        }

        // The enumerated power supply instances.
        // The boolean is used to determine which instances have been requested by the application
        // to determine which instances to close during cleanup so that separate applications
        // may take control of the device.
        private static List<EnumeratedSupply> supplies = new List<EnumeratedSupply>();

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
                if (psu.Connect() != PowerSupplyResult.OK)
                {
                    break;
                }

                if (psu.GetDeviceInfo() == PowerSupplyResult.OK)
                {
                    supplies.Add(new EnumeratedSupply(psu, false));
                }
            }

            supplies.Sort();

            return supplies.Count;
        }

        /// <summary>
        /// Disconnects all instances that were not requested via GetDeviceBySerial
        /// nor GetDeviceByIndex and resets the enumeration.
        /// </summary>
        public static void Done()
        {
            foreach (var psu in supplies)
            {
                if (!psu.Selected)
                {
                    psu.Instance.Disconnect();
                }
            }

            supplies.Clear();
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
        /// <remarks>
        /// Successful requests will mark the instance as "selected" and
        /// it will be the responsibility of the application to close
        /// the instance when no longer needed.
        /// </remarks>
        /// <param name="serial">The serial number to search for.</param>
        /// <returns>
        /// The matching PowerSupply instance. If no match found, this method
        /// will return null.
        /// </returns>
        public static PowerSupply GetDeviceBySerial(string serial)
        {
            foreach (var psu in supplies)
            {
                if (psu.Instance.Device.SerialNumber == serial)
                {
                    psu.Selected = true;
                    return psu.Instance;
                }
            }

            return null;
        }

        /// <summary>
        /// Gets a PowerSupply at the specified index.
        /// </summary>
        /// <remarks>
        /// Successful requests will mark the instance as "selected" and
        /// it will be the responsibility of the application to close
        /// the instance when no longer needed.
        /// </remarks>
        /// <param name="index">The index of the PowerSupply.</param>
        /// <returns>The PowerSupply at the specified index.</returns>
        public static PowerSupply GetDeviceByIndex(int index)
        {
            if (index >= supplies.Count)
            {
                return null;
            }

            supplies[index].Selected = true;
            return supplies[index].Instance;
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
                list.Add(psu.Instance.Device.SerialNumber);
            }

            return list;
        }
    }
}
