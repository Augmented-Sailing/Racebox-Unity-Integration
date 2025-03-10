using System.Collections;
using RaceboxIntegration.Events;
using RaceboxIntegration.Other;
using UnityEngine;

namespace RaceboxIntegration.Managers
{
    [DefaultExecutionOrder(-500)]
    public class RaceboxManager : MonoBehaviour
    {
        public static RaceboxManager Instance { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetInstance()
        {
            Instance = null;
        }

        public RaceboxDeviceController RaceboxDeviceController;
        public bool IsRaceboxConnected { get => RaceboxDeviceController != null && RaceboxDeviceController.Device.IsConnected && RaceboxDeviceController.Data != null; }

        private Coroutine simulationCoroutine;

        private void Awake()
        {
            // Delete ourselves if manager already exists
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnEnable()
        {
            MainEventBus.OnDeviceConnectionUpdated.AddListener(OnDeviceConnectionStatusUpdated);
        }

        private void OnDisable()
        {
            MainEventBus.OnDeviceConnectionUpdated.RemoveListener(OnDeviceConnectionStatusUpdated);
        }

        private void OnDeviceConnectionStatusUpdated(string deviceUid)
        {
            BluetoothDevice device = BluetoothManager.Instance.GetDevice(deviceUid);
            if (device == null)
            {
                Debug.LogWarning("Device not found: " + deviceUid);
                return;
            }

            RaceboxDeviceController raceboxDeviceController = device.DeviceController as RaceboxDeviceController;
            if (raceboxDeviceController != null)
            {
                RaceboxDeviceController = raceboxDeviceController;
                if (IsRaceboxConnected)
                {
                    StartSimulation();
                }
                else
                {
                    StopSimulation();
                }
            }
        }

        private void StartSimulation()
        {
            if (simulationCoroutine == null)
            {
                simulationCoroutine = StartCoroutine(SimulateData());
            }
        }

        private void StopSimulation()
        {
            if (simulationCoroutine != null)
            {
                StopCoroutine(simulationCoroutine);
                simulationCoroutine = null;
            }
        }

        private IEnumerator SimulateData()
        {
            while (true)
            {
                if (RaceboxDeviceController != null)
                {
                    RaceboxDeviceController.SimulateRaceboxData();
                }
                yield return new WaitForSeconds(0.2f); // Update every second
            }
        }
    }
}