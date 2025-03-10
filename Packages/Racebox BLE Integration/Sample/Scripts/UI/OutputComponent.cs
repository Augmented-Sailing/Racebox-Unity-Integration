using System.Collections;
using System.Collections.Generic;
using System.Linq;
using RaceboxIntegration.Events;
using RaceboxIntegration.Other;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace RaceboxIntegration.UI
{
    /// <summary>
    /// UI component responsible for displaying real-time data from a connected Racebox device.
    /// Handles data visualization and user interaction for device data viewing.
    /// </summary>
    public class OutputComponent : MonoBehaviour
    {
        [SerializeField] private TMP_Text headerText;
        [SerializeField] private TMP_Text outputText;
        [SerializeField] private Button backButton;
        [SerializeField] private Button arButton;

        private BluetoothDevice device;

        private void OnEnable()
        {
            backButton.onClick.RemoveAllListeners();
            backButton.onClick.AddListener(OnBack);
            MainEventBus.OnDeviceRefreshed.AddListener(OnDeviceUpdated);
            MainEventBus.OnDeviceUpdated.AddListener(OnDeviceUpdated);
        }

        private void OnDisable()
        {
            backButton.onClick.RemoveListener(OnBack);
            MainEventBus.OnDeviceRefreshed.RemoveListener(OnDeviceUpdated);
            MainEventBus.OnDeviceUpdated.RemoveListener(OnDeviceUpdated);
        }

        /// <summary>
        /// Initializes the output component with a specific device.
        /// </summary>
        /// <param name="device">The Bluetooth device to display data for.</param>
        public void Initialize(BluetoothDevice device)
        {
            this.device = device;
        }

        private void Refresh()
        {
            if (device.IsConnected == false)
            {
                OnBack();
                return;
            }
            headerText.SetText(device.DeviceName);
            
            if(device.DeviceController is RaceboxDeviceController raceboxDeviceController)
                outputText.SetText(raceboxDeviceController.GetOutput());
            else
                outputText.SetText("Device not recognized");
        }
        
        private void OnBack()
        {
            BluetoothUI.Instance.ReturnToConnection();
        }

        private void OnDeviceUpdated(string deviceId)
        {
            if (device != null && device.DeviceUID == deviceId)
            {
                Refresh();
            }
        }
    }
}