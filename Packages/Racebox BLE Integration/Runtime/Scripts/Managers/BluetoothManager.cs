using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using Android.BLE;
using Android.BLE.Commands;
using RaceboxIntegration.Events;
using RaceboxIntegration.Other;
using Random = UnityEngine.Random;
using UnityEngine.Android; // Add this for Permission class

namespace RaceboxIntegration.Managers
{
    [DefaultExecutionOrder(-500)]
    public class BluetoothManager : MonoBehaviour
    {
        public static BluetoothManager Instance { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetInstance()
        {
            Instance = null;
        }

        public bool IsScanning { get; private set; }
        public float ScanProgress { get => _scanTimer / _scanTime; }

        [SerializeField] private int _scanTime = 10;
        [SerializeField] private List<BluetoothDevice> bluetoothDevices = new List<BluetoothDevice>();

        private float _scanTimer = 0f;

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
#if !UNITY_EDITOR
            BleManager.Instance.Initialize();
#endif
        }

        private void Update()
        {
            if (IsScanning)
            {
                if (_scanTimer < _scanTime)
                {
                    _scanTimer += Time.deltaTime;
                }
            }
        }

        public void ScanForDevices()
        {
            if (!IsScanning)
            {
#if UNITY_ANDROID && !UNITY_EDITOR
                // Check and request ACCESS_FINE_LOCATION permission
                if (!Permission.HasUserAuthorizedPermission(Permission.FineLocation))
                {
                    Permission.RequestUserPermission(Permission.FineLocation);
                    StartCoroutine(WaitForPermission());
                    return;
                }
#endif
                StartScan();
            }
        }

        private void StartScan()
        {
            IsScanning = true;
            _scanTimer = 0f;
#if !UNITY_EDITOR
            BleManager.Instance.QueueCommand(new DiscoverDevices(OnDeviceFound, OnFinishedScan, _scanTime * 1000));
#else
            StartCoroutine(SimulateDevices());
#endif
        }

        private IEnumerator WaitForPermission()
        {
            while (!Permission.HasUserAuthorizedPermission(Permission.FineLocation))
            {
                yield return null;
            }
            StartScan(); // Proceed with scanning once permission is granted
        }

#if UNITY_EDITOR
        private IEnumerator SimulateDevices()
        {
            while (IsScanning)
            {
                yield return new WaitForSeconds(Random.Range(.25f, 2f));
                string deviceName = "Device " + Random.Range(0, 100);
                if (Random.Range(0, 100) < 50)
                {
                    deviceName = "Racebox " + Random.Range(0, 100);
                }

                string deviceUid = Random.Range(0, 99) + ":" +
                                   Random.Range(0, 99) + ":" +
                                   Random.Range(0, 99) + ":" +
                                   Random.Range(0, 99) + ":" +
                                   Random.Range(0, 99);
                OnDeviceFound(deviceUid, deviceName);
            }
        }
#endif

        public BluetoothDevice GetDevice(string deviceUid)
        {
            return bluetoothDevices.FirstOrDefault(d => d.DeviceName == deviceUid);
        }

        public List<BluetoothDevice> GetDeviceList()
        {
            return bluetoothDevices;
        }

        public void Connect(BluetoothDevice device)
        {
            if (device.IsConnected)
            {
                Debug.LogError("Already connected while attempting to connect");
                return;
            }
            StartCoroutine(device.Connect());
        }

        public void Disconnect(BluetoothDevice device)
        {
            if (!device.IsConnected)
            {
                Debug.LogError("Already disconnected while attempting to disconnect");
                return;
            }
            StartCoroutine(device.Disconnect());
        }

        private void OnDeviceFound(string uid, string deviceName)
        {
            if (string.IsNullOrWhiteSpace(deviceName))
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