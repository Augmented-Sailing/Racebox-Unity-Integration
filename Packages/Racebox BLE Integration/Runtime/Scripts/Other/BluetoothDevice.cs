using System;
using System.Collections;
using Android.BLE;
using Android.BLE.Commands;
#if UNITY_EDITOR
using UnityEditor;
#endif
using RaceboxIntegration.Events;
using UnityEngine;

namespace RaceboxIntegration.Other
{
    [Serializable]
    public class BluetoothDevice
    {
        public string DeviceName { get; private set; }
        public string DeviceUID { get; private set; }
        public IDeviceController DeviceController { get; private set; }
        public bool IsConnecting { get; private set; }
        public bool IsConnected
        {
            get
            {
#if UNITY_EDITOR
                return EditorPrefs.GetBool(DeviceUID + "_IsConnected", false);
#else
                return connectToDeviceCommand != null && connectToDeviceCommand.IsConnected;
#endif
            }
        }
        public bool IsDirty { get => connectToDeviceCommand != null; }

        private ConnectToDevice connectToDeviceCommand;

        private byte[] buffer = new byte[512];
        private int bufferPos = 0;

        public BluetoothDevice(string deviceName, string deviceUid)
        {
            DeviceName = deviceName;
            DeviceUID = deviceUid;
            DeviceController = ControllerFactory.CreateController(deviceName);
        }

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

        public bool CanInteract()
        {
            return !IsConnecting;
        }

        public string GetActionText()
        {
            return IsConnected ? "Disconnect" : "Connect";
        }

        public IEnumerator Connect()
        {
            Debug.Log("Connecting to " + DeviceUID + " [" + DeviceName + "]");

            IsConnecting = true;
            MainEventBus.OnDeviceRefreshed?.Invoke(DeviceName);

#if UNITY_EDITOR
            yield return new WaitForSeconds(2);
            OnConnected(DeviceUID);
#else
            connectToDeviceCommand = new ConnectToDevice(DeviceUID, OnConnected, OnDisconnected, OnServiceDiscovered, OnCharacteristicDiscovered);
            BleManager.Instance.QueueCommand(connectToDeviceCommand);
            while (IsConnecting)
                yield return null;
#endif

            MainEventBus.OnDeviceRefreshed?.Invoke(DeviceName);
            MainEventBus.OnDeviceConnectionUpdated?.Invoke(DeviceName);
        }

        public IEnumerator Disconnect()
        {
            Debug.Log("Disconnecting from " + DeviceUID + " [" + DeviceName + "]");

            IsConnecting = true;
            MainEventBus.OnDeviceRefreshed?.Invoke(DeviceName);

#if UNITY_EDITOR
            yield return new WaitForSeconds(2);
            OnDisconnected(DeviceUID);
#else
            connectToDeviceCommand.Disconnect();
            while (IsConnecting)
                yield return null;
#endif

            MainEventBus.OnDeviceRefreshed?.Invoke(DeviceName);
            MainEventBus.OnDeviceConnectionUpdated?.Invoke(DeviceName);
        }

        private void OnConnected(string deviceId)
        {
            IsConnecting = false;
            DeviceController.Execute(this);
            MainEventBus.OnDeviceRefreshed?.Invoke(DeviceName);
#if UNITY_EDITOR
            EditorPrefs.SetBool(DeviceUID + "_IsConnected", true);
#endif
        }

        private void OnDisconnected(string deviceId)
        {
            IsConnecting = false;
            MainEventBus.OnDeviceRefreshed?.Invoke(DeviceName);
#if UNITY_EDITOR
            EditorPrefs.SetBool(DeviceUID + "_IsConnected", false);
#endif
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