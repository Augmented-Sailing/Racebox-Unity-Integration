using UnityEngine;

/// <summary>
/// Controls the camera's rotation using the device's gyroscope and its position using GPS data.
/// The position is mapped from real-world latitude and longitude to Unity's 3D space, with an adjustable scale.
/// </summary>
public class CameraGyroController : MonoBehaviour
{
    [Header("Configuration")]
    [Tooltip("Adjustment quaternion to align gyroscope data with Unity's coordinate system.")]
    [SerializeField] private Quaternion rotationAdjustment = Quaternion.Euler(270f, 0f, 0f);

    [SerializeField] private bool logDebugInfo = false;

    #region Unity Lifecycle Methods

    /// <summary>
    /// Initializes the gyroscope and starts the location service coroutine.
    /// </summary>
    private void Start()
    {
        // Enable gyroscope if supported
        if (SystemInfo.supportsGyroscope)
        {
            Input.gyro.enabled = true;
            if (logDebugInfo) Debug.Log("Gyroscope enabled.");
        }
        else
        {
            Debug.LogWarning("Gyroscope not supported on this device.");
        }
    }

    /// <summary>
    /// Updates the camera's rotation and position every frame.
    /// </summary>
    private void Update()
    {
        UpdateRotation();
    }

    #endregion

    /// <summary>
    /// Updates the camera's rotation based on the device's gyroscope data.
    /// </summary>
    private void UpdateRotation()
    {
        if (!Input.gyro.enabled) return;

        // Get the gyroscope attitude and apply the adjustment for Unity's coordinate system
        Quaternion gyroQuat = Input.gyro.attitude;
        // Invert the X and Y components to correct the rotation directions
        Quaternion adjustedGyroQuat = new Quaternion(-gyroQuat.x, -gyroQuat.y, gyroQuat.z, gyroQuat.w);
        transform.rotation = rotationAdjustment * adjustedGyroQuat;

        if (logDebugInfo)
        {
            Debug.Log($"Camera rotation updated: {transform.eulerAngles}");
        }
    }
}