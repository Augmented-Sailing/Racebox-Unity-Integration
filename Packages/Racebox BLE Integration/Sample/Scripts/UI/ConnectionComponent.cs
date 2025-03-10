using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using RaceboxIntegration.Events;
using RaceboxIntegration.Managers;
using RaceboxIntegration.Other;
using UnityEngine;
using UnityEngine.UI;

namespace RaceboxIntegration.UI
{
    /// <summary>
    /// UI component that manages the device scanning and connection interface.
    /// Handles device discovery, connection management, and device list display.
    /// </summary>
    public class ConnectionComponent : MonoBehaviour
    {
        [SerializeField] private Transform _deviceList;
        [SerializeField] private GameObject _deviceEntryPrefab;
        [SerializeField] private Button _scanButton;
        [SerializeField] private Slider _progressSlider;

        private List<DeviceEntry> _bluetoothEntries = new List<DeviceEntry>();

        private void Awake()
        {
            ClearEntries();
        }

        private void OnEnable()
        {
            MainEventBus.OnDeviceFound.AddListener(OnDeviceRefreshed);
            MainEventBus.OnDeviceRefreshed.AddListener(OnDeviceRefreshed);

            _scanButton.onClick.RemoveAllListeners();
            _scanButton.onClick.AddListener(ScanForDevices);
            
            // Show the scan button, hide the progress slider
            _scanButton.gameObject.SetActive(true);
            _progressSlider.gameObject.SetActive(false);

            RefreshAllEntries();
        }

        private void OnDisable()
        {
            MainEventBus.OnDeviceFound.RemoveListener(OnDeviceRefreshed);
            MainEventBus.OnDeviceRefreshed.AddListener(OnDeviceRefreshed);
        }

        /// <summary>
        /// Clears all device entries from the list.
        /// </summary>
        private void ClearEntries()
        {
            _bluetoothEntries.Clear();
            foreach (Transform child in _deviceList)
            {
                Destroy(child.gameObject);
            }
        }
        
        /// <summary>
        /// Initiates a scan for nearby Bluetooth devices.
        /// </summary>
        private void ScanForDevices()
        {
            BluetoothManager.Instance.ScanForDevices();
            
            StartCoroutine(UpdateScanProgress());
        }

        /// <summary>
        /// Updates the scan progress UI and handles the scanning state.
        /// </summary>
        private IEnumerator UpdateScanProgress()
        {
            // Hide button and show progress bar
            _scanButton.gameObject.SetActive(false);
            _progressSlider.gameObject.SetActive(true);
            
            // Update slider w/ scan progress
            while (BluetoothManager.Instance.IsScanning)
            {
                _progressSlider.value = BluetoothManager.Instance.ScanProgress;
                yield return null;
            }
            
            _scanButton.gameObject.SetActive(true);
            _progressSlider.gameObject.SetActive(false);
        }
        
        /// <summary>
        /// Refreshes all device entries in the list.
        /// </summary>
        private void RefreshAllEntries()
        {
            List<BluetoothDevice> devices = BluetoothManager.Instance.GetDeviceList();

            Debug.Log("Refreshing all entries with " + devices.Count + " devices");
            
            // Refresh all the device entries
            foreach (var device in devices)
            {
                if (RefreshDevice(device.DeviceUID))
                {
                    continue;
                }
                AddBluetoothEntry(device);
            }
            
            // Check if any spawned entries are not in the device list returned to remove them
            foreach (var bluetoothEntry in _bluetoothEntries)
            {
                if(devices.Exists(x => x.DeviceUID == bluetoothEntry.BluetoothDevice.DeviceUID))
                    continue;
                Destroy(bluetoothEntry.gameObject);
                _bluetoothEntries.Remove(bluetoothEntry);
            }
        }
        
        /// <summary>
        /// Refreshes a specific device entry if it exists.
        /// </summary>
        /// <param name="deviceUID">The unique identifier of the device to refresh.</param>
        /// <returns>True if the device was found and refreshed, false otherwise.</returns>
        private bool RefreshDevice(string deviceUID)
        {
            foreach (var entry in _bluetoothEntries)
            {
                if (entry.BluetoothDevice.DeviceUID == deviceUID)
                {
                    entry.Refresh();
                    return true;                    
                }
            }

            return false;
        }
        
        /// <summary>
        /// Adds a new device entry to the list.
        /// </summary>
        /// <param name="device">The Bluetooth device to add.</param>
        private void AddBluetoothEntry(BluetoothDevice device)
        {
            DeviceEntry entryFound = _bluetoothEntries.FirstOrDefault(x => x.BluetoothDevice.DeviceUID == device.DeviceUID);
            if(entryFound != null)
            {
                entryFound.Refresh();
                return;
            }

            GameObject entryObject = Instantiate(_deviceEntryPrefab, _deviceList);
            DeviceEntry entry = entryObject.GetComponent<DeviceEntry>();
            entry.Init(device, () => OnEntryClicked(device));
            _bluetoothEntries.Add(entry);
        }

        private void OnEntryClicked(BluetoothDevice device)
        {
            Debug.Log("Entry clicked: " + device.DeviceName);
            
            string actionId = "connect";
            BluetoothUI.Instance.DevicePopup.ShowPopup(device);
        }

        private void OnDeviceRefreshed(string deviceUID)
        {
            if (RefreshDevice(deviceUID))
                return;

            AddBluetoothEntry(BluetoothManager.Instance.GetDevice(deviceUID));
        }
    }
}