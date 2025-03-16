using System;
using RaceboxIntegration.Other;
using TMPro;
using Unity.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace RaceboxIntegration.UI
{
    /// <summary>
    ///     UI component representing a single Bluetooth device in the device list.
    ///     Displays device information and handles user interaction for device selection.
    /// </summary>
    public class DeviceEntry : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private TMP_Text deviceNameText;
        [SerializeField] private TMP_Text deviceIDText;
        [SerializeField] private TMP_Text statusText;

        [SerializeField] [ReadOnly] private BluetoothDevice bluetoothDevice;

        private Action OnClick;

        /// <summary>
        ///     Gets the Bluetooth device associated with this entry.
        /// </summary>
        public BluetoothDevice BluetoothDevice => bluetoothDevice;

        private void Awake()
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnClicked);
        }

        /// <summary>
        ///     Initializes the device entry with a specific device and click handler.
        /// </summary>
        /// <param name="bluetoothDevice">The Bluetooth device to display.</param>
        /// <param name="onClicked">Callback to execute when the entry is clicked.</param>
        public void Init(BluetoothDevice bluetoothDevice, Action onClicked)
        {
            this.bluetoothDevice = bluetoothDevice;
            OnClick = onClicked;

            Refresh();
        }

        /// <summary>
        ///     Updates the UI elements with current device information.
        /// </summary>
        public void Refresh()
        {
            deviceNameText.SetText(bluetoothDevice.DeviceName);
            deviceIDText.SetText(bluetoothDevice.DeviceUID);
            statusText.SetText(bluetoothDevice.GetStatus());
        }

        private void OnClicked()
        {
            Debug.Log("Clicked on " + bluetoothDevice.DeviceName);

            OnClick?.Invoke();
        }
    }
}