using RaceboxIntegration.Other;
using UnityEngine;

namespace RaceboxIntegration.UI
{
    /// <summary>
    ///     Main UI controller for the Racebox integration system.
    ///     Manages the overall UI state and navigation between different UI components.
    /// </summary>
    public class BluetoothUI : MonoBehaviour
    {
        [SerializeField] private ConnectionComponent connectionComponent;
        [SerializeField] private OutputComponent outputComponent;
        [SerializeField] private DevicePopup devicePopup;
        [SerializeField] private InitializePopup initializePopup;

        /// <summary>
        ///     Gets the singleton instance of the BluetoothUI.
        /// </summary>
        public static BluetoothUI Instance { get; private set; }

        /// <summary>
        ///     Gets the device popup component for displaying device details.
        /// </summary>
        public DevicePopup DevicePopup => devicePopup;

        /// <summary>
        ///     Gets the initialization popup component for BLE system status.
        /// </summary>
        public InitializePopup InitializePopup => initializePopup;

        private void Awake()
        {
            Instance = this;

            connectionComponent.gameObject.SetActive(true);
            outputComponent.gameObject.SetActive(false);
            devicePopup.gameObject.SetActive(false);
        }

        /// <summary>
        ///     Shows the device output view for a specific device.
        /// </summary>
        /// <param name="device">The Bluetooth device to display data for.</param>
        public void ShowDevice(BluetoothDevice device)
        {
            outputComponent.Initialize(device);

            connectionComponent.gameObject.SetActive(false);
            outputComponent.gameObject.SetActive(true);
        }

        /// <summary>
        ///     Returns to the device connection view.
        /// </summary>
        public void ReturnToConnection()
        {
            connectionComponent.gameObject.SetActive(true);
            outputComponent.gameObject.SetActive(false);
        }
    }
}