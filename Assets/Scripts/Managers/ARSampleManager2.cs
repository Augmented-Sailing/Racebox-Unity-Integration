using RaceboxIntegration.DataModels;
using RaceboxIntegration.Events;
using RaceboxIntegration.Managers;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

public class ARSampleManager2 : MonoBehaviour
{
    // **Conversion Constants**
    private const float METERS_PER_DEGREE_LAT = 111000f;  // Approximate meters per degree latitude
    private const float MICRO_TO_DEGREES = 1e-6f;        // Microdegrees to degrees
    private const float MM_TO_METERS = 0.001f;           // Millimeters to meters

    // **Singleton Instance**
    public static ARSampleManager2 Instance { get; private set; }

    // **Dependencies**
    private RaceboxManager RaceboxManager;
    private bool useNativeGPS = false;
    private Vector3 initialGPS = Vector3.zero;  // Stores initial (lat, lon, alt) as reference
    private Vector3 currentGPS;                 // Stores current (lat, lon, alt)

    // **AR Components**
    public ARSessionOrigin arSessionOrigin;     // Reference to ARSessionOrigin for AR management
    public GameObject modelInstance;            // Reference to the existing model in the scene

    // **Lifecycle Methods**
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

    // **GPS Tracking Methods**
    private void OnDeviceRefreshed(string arg0)
    {
        if (!useNativeGPS && RaceboxManager.IsRaceboxConnected)
        {
            UpdateTracking(RaceboxManager.RaceboxDeviceController.Data);
        }
    }

    public void UpdateTracking(RaceboxData raceboxData)
    {
        if (raceboxData == null || raceboxData.invalidLatLon)
        {
            Debug.LogWarning("Invalid or missing Racebox data, skipping update.");
            return;
        }

        float latDegrees = raceboxData.latitude * MICRO_TO_DEGREES;
        float lonDegrees = raceboxData.longitude * MICRO_TO_DEGREES;
        float altitudeMeters = raceboxData.wgsAltitude * MM_TO_METERS;
        float headingDegrees = raceboxData.heading * MICRO_TO_DEGREES;

        currentGPS = new Vector3(latDegrees, lonDegrees, altitudeMeters);

        if (initialGPS == Vector3.zero)
        {
            initialGPS = currentGPS;
        }

        Debug.Log($"Current GPS: Lat={latDegrees}, Lon={lonDegrees}, Alt={altitudeMeters}, Heading={headingDegrees}");
    }

    public void ToggleNativeGPS(bool enable)
    {
        useNativeGPS = enable;
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

            currentGPS = new Vector3(latDegrees, lonDegrees, altitudeMeters);

            if (initialGPS == Vector3.zero)
            {
                initialGPS = currentGPS;
            }

            Debug.Log($"Native GPS: Lat={latDegrees}, Lon={lonDegrees}, Alt={altitudeMeters}, Heading={headingDegrees}");
        }
    }

    // **AR Position Calculation**
    private Vector3 CalculateARPosition(Vector3 gps)
    {
        float deltaLat = (gps.x - initialGPS.x) * METERS_PER_DEGREE_LAT;
        float deltaLon = (gps.y - initialGPS.y) * METERS_PER_DEGREE_LAT * Mathf.Cos(initialGPS.x * Mathf.Deg2Rad);
        float deltaAlt = gps.z - initialGPS.z;
        return new Vector3(deltaLon, deltaAlt, deltaLat);
    }

    // **Object Placement**
    public void PlaceObjectAtCurrentGPS(GameObject prefab)
    {
        if (initialGPS == Vector3.zero)
        {
            Debug.LogWarning("Initial GPS not set. Cannot place object.");
            return;
        }

        if (prefab == null)
        {
            Debug.LogWarning("Prefab is null. Cannot place object.");
            return;
        }

        Vector3 arPosition = CalculateARPosition(currentGPS);
        GameObject obj = Instantiate(prefab, arPosition, Quaternion.identity);
        Debug.Log($"Placed object at AR position: {arPosition}");
    }

    // **Origin Refresh**
    public void RefreshOrigin()
    {
        if (arSessionOrigin == null || modelInstance == null)
        {
            Debug.LogWarning("Cannot refresh origin: ARSessionOrigin or modelInstance not assigned.");
            return;
        }

        // Calculate the offset from the camera to the model
        Vector3 modelOffset = modelInstance.transform.position - arSessionOrigin.camera.transform.position;

        // Reset the AR session origin to the camera's current position and rotation
        arSessionOrigin.transform.position = arSessionOrigin.camera.transform.position;
        arSessionOrigin.transform.rotation = arSessionOrigin.camera.transform.rotation;

        // Reposition the model relative to the new origin
        modelInstance.transform.position = arSessionOrigin.camera.transform.position + modelOffset;

        Debug.Log("Origin refreshed and model repositioned.");
    }
}