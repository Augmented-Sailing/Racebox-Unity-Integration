namespace RaceboxIntegration.Other
{
    /// <summary>
    ///     Factory class responsible for creating appropriate device controllers based on device type.
    ///     Implements the Factory pattern to handle device-specific controller instantiation.
    /// </summary>
    public class ControllerFactory
    {
        /// <summary>
        ///     Creates a device controller based on the device name.
        /// </summary>
        /// <param name="deviceName">The name of the device to create a controller for.</param>
        /// <returns>
        ///     An IDeviceController implementation specific to the device type, or a GenericDeviceController if the device
        ///     type is unknown.
        /// </returns>
        public static IDeviceController CreateController(string deviceName)
        {
            if (deviceName.ToLowerInvariant().Contains("racebox")) return new RaceboxDeviceController();
            return new GenericDeviceController();
        }
    }
}