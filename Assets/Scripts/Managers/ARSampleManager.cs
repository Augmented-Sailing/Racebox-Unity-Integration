using System;
using RaceboxIntegration.DataModels;
using RaceboxIntegration.Events;
using RaceboxIntegration.Managers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ARSampleManager : MonoBehaviour
{
    #region Constants
    private const float METERS_PER_DEGREE_LAT = 111000f;  // Approximate meters per degree latitude
    private const float MICRO_TO_DEGREES = 1e-7f;         // Convert from factor of 10^7 to degrees
    private const float MM_TO_METERS = 0.001f;            // Millimeters to meters
    private const float HEADING_TO_DEGREES = 1e-5f;       // Convert from factor of 10^5 to degrees
    private const float FILTER_STRENGTH = 0.25f;          // Lower values = more smoothing, higher values = more responsive
    #endregion

    #region Singleton
    public static ARSampleManager Instance { get; private set; }
    #endregion

    #region Serialized Fields
    [SerializeField] private TMP_Text detailsText;
    [SerializeField] private CustomARModel customARModel;
    #endregion

    #region Private Fields
    private RaceboxManager raceboxManager;
    private bool useNativeGPS = false;
    private bool isFirstSample = true;

    // Filtered values for native GPS
    private Vector3 filteredNativePosition = Vector3.zero;
    private Quaternion filteredNativeRotation = Quaternion.identity;
    private float filteredNativeLatitude = 0f;
    private float filteredNativeLongitude = 0f;
    private float filteredNativeAltitude = 0f;
    private float filteredNativeHeading = 0f;

    // Filtered values for Racebox
    private Vector3 filteredRaceboxPosition = Vector3.zero;
    private Quaternion filteredRaceboxRotation = Quaternion.identity;
    private float filteredRaceboxLatitude = 0f;
    private float filteredRaceboxLongitude = 0f;
    private float filteredRaceboxAltitude = 0f;
    private float filteredRaceboxHeading = 0f;
    #endregion

    #region Unity Lifecycle
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

    private void Start()
    {
        raceboxManager = Provider.Instance.GetService<RaceboxManager>();
        if (raceboxManager == null)
        {
            Debug.LogError("RaceboxManager not found in Provider!");
        }
    }

    private void OnEnable()
    {
        MainEventBus.OnDeviceUpdated.AddListener(OnDeviceRefreshed);
    }

    private void OnDisable()
    {
        MainEventBus.OnDeviceUpdated.RemoveListener(OnDeviceRefreshed);
        StopAllTracking();
    }
    #endregion

    #region Public Methods
    public void ToggleTrackingSource(bool useNative)
    {
        StopAllTracking();
        useNativeGPS = useNative;
        isFirstSample = true; // Reset filter state when switching sources

        if (useNativeGPS)
        {
            StartNativeGPSTracking();
        }
        else if (raceboxManager != null && raceboxManager.IsRaceboxConnected)
        {
            UpdateRaceboxTracking();
        }
        else
        {
            Debug.LogWarning("Racebox not connected. Please connect a Racebox device first.");
        }
    }

    public void ResetOrigin()
    {
        customARModel.RefreshOrigin();
        isFirstSample = true; // Reset filter state when resetting origin
    }
    #endregion

    #region Private Methods
    private void StopAllTracking()
    {
        if (Input.location.isEnabledByUser)
        {
            Input.location.Stop();
            Input.compass.enabled = false;
        }
    }

    private void StartNativeGPSTracking()
    {
        if (!Input.location.isEnabledByUser)
        {
            Debug.LogWarning("Location services are not enabled by the user.");
            return;
        }

        Input.location.Start();
        Input.compass.enabled = true;
        
    }

    private void Update()
    {
        if (useNativeGPS && Input.location.status == LocationServiceStatus.Running)
        {
            UpdateNativeGPSTracking();
        }
    }

    private void OnDeviceRefreshed(string deviceId)
    {
        if (!useNativeGPS && raceboxManager != null && raceboxManager.IsRaceboxConnected)
        {
            UpdateRaceboxTracking();
        }
    }

    private void UpdateNativeGPSTracking()
    {
        LocationInfo loc = Input.location.lastData;
        float headingDegrees = Input.compass.trueHeading;
        
        // Apply low-pass filter to raw values
        filteredNativeLatitude = LowPassFilter(loc.latitude, filteredNativeLatitude);
        filteredNativeLongitude = LowPassFilter(loc.longitude, filteredNativeLongitude);
        filteredNativeAltitude = LowPassFilter(loc.altitude, filteredNativeAltitude);
        filteredNativeHeading = LowPassFilter(headingDegrees, filteredNativeHeading);

        UpdateTracking(filteredNativeLatitude, filteredNativeLongitude, filteredNativeAltitude, filteredNativeHeading, true);
        
        detailsText.text +=
            $"\nGyro Rotation: {Input.gyro.attitude.eulerAngles}\n" +
            $"Horizontal Accuracy: {Input.location.lastData.horizontalAccuracy}\n" +
            $"Vertical Accuracy: {Input.location.lastData.verticalAccuracy}";
    }

    private void UpdateRaceboxTracking()
    {
        var raceboxData = raceboxManager.RaceboxDeviceController.Data;
        if (raceboxData == null || raceboxData.invalidLatLon)
        {
            Debug.LogWarning("Invalid or missing Racebox data, skipping update.");
            return;
        }

        float latDegrees = raceboxData.latitude * MICRO_TO_DEGREES;
        float lonDegrees = raceboxData.longitude * MICRO_TO_DEGREES;
        float altitudeMeters = raceboxData.wgsAltitude * MM_TO_METERS;
        float headingDegrees = raceboxData.heading * HEADING_TO_DEGREES;

        // Apply low-pass filter to raw values
        filteredRaceboxLatitude = LowPassFilter(latDegrees, filteredRaceboxLatitude);
        filteredRaceboxLongitude = LowPassFilter(lonDegrees, filteredRaceboxLongitude);
        filteredRaceboxAltitude = LowPassFilter(altitudeMeters, filteredRaceboxAltitude);
        filteredRaceboxHeading = LowPassFilter(headingDegrees, filteredRaceboxHeading);

        UpdateTracking(filteredRaceboxLatitude, filteredRaceboxLongitude, filteredRaceboxAltitude, filteredRaceboxHeading, false);
        
        detailsText.text +=
            $"\nHeading Accuracy: {raceboxData.headingAccuracy}\n" +
            $"Horizontal Accuracy: {raceboxData.horizontalAccuracy}\n" +
            $"Vertical Accuracy: {raceboxData.verticalAccuracy}";
    }

    private void UpdateTracking(float latDegrees, float lonDegrees, float altitudeMeters, float headingDegrees, bool isNative)
    {
        if (!ValidateTrackingData(latDegrees, lonDegrees, altitudeMeters, headingDegrees))
        {
            return;
        }

        (Vector3 position, Quaternion rotation) = ComputeTrackingData(latDegrees, lonDegrees, altitudeMeters, headingDegrees);

        // Apply low-pass filter to position and rotation
        if (isNative)
        {
            filteredNativePosition = LowPassFilterVector3(position, filteredNativePosition);
            filteredNativeRotation = LowPassFilterQuaternion(rotation, filteredNativeRotation);
            customARModel.UpdateTracking(filteredNativePosition, filteredNativeRotation);
        }
        else
        {
            filteredRaceboxPosition = LowPassFilterVector3(position, filteredRaceboxPosition);
            filteredRaceboxRotation = LowPassFilterQuaternion(rotation, filteredRaceboxRotation);
            customARModel.UpdateTracking(filteredRaceboxPosition, filteredRaceboxRotation);
        }
        
        UpdateDetailsText(latDegrees, lonDegrees, altitudeMeters, headingDegrees, isNative ? filteredNativePosition : filteredRaceboxPosition);
    }

    private bool ValidateTrackingData(float latDegrees, float lonDegrees, float altitudeMeters, float headingDegrees)
    {
        if (latDegrees < -90f || latDegrees > 90f)
        {
            Debug.LogError($"Invalid latitude: {latDegrees}. Must be between -90 and 90 degrees.");
            return false;
        }

        if (lonDegrees < -180f || lonDegrees > 180f)
        {
            Debug.LogError($"Invalid longitude: {lonDegrees}. Must be between -180 and 180 degrees.");
            return false;
        }

        if (altitudeMeters < -10000f || altitudeMeters > 10000f)
        {
            Debug.LogWarning($"Unusual altitude: {altitudeMeters} meters. Please verify the data.");
        }

        if (headingDegrees < 0f || headingDegrees >= 360f)
        {
            Debug.LogError($"Invalid heading: {headingDegrees}. Must be between 0 and 360 degrees.");
            return false;
        }

        return true;
    }

    private (Vector3 position, Quaternion rotation) ComputeTrackingData(float latDegrees, float lonDegrees, float altitudeMeters, float headingDegrees)
    {
        float metersPerDegreeLon = METERS_PER_DEGREE_LAT * Mathf.Cos(latDegrees * Mathf.Deg2Rad);

        float x = lonDegrees * metersPerDegreeLon;
        float y = altitudeMeters;
        float z = latDegrees * METERS_PER_DEGREE_LAT;

        Vector3 position = new Vector3(x, y, z);
        Quaternion rotation = Quaternion.Euler(0, -headingDegrees, 0);

        return (position, rotation);
    }

    private void UpdateDetailsText(float latDegrees, float lonDegrees, float altitudeMeters, float headingDegrees, Vector3 position)
    {
        if (detailsText != null)
        {
            detailsText.text = $"Lat: {latDegrees}\n" +
                             $"Lon: {lonDegrees}\n" +
                             $"Alt: {altitudeMeters}m\n" +
                             $"Heading: {headingDegrees:F1}°\n" +
                             $"Position: {position}";
        }
    }
#endregion
    
    #region Low-Pass Filter Methods
    private float LowPassFilter(float newValue, float previousValue)
    {
        if (isFirstSample)
        {
            isFirstSample = false;
            return newValue;
        }
        return (FILTER_STRENGTH * newValue) + ((1 - FILTER_STRENGTH) * previousValue);
    }

    private Vector3 LowPassFilterVector3(Vector3 newValue, Vector3 previousValue)
    {
        if (isFirstSample)
        {
            isFirstSample = false;
            return newValue;
        }
        return (FILTER_STRENGTH * newValue) + ((1 - FILTER_STRENGTH) * previousValue);
    }

    private Quaternion LowPassFilterQuaternion(Quaternion newValue, Quaternion previousValue)
    {
        if (isFirstSample)
        {
            isFirstSample = false;
            return newValue;
        }

        // Convert quaternions to vectors for filtering
        Vector4 newInputVector = new Vector4(newValue.x, newValue.y, newValue.z, newValue.w);
        Vector4 prevValueVector = new Vector4(previousValue.x, previousValue.y, previousValue.z, previousValue.w);
        
        // Apply low pass filter to each component
        Vector4 filtered = (FILTER_STRENGTH * newInputVector) + ((1 - FILTER_STRENGTH) * prevValueVector);
        
        // Normalize to ensure valid quaternion
        float magnitude = Mathf.Sqrt(filtered.x * filtered.x + filtered.y * filtered.y + 
                                   filtered.z * filtered.z + filtered.w * filtered.w);
        filtered /= magnitude;
        
        return new Quaternion(filtered.x, filtered.y, filtered.z, filtered.w);
    }
    #endregion
}