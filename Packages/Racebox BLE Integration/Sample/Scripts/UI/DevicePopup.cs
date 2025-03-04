using System;
using RaceboxIntegration.Events;
using RaceboxIntegration.Managers;
using RaceboxIntegration.Other;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RaceboxIntegration.UI
{
    /// <summary>
    /// UI component that displays detailed information about a Bluetooth device.
    /// Provides controls for device connection and data viewing.
    /// </summary>
    public class DevicePopup : MonoBehaviour
    {
        [SerializeField] private TMP_Text deviceNameText;
        [SerializeField] private TMP_Text deviceIDText;
        [SerializeField] private TMP_Text detailText;
        [SerializeField] private Button viewButton;
        [SerializeField] private Button actionButton;
        [SerializeField] private Button exitButton;
        [SerializeField] private TMP_Text actionButtonText;

        private BluetoothDevice device;
        
        private void Awake()
        {
            viewButton.onClick.RemoveAllListeners();
            viewButton.onClick.AddListener(OnViewDevice);
            actionButton.onClick.RemoveAllListeners();
            actionButton.onClick.AddListener(OnClick);
            exitButton.onClick.RemoveAllListeners();
            exitButton.onClick.AddListener(OnExit);
        }

        private void OnEnable()
        {
            MainEventBus.OnDeviceStatusUpdated.AddListener(OnDeviceUpdated);
        }

        private void OnDisable()
        {
            MainEventBus.OnDeviceStatusUpdated.RemoveListener(OnDeviceUpdated);
        }

        /// <summary>
        /// Displays the device popup with information about a specific device.
        /// </summary>
        /// <param name="device">The Bluetooth device to display information for.</param>
        public void ShowPopup(BluetoothDevice device)
        {
            this.device = device;
            
            Refresh();

            gameObject.SetActive(true);
        }

        private void Refresh()
        {
            deviceNameText.SetText(device.DeviceUID);
            deviceIDText.SetText(device.DeviceName);
            detailText.SetText(device.GetStatus());

            actionButton.interactable = device.CanInteract();
            actionButtonText.SetText(this.device.GetActionText());

            viewButton.interactable = device.IsConnected;
        }

        private void OnDeviceUpdated(string deviceId)
        {
            if (deviceId == device.DeviceName)
            {
                Refresh();
            }
        }

        private void OnViewDevice()
        {
            OnExit();
            BluetoothUI.Instance.ShowDevice(device);
        }
        
        private void OnClick()
        {
            // Safety check
            if (device.CanInteract() == false)
            {
                Debug.LogError("Should not be able to click this button");
                return;
            }

            // Execute event
            if (device.IsConnected)
                BluetoothManager.Instance.Disconnect(device);
            else
                BluetoothManager.Instance.Connect(device);
            
            Refresh();
        }
        
        private void OnExit()
        {
            gameObject.SetActive(false);
        }
    }
}
