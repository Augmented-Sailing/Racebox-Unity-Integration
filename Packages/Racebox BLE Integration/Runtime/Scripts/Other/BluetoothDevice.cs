using System;
using System.Collections;
using Android.BLE.Commands;
using RaceboxIntegration.Events;
#if !UNITY_EDITOR
using Android.BLE;
#endif
using UnityEngine;


namespace RaceboxIntegration.Other
{
    /// <summary>
    /// Represents a Bluetooth Low Energy device in the Racebox integration system.
    /// Handles device connection state, communication, and data management.
    /// </summary>
    [Serializable]
    public class BluetoothDevice
    {
        /// <summary>
        /// Gets the display name of the Bluetooth device.
        /// </summary>
        public string DeviceName { get; private set; }

        /// <summary>
        /// Gets the unique identifier of the Bluetooth device.
        /// </summary>
        public string DeviceUID { get; private set; }

        /// <summary>
        /// Gets the device-specific controller that handles communication and data processing.
        /// </summary>
        public IDeviceController DeviceController { get; private set; }
        
        /// <summary>
        /// Gets whether the device is currently in the process of connecting or disconnecting.
        /// </summary>
        public bool IsConnecting { get; private set; }

        /// <summary>
        /// Gets whether the device is currently connected.
        /// </summary>
        public bool IsConnected { get => connectToDeviceCommand != null && connectToDeviceCommand.IsConnected; }

        /// <summary>
        /// Gets whether the device has been initialized with a connection command.
        /// </summary>
        public bool IsDirty { get => connectToDeviceCommand != null; }

        private ConnectToDevice connectToDeviceCommand; 
        private ReadFromCharacteristic readCommand; 
        private byte[] buffer = new byte[512]; // Max packet size per docs (Page 3)
        private int bufferPos = 0;

        /// <summary>
        /// Initializes a new instance of the BluetoothDevice class.
        /// </summary>
        /// <param name="deviceName">The display name of the device.</param>
        /// <param name="deviceUid">The unique identifier of the device.</param>
        public BluetoothDevice(string deviceName, string deviceUid)
        {
            DeviceName = deviceName;
            DeviceUID = deviceUid;
            DeviceController = ControllerFactory.CreateController(deviceName);
        }

        /// <summary>
        /// Gets the current status of the device.
        /// </summary>
        /// <returns>A string describing the current device status.</returns>
        public string GetStatus()
        {
            if (IsConnecting)
            {
                if (IsConnected)
                    return "Disconnecting...";
                return "Connecting...";
            }
            if (IsConnected)
                return "Connected";
            if (IsDirty)
                return "Disconnected";
            return "Found";
        }

        /// <summary>
        /// Determines whether the device can be interacted with.
        /// </summary>
        /// <returns>True if the device is not in the process of connecting or disconnecting.</returns>
        public bool CanInteract()
        {
            return !IsConnecting;
        }

        /// <summary>
        /// Gets the text to display for the device's primary action button.
        /// </summary>
        /// <returns>"Connect" or "Disconnect" based on the current connection state.</returns>
        public string GetActionText()
        {
            return IsConnected ? "Disconnect" : "Connect";
        }
        
        /// <summary>
        /// Initiates the connection process with the device.
        /// </summary>
        /// <returns>An IEnumerator for coroutine-based connection handling.</returns>
        public IEnumerator Connect()
        {
            Debug.Log("Connecting to " + DeviceUID + " [" + DeviceName + "]");
            
            IsConnecting = true;
            MainEventBus.OnDeviceStatusUpdated?.Invoke(DeviceName);

#if UNITY_EDITOR
            yield return new WaitForSeconds(2);
            IsConnecting = false;
#else
            connectToDeviceCommand = new ConnectToDevice(DeviceUID, OnConnected, OnDisconnected, OnServiceDiscovered, OnCharacteristicDiscovered);
            BleManager.Instance.QueueCommand(connectToDeviceCommand);
            while (IsConnecting)
                yield return null;
#endif
            
            MainEventBus.OnDeviceStatusUpdated?.Invoke(DeviceName);
        }

        /// <summary>
        /// Initiates the disconnection process from the device.
        /// </summary>
        /// <returns>An IEnumerator for coroutine-based disconnection handling.</returns>
        public IEnumerator Disconnect()
        {
            Debug.Log("Disconnecting from " + DeviceUID + " [" + DeviceName + "]");
            
            IsConnecting = true;
            MainEventBus.OnDeviceStatusUpdated?.Invoke(DeviceName);

#if UNITY_EDITOR
            yield return new WaitForSeconds(2);
            IsConnecting = false;
#else
            connectToDeviceCommand.Disconnect();
            while (IsConnecting)
                yield return null;
#endif

            MainEventBus.OnDeviceStatusUpdated?.Invoke(DeviceName);
        }

        private void OnConnected(string deviceId)
        {
            IsConnecting = false;
            DeviceController.Execute(this);
            MainEventBus.OnDeviceStatusUpdated?.Invoke(DeviceName);
        }

        private void OnDisconnected(string deviceId)
        {
            IsConnecting = false;
            MainEventBus.OnDeviceStatusUpdated?.Invoke(DeviceName);
        }

        private void OnCharacteristicDiscovered(string deviceaddress, string serviceaddress, string characteristicaddress)
        {
            Debug.Log("Characteristic Discovered: " + characteristicaddress + " [" + deviceaddress + "]");
        }

        private void OnServiceDiscovered(string deviceaddress, string serviceaddress)
        {
            Debug.Log("Service Discovered: " + serviceaddress + " [" + deviceaddress + "]");
        }
    }
}