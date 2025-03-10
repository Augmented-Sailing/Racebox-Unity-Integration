using UnityEngine;
using UnityEngine.Events;

namespace RaceboxIntegration.Events
{
    /// <summary>
    /// Central event bus for the Racebox integration system.
    /// Provides a unified communication channel for system-wide events.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public static class MainEventBus
    {
        /// <summary>
        /// Event triggered when the BLE system is initialized and ready.
        /// </summary>
        public static UnityEvent OnBLEInitialized;

        /// <summary>
        /// Event triggered when a new Bluetooth device is discovered.
        /// </summary>
        public static UnityEvent<string> OnDeviceFound;

        /// <summary>
        /// Event triggered when a device's connection status changes.
        /// </summary>
        public static UnityEvent<string> OnDeviceRefreshed;

        /// <summary>
        /// Event triggered when a device's connection status changes.
        /// </summary>
        public static UnityEvent<string> OnDeviceConnectionUpdated;

        /// <summary>
        /// Event triggered when new data is received from a device.
        /// </summary>
        public static UnityEvent<string> OnDeviceUpdated;
        
        /// <summary>
        /// Resets all events when the subsystem is registered.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetEvents ()
        {
            OnBLEInitialized = new UnityEvent();
            OnDeviceFound = new UnityEvent<string>();
            OnDeviceRefreshed = new UnityEvent<string>();
            OnDeviceConnectionUpdated = new UnityEvent<string>();
            OnDeviceUpdated = new UnityEvent<string>();
        }
    }
}
