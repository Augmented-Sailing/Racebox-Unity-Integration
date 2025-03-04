using UnityEngine;

namespace RaceboxIntegration.Other
{
    /// <summary>
    /// Interface defining the contract for device controllers in the Racebox integration system.
    /// Provides a standardized way to handle device-specific communication and data processing.
    /// </summary>
    public interface IDeviceController
    {
        /// <summary>
        /// Executes the device-specific initialization and setup process.
        /// </summary>
        /// <param name="device">The Bluetooth device to be controlled.</param>
        public void Execute(BluetoothDevice device);
    }
}