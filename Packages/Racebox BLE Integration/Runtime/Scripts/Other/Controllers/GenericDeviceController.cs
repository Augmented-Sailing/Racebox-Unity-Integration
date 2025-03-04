using System;

namespace RaceboxIntegration.Other
{
    /// <summary>
    /// Generic implementation of IDeviceController for devices that don't have a specific controller.
    /// Throws NotImplementedException as this is a placeholder for unknown device types.
    /// </summary>
    [Serializable]
    public class GenericDeviceController : IDeviceController
    {
        /// <summary>
        /// Executes the device initialization process.
        /// </summary>
        /// <param name="device">The Bluetooth device to be controlled.</param>
        /// <exception cref="NotImplementedException">Thrown as this is a placeholder implementation.</exception>
        public void Execute(BluetoothDevice device)
        {
            throw new NotImplementedException();
        }
    }
}