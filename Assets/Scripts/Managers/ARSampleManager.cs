using System;
using RaceboxIntegration.DataModels;
using RaceboxIntegration.Events;
using RaceboxIntegration.Managers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ARSampleManager : MonoBehaviour
{
    // Conversion constants
    private const float METERS_PER_DEGREE_LAT = .5f;  // Approximate meters per degree latitude
    private const float MICRO_TO_DEGREES = 1e-7f;         // Convert from factor of 10^7 to degrees
    private const float MM_TO_METERS = 0.001f;            // Millimeters to meters
    private const float HEADING_TO_DEGREES = 1e-5f;       // Convert from factor of 10^5 to degrees
    
    public static ARSampleManager Instance { get; private set; }

    public CustomARModel CustomARModel { get => _customARModel; }
    
    [SerializeField] private TMP_Text detailsText;
    [SerializeField] private CustomARModel _customARModel;

    private RaceboxManager RaceboxManager;
    private bool useNativeGPS = false;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Provider.Instance.RegisterService(this);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        MainEventBus.OnDeviceUpdated.AddListener(OnDeviceRefreshed);
    }

    private void Start()
    {
        if (RaceboxManager == null)
            RaceboxManager = Provider.Instance.GetService<RaceboxManager>();
    }

    private void OnDisable()
    {
        MainEventBus.OnDeviceUpdated.RemoveListener(OnDeviceRefreshed);
    }

    private void Update()
    {
        if (useNativeGPS)
        {
            UpdateNativeGPSTracking();
        }
    }

    private void OnDeviceRefreshed(string arg0)
    {
        if (!useNativeGPS && RaceboxManager.IsRaceboxConnected)
        {
            UpdateTracking(RaceboxManager.RaceboxDeviceController.Data);
        }
    }

    public void ToggleNativeGPS(bool arg0)
    {
        useNativeGPS = arg0;
        if (useNativeGPS)
        {
            if (!Input.location.isEnabledByUser)
            {
                Debug.LogWarning("Location services are not enabled by the user.");
                return;
            }
            Input.location.Start();
            Input.compass.enabled = true;
        }
        else
        {
            Input.location.Stop();
            Input.compass.enabled = false;
        }
    }

    private void UpdateNativeGPSTracking()
    {
        if (Input.location.status == LocationServiceStatus.Running)
        {
            LocationInfo loc = Input.location.lastData;
            float latDegrees = loc.latitude;
            float lonDegrees = loc.longitude;
            float altitudeMeters = loc.altitude;
            float headingDegrees = Input.compass.trueHeading;

            (var position, var rotation) = ComputeTrackingData(latDegrees, lonDegrees, altitudeMeters, headingDegrees);
            CustomARModel.UpdateTracking(position, rotation);
        }
    }

   private (Vector3 position, Quaternion rotation) ComputeTrackingData(float latDegrees, float lonDegrees, float altitudeMeters, float headingDegrees)
    {
        // Validate latitude
        if (latDegrees < -90f || latDegrees > 90f)
        {
            Debug.LogError($"Invalid latitude: {latDegrees}. Latitude must be between -90 and 90 degrees.");
            return (Vector3.zero, Quaternion.identity);
        }

        // Validate longitude
        if (lonDegrees < -180f || lonDegrees > 180f)
        {
            Debug.LogError($"Invalid longitude: {lonDegrees}. Longitude must be between -180 and 180 degrees.");
            return (Vector3.zero, Quaternion.identity);
        }

        // Validate altitude (assuming no specific range, but you can add checks if needed)
        if (altitudeMeters < -10000f || altitudeMeters > 10000f)
        {
            Debug.LogWarning($"Unusual altitude: {altitudeMeters} meters. Please verify the data.");
        }

        // Validate heading
        if (headingDegrees < 0f || headingDegrees >= 360f)
        {
            Debug.LogError($"Invalid heading: {headingDegrees}. Heading must be between 0 and 360 degrees.");
            return (Vector3.zero, Quaternion.identity);
        }

        float metersPerDegreeLon = METERS_PER_DEGREE_LAT * Mathf.Cos(latDegrees * Mathf.Deg2Rad);

        float x = lonDegrees * metersPerDegreeLon;
        float y = altitudeMeters;
        float z = latDegrees * METERS_PER_DEGREE_LAT;

        Vector3 position = new Vector3(x, y, z);
        Quaternion rotation = Quaternion.Euler(0, -headingDegrees, 0);

        Debug.Log($"Computed Position: {position}, Rotation: {rotation.eulerAngles}");
        return (position, rotation);
    }

    private void UpdateTracking(RaceboxData raceboxData)
    {
        if (raceboxData == null || raceboxData.invalidLatLon)
        {
            Debug.LogWarning("Invalid or missing Racebox data, skipping update.");
            return;
        }

        float latDegrees = raceboxData.latitude * MICRO_TO_DEGREES;
        float lonDegrees = raceboxData.longitude * MICRO_TO_DEGREES;
        float altitudeMeters = raceboxData.wgsAltitude * MM_TO_METERS;
        float headingDegrees = raceboxData.heading * HEADING_TO_DEGREES;

        (var position, var rotation) = ComputeTrackingData(latDegrees, lonDegrees, altitudeMeters, headingDegrees);
        CustomARModel.UpdateTracking(position, rotation);

        detailsText.text = "Lat: " + latDegrees + "\nLon: " + lonDegrees + "\nAlt: " + altitudeMeters + "\nHeading: " + headingDegrees;

        detailsText.text += "\n(" + position.ToString() + ")";
        
        Debug.Log($"Updated Tracking - Position: {position}, Rotation: {rotation.eulerAngles}");
    }
}