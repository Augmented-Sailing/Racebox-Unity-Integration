using UnityEngine;

public class CameraGPSController4 : MonoBehaviour
{
    // ========== ORIGIN ==========
    public float originLatitude = 0f;
    public float originLongitude = 0f;
    public float originAltitude = 0f;

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

    [Tooltip("Speed at which the camera rotates to point to the new direction.")]
    public float rotateSmoothSpeed = 5f;

    public bool useGPSAltitude = false;
    public float fixedHeight = 5f;

    // --- Threshold in meters to "ignore" minimal displacements ---
    [Tooltip("Ignores any position delta smaller than this value (in meters).")]
    public float clampThreshold = 1.0f;

    // --- Allow or disallow camera rotation ---
    [Header("Camera Rotation")]
    [Tooltip("If active, the camera will rotate towards the direction of movement.")]
    public bool allowCameraRotation = true;

    // Indicates if the GPS is active
    private bool gpsActive = false;

    // Current position (either by GPS or manual)
    private float currentLat = 0.0f;
    private float currentLon = 0.0f;
    private float currentAlt = 0.0f;

    // To calculate rotation based on movement
    private Vector3 lastPos;
    private bool hasInitializedRotation = false;

    void Start()
    {
        // Force the camera to the initial position
        Vector3 initialPos = GPSToWorldPosition(lat, lon, 0f);
        transform.position = initialPos;
        lastPos = initialPos;
    }

    void Update()
    {
        // Manual mode
        currentLat = lat;
        currentLon = lon;
        currentAlt = 0f;

        // Calculate the target position in Unity
        Vector3 targetPos = GPSToWorldPosition(currentLat, currentLon, currentAlt);

        // Interpolate the camera's position
        Vector3 newPos = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * moveSmoothSpeed);

        // Calculate the movement direction for rotation
        if (hasInitializedRotation && allowCameraRotation)
        {
            Vector3 movementDir = newPos - lastPos;

            // Rotate only if moved significantly
            if (movementDir.sqrMagnitude > 0.01f)
            {
                float angleDeg = Mathf.Atan2(movementDir.x, movementDir.z) * Mathf.Rad2Deg;
                Quaternion finalRot = Quaternion.Euler(0f, angleDeg, 0f);

                transform.rotation = Quaternion.Slerp(transform.rotation, finalRot, Time.deltaTime * rotateSmoothSpeed);
            }
        }
        else if (!hasInitializedRotation)
        {
            // Initial setup
            transform.eulerAngles = Vector3.zero;
            Debug.Log("[CameraGPSController4] Initial rotation set to 0,0,0.");
            hasInitializedRotation = true;
        }

        // Assign the new position
        transform.position = newPos;
        lastPos = newPos;
    }

    private bool initialized;
    // ========================================
    // CONVERSION AND CLAMP METHOD
    // ========================================
    private Vector3 GPSToWorldPosition(float lat, float lon, float alt)
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

        // Height: either from GPS or fixed
        float yPos = useGPSAltitude ? alt : fixedHeight;

        if (!initialized)
        {
            initialized = true;
            RefreshOrigin();
        }
        
        return new Vector3(xOffset, yPos, zOffset);
    }

    public void RefreshOrigin()
    {
        originLatitude = currentLat;
        originLongitude = currentLon;
        originAltitude = currentAlt;

        Debug.Log("Origin re-aligned: " + originLatitude + ", " + originLongitude);
    }

    public void UpdateGPSLocation(double latDegrees, double lonDegrees)
    {
        this.lat = (float)latDegrees;
        this.lon = (float)lonDegrees;
    }
}