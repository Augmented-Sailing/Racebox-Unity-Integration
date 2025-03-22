using RaceboxIntegration.Events;
using RaceboxIntegration.Managers;
using UnityEngine;

public class ARWrapper : MonoBehaviour
{
    [SerializeField] private GPSTracker gpsTracker;
    [SerializeField] private CameraGPSController cameraGpsController;

    [SerializeField] private Transform modelInstance;
    [SerializeField] private Transform anchor;

    private RaceboxManager raceboxManager;
    public bool useNativeGPS { get; private set; } = true;

    void Awake()
    {
        // Initialize RaceboxManager
        raceboxManager = Provider.Instance.GetService<RaceboxManager>();
        if (raceboxManager == null)
        {
            Debug.LogError("RaceboxManager not found in Provider!");
        }
    }

    void OnEnable()
    {
        // Listen for Racebox device updates
        if (raceboxManager.IsRaceboxConnected)
            useNativeGPS = false;
        
        MainEventBus.OnDeviceUpdated.AddListener(OnDeviceRefreshed);
        MainEventBus.OnDeviceConnectionUpdated.AddListener(OnDeviceRefreshed);
    }

    void OnDisable()
    {
        // Clean up listener and stop tracking
        MainEventBus.OnDeviceUpdated.RemoveListener(OnDeviceRefreshed);
        MainEventBus.OnDeviceConnectionUpdated.RemoveListener(OnDeviceRefreshed);
        StopAllTracking();
    }

    public void ToggleTrackingSource(bool useNative)
    {
        StopAllTracking(); // Clear any active tracking
        useNativeGPS = useNative; // Set the tracking source flag
        if (useNativeGPS)
        {
            StartNativeGPSTracking(); // Start native GPS
        }
        else if (raceboxManager != null && raceboxManager.IsRaceboxConnected)
        {
            UpdateRaceboxTracking(); // Start Racebox tracking if connected
        }
        else
        {
            Debug.LogWarning("Racebox not connected. Please connect a Racebox device first.");
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

    private void StopAllTracking()
    {
        if (Input.location.isEnabledByUser)
        {
            Input.location.Stop();
            Input.compass.enabled = false;
        }
    }

    private void Update()
    {
        // Only update native GPS tracking if we’re using it
        if (useNativeGPS && Input.location.status == LocationServiceStatus.Running)
        {
            UpdateNativeGPSTracking();
        }
    }

    private void OnDeviceRefreshed(string deviceId)
    {
        // Update Racebox tracking when device updates, if we’re not using native GPS
        if (!useNativeGPS && raceboxManager != null && raceboxManager.IsRaceboxConnected)
        {
            UpdateRaceboxTracking();
        }
    }

    private void UpdateNativeGPSTracking()
    {
        LocationInfo loc = Input.location.lastData;
        float headingDegrees = Input.compass.trueHeading;
        UpdateTracking(loc.latitude, loc.longitude, loc.altitude, headingDegrees, loc.horizontalAccuracy);
    }

    private void UpdateRaceboxTracking()
    {
        var raceboxData = raceboxManager.RaceboxDeviceController.Data;
        if (raceboxData == null || raceboxData.invalidLatLon)
        {
            Debug.LogWarning("Invalid or missing Racebox data, skipping update.");
            return;
        }
        double latDegrees = raceboxData.latitude;
        double lonDegrees = raceboxData.longitude;
        float altitudeMeters = raceboxData.wgsAltitude;
        float headingDegrees = raceboxData.heading;
        float accuracy = raceboxData.horizontalAccuracy;
        UpdateTracking(latDegrees, lonDegrees, altitudeMeters, headingDegrees, accuracy);
    }

    private bool initialized;
    
    private void UpdateTracking(double latDegrees, double lonDegrees, float altitudeMeters, float headingDegrees, float accuracy)
    {
        if(gpsTracker != null)
            gpsTracker.UpdateGPSData(latDegrees, lonDegrees, accuracy);
        if(cameraGpsController != null) 
            cameraGpsController.UpdateGPSLocation(latDegrees, lonDegrees);

        if (!initialized)
        {
            initialized = true;
            ResetOrigin();
        }
    }

    public void ResetOrigin()
    {
        float modelY = modelInstance.position.y;
        if(gpsTracker != null)
            gpsTracker.RefreshOrigin();
        if(cameraGpsController != null)
            cameraGpsController.RefreshOrigin();
        modelInstance.position = new Vector3(anchor.position.x, modelY, anchor.position.z);
        Debug.Log("Origin reset in ARSampleManager");
    }

    public void SetRotation(float arg0)
    {
        modelInstance.localEulerAngles = new Vector3(0, arg0, 0);
    }

    public void SetScale(float arg0)
    {
        modelInstance.localScale = Vector3.one * arg0;
    }

    public void SetYPos(float yPos)
    {
        Vector3 position = modelInstance.position;
        position.y = yPos;
        modelInstance.position = position;
    }

    public float GetYPos()
    {
        return modelInstance.position.y;
    }
    
    public float GetRotation()
    {
        return modelInstance.localEulerAngles.y;
    }

    public float GetScale()
    {
        return modelInstance.localScale.x;
    }
}