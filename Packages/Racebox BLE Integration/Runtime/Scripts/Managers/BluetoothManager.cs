using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Android.BLE;
using Android.BLE.Commands;
using RaceboxIntegration.Events;
using RaceboxIntegration.Other;
using Random = UnityEngine.Random;

namespace RaceboxIntegration.Managers
{
    /// <summary>
    /// Central manager for Bluetooth Low Energy operations in the Racebox integration system.
    /// Handles device scanning, connection management, and device tracking.
    /// </summary>
    public class BluetoothManager : MonoBehaviour
    {
        /// <summary>
        /// Gets the singleton instance of the BluetoothManager.
        /// </summary>
        public static BluetoothManager Instance { get; private set; }
        
        /// <summary>
        /// Gets whether the manager is currently scanning for devices.
        /// </summary>
        public bool IsScanning { get; private set; }

        /// <summary>
        /// Gets the current progress of the device scanning operation (0-1).
        /// </summary>
        public float ScanProgress {get => _scanTimer / _scanTime; }

        [SerializeField] private int _scanTime = 10;
        [SerializeField] private List<BluetoothDevice> bluetoothDevices = new List<BluetoothDevice>();

        private float _scanTimer = 0f;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            BleManager.Instance.Initialize();
        }

        private void Update()
        {
            // Check for scanning and update the timer
            if(IsScanning)
            {
                if(_scanTimer < _scanTime)
                {
                    _scanTimer += Time.deltaTime;
                }
            }
        }

        /// <summary>
        /// Initiates a scan for nearby Bluetooth devices.
        /// </summary>
        public void ScanForDevices()
        {
            if (!IsScanning)
            {
                IsScanning = true;
                _scanTimer = 0f;
                BleManager.Instance.QueueCommand(new DiscoverDevices(OnDeviceFound, OnFinishedScan, _scanTime * 1000));
            }
            
#if UNITY_EDITOR
            StartCoroutine(SimulateDevices());
#endif
        }

        /// <summary>
        /// Simulates device discovery in the Unity Editor for testing purposes.
        /// </summary>
        private IEnumerator SimulateDevices()
        {
            while (IsScanning)
            {
                yield return new WaitForSeconds(Random.Range(.25f, 2f));
                OnDeviceFound("Device " + Random.Range(0, 100), "DeviceID" + Random.Range(0, 100));
            }
        }

        /// <summary>
        /// Retrieves a device by its unique identifier.
        /// </summary>
        /// <param name="deviceUid">The unique identifier of the device to find.</param>
        /// <returns>The BluetoothDevice instance if found, null otherwise.</returns>
        public BluetoothDevice GetDevice(string deviceUid)
        {
            return bluetoothDevices.FirstOrDefault(d => d.DeviceName == deviceUid);
        }
        
        /// <summary>
        /// Gets the list of all discovered devices.
        /// </summary>
        /// <returns>A list of all BluetoothDevice instances.</returns>
        public List<BluetoothDevice> GetDeviceList()
        {
            return bluetoothDevices;
        }

        /// <summary>
        /// Initiates connection to a specific device.
        /// </summary>
        /// <param name="device">The device to connect to.</param>
        public void Connect(BluetoothDevice device)
        {
            if (device.IsConnected)
            {
                Debug.LogError("Already connected while attempting to connect");
                return;
            }
            StartCoroutine(device.Connect());
        }

        /// <summary>
        /// Initiates disconnection from a specific device.
        /// </summary>
        /// <param name="device">The device to disconnect from.</param>
        public void Disconnect(BluetoothDevice device)
        {
            if (device.IsConnected == false)
            {
                Debug.LogError("Already disconnected while attempting to disconnect");
                return;
            }
            StartCoroutine(device.Disconnect());
        }

        private void OnDeviceFound(string uid, string deviceName)
        {
            if(string.IsNullOrWhiteSpace(deviceName))
                deviceName = "Unknown Device";
            
            BluetoothDevice newDevice = new BluetoothDevice(deviceName, uid);
            
            bluetoothDevices.Add(newDevice);
            
            MainEventBus.OnDeviceFound?.Invoke(deviceName);
        }
        
        private void OnFinishedScan()
        {
            Debug.Log("Finished scanning for devices");
            IsScanning = false;
        }
    }
}
