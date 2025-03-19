using RaceboxIntegration.Events;
using RaceboxIntegration.Managers;
using TMPro;
using UnityEngine;

public class DisplayGPSDetails : MonoBehaviour
{
    
    [SerializeField] private TMP_Text detailsText;
    
    private void OnEnable()
    {
        MainEventBus.OnDeviceUpdated.AddListener(DeviceUpdated);
        if(RaceboxManager.Instance.IsRaceboxConnected == false)
        {
            detailsText.SetText("Waiting for Racebox Connection");
        }
    }

    private void DeviceUpdated(string deviceUid)
    {
        if(RaceboxManager.Instance.IsRaceboxConnected && 
           RaceboxManager.Instance.RaceboxDeviceController.Device.DeviceUID == deviceUid)
        {
            var raceboxData = RaceboxManager.Instance.RaceboxDeviceController.Data;
            if (raceboxData == null || raceboxData.invalidLatLon)
            {
                Debug.LogWarning("Invalid or missing Racebox data, skipping update.");
                return;
            }
            double latDegrees = raceboxData.latitude;
            double lonDegrees = raceboxData.longitude;
            float altitudeMeters = raceboxData.wgsAltitude;
            float accuracy = raceboxData.horizontalAccuracy;
            UpdateDetailsText(latDegrees, lonDegrees, altitudeMeters, accuracy);
        }
    }

    private void UpdateDetailsText(double latDegrees, double lonDegrees, float altitudeMeters, float accuracy)
    {
        if (detailsText != null)
        {
            detailsText.text = $"Lat: {latDegrees:F8}\nLon: {lonDegrees:F8}\nAlt: {altitudeMeters:F4}m\nAccuracy: {accuracy:F3}mm";
        }
    }
}
