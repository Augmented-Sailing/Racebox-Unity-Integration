using UnityEngine;

public class CameraGPSController : MonoBehaviour
{
    // ========== ORIGIN ==========
    public float originLatitude = 0f;
    public float originLongitude = 0f;

    // ========== CURRENT POSITION (manual or GPS) ==========
    [Header("GPS / Manual Actual")]
    public float lat;
    public float lon;

    [Header("Conversions")]
    public float metersPerLatDegree = 111320f;
    public float metersPerLonDegree = 111320f;

    [Header("Movements")]
    [Tooltip("Speed of the camera's Lerp (position).")]
    public float moveSmoothSpeed = 5f;

    public float fixedHeight = 5f;

    // --- Threshold in meters to "ignore" minimal displacements ---
    [Tooltip("Ignores any position delta smaller than this value (in meters).")]
    public float clampThreshold = 1.0f;

    // Indicates if the GPS is active
    private bool gpsActive = false;

    void Start()
    {
        // Force the camera to the initial position
        Vector3 initialPos = GPSToWorldPosition(lat, lon);
        transform.position = initialPos;
    }

    void Update()
    {
        UpdateCamera(true);
    }

    private void UpdateCamera(bool lerp)
    {
        // Calculate the target position in Unity
        Vector3 targetPos = GPSToWorldPosition(lat, lon);

        // Interpolate the camera's position
        Vector3 newPos = targetPos;
        if(lerp)
            newPos = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * moveSmoothSpeed);

        // Assign the new position
        transform.position = newPos;
    }

    // ========================================
    // CONVERSION AND CLAMP METHOD
    // ========================================
    private Vector3 GPSToWorldPosition(float lat, float lon)
    {
        // Difference in degrees between the current position and the origin
        float latDiff = lat - originLatitude;
        float lonDiff = lon - originLongitude;

        // Conversion adjustment based on latitude
        float actualMetersPerLonDegree = metersPerLonDegree * Mathf.Cos(originLatitude * Mathf.Deg2Rad);

        // Displacement in meters in X (longitude) and Z (latitude)
        float xOffset = lonDiff * actualMetersPerLonDegree;
        float zOffset = latDiff * metersPerLatDegree;

        // If the distance in XZ is less than clampThreshold, force to 0
        Vector2 offsetXZ = new Vector2(xOffset, zOffset);
        if (offsetXZ.sqrMagnitude < (clampThreshold * clampThreshold))
        {
            xOffset = 0f;
            zOffset = 0f;
        }

        return new Vector3(xOffset, fixedHeight, zOffset);
    }

    public void RefreshOrigin()
    {
        originLatitude = lat;
        originLongitude = lon;
        
        UpdateCamera(false);

        Debug.Log("Origin re-aligned: " + originLatitude + ", " + originLongitude);
    }

    public void UpdateGPSLocation(double latDegrees, double lonDegrees)
    {
        this.lat = (float)latDegrees;
        this.lon = (float)lonDegrees;
    }
}