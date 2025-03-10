using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class CustomARModel : MonoBehaviour
{
    [SerializeField] private RawImage background;
    [SerializeField] private GameObject modelInstance;
    [SerializeField] private Transform modelInstancePivot;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private float cameraHeight = 5f; // Height of the fixed camera
    [SerializeField] private float movementScale = 2f; // Scale of object movement
    private const float FILTER_STRENGTH = 0.25f;
    private WebCamTexture cameraTexture;

    private bool originSet;
    private bool gyroInitialized;
    
    // Low-pass filter variables
    private Vector3 filteredGyroEuler = Vector3.zero;
    private Vector3 filteredPosition = Vector3.zero;
    private Quaternion filteredRotation = Quaternion.identity;
    private bool isFirstSample = true;

    private Vector3 trackedPosition = Vector3.zero;
    private Quaternion trackedRotation = Quaternion.identity;
    private Vector3 originOffset = Vector3.zero;

    void Start()
    {
        InitializeGyroscope();
        
        // Initialize camera feed
        cameraTexture = new WebCamTexture();
        background.texture = cameraTexture;
        cameraTexture.Play();

        // Start the coroutine to correct the webcam feed
        StartCoroutine(WaitForWebCamTexture());

        // Ensure RawImage fills the canvas
        RectTransform rt = background.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 0);
        rt.anchorMax = new Vector2(1, 1);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        // Set up main camera at fixed position looking down
        if (mainCamera != null)
        {
            mainCamera.clearFlags = CameraClearFlags.Depth;
            mainCamera.nearClipPlane = 0.1f;
            mainCamera.farClipPlane = 100f;
            // Position camera at an angle
            mainCamera.transform.position = new Vector3(0, cameraHeight, -cameraHeight);
        }
        else
        {
            Debug.LogError("Main Camera not assigned!", this);
        }

        // Set up model at initial position
        if (modelInstance == null)
        {
            Debug.LogError("ModelInstance not assigned!", this);
        }
        else
        {
            modelInstance.transform.position = Vector3.zero; // Start at center
            modelInstance.transform.rotation = Quaternion.identity;
        }
    }

    private void InitializeGyroscope()
    {
        if (SystemInfo.supportsGyroscope)
        {
            Input.gyro.enabled = true;
            gyroInitialized = true;
            isFirstSample = true;
            Debug.Log("Gyroscope initialized successfully");
        }
        else
        {
            Debug.LogWarning("Gyroscope not supported on this device");
            gyroInitialized = false;
        }
    }

    private Vector3 LowPassFilterVector3(Vector3 newInput, Vector3 prevValue)
    {
        if (isFirstSample)
        {
            isFirstSample = false;
            return newInput;
        }
        
        // Low pass filter: Y[n] = α * X[n] + (1-α) * Y[n-1]
        return (FILTER_STRENGTH * newInput) + ((1 - FILTER_STRENGTH) * prevValue);
    }

    private Quaternion LowPassFilterQuaternion(Quaternion newInput, Quaternion prevValue)
    {
        if (isFirstSample)
        {
            isFirstSample = false;
            return newInput;
        }

        // Convert quaternions to vectors for filtering
        Vector4 newInputVector = new Vector4(newInput.x, newInput.y, newInput.z, newInput.w);
        Vector4 prevValueVector = new Vector4(prevValue.x, prevValue.y, prevValue.z, prevValue.w);
        
        // Apply low pass filter to each component
        Vector4 filtered = (FILTER_STRENGTH * newInputVector) + ((1 - FILTER_STRENGTH) * prevValueVector);
        
        // Normalize to ensure valid quaternion
        float magnitude = Mathf.Sqrt(filtered.x * filtered.x + filtered.y * filtered.y + 
                                   filtered.z * filtered.z + filtered.w * filtered.w);
        filtered /= magnitude;
        
        return new Quaternion(filtered.x, filtered.y, filtered.z, filtered.w);
    }

    private IEnumerator WaitForWebCamTexture()
    {
        // Wait until the webcam texture is ready
        yield return new WaitUntil(() => cameraTexture.didUpdateThisFrame);

        // Get rotation and mirroring properties
        float rotationAngle = -cameraTexture.videoRotationAngle;
        bool isMirrored = cameraTexture.videoVerticallyMirrored;

        // Apply rotation to the RawImage
        background.rectTransform.localEulerAngles = new Vector3(0, 0, rotationAngle);

        // Apply mirroring if needed
        if (isMirrored)
        {
            background.rectTransform.localScale = new Vector3(1, -1, 1);
        }
        else
        {
            background.rectTransform.localScale = Vector3.one;
        }

        // Calculate the effective aspect ratio after rotation
        float aspectRatio;
        if (Mathf.Abs(rotationAngle) % 180 == 90)
        {
            // Swap width and height for 90 or 270-degree rotations
            aspectRatio = (float)cameraTexture.height / cameraTexture.width;
        }
        else
        {
            aspectRatio = (float)cameraTexture.width / cameraTexture.height;
        }

        // Add and configure AspectRatioFitter
        AspectRatioFitter arf = background.GetComponent<AspectRatioFitter>();
        if (arf == null)
        {
            arf = background.gameObject.AddComponent<AspectRatioFitter>();
        }
        arf.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        arf.aspectRatio = aspectRatio;
    }

    // Update tracking data from your system
    public void UpdateTracking(Vector3 position, Quaternion rotation)
    {
        trackedPosition = position;
        trackedRotation = rotation;
        
        if (!originSet)
        {
            RefreshOrigin();
            originSet = true;
        }
    }

    void Update()
    {
        if (modelInstance != null && gyroInitialized)
        {
            // Get filtered gyroscope data
            Quaternion rawGyroRotation = Input.gyro.attitude;
            rawGyroRotation = new Quaternion(rawGyroRotation.x, rawGyroRotation.y, -rawGyroRotation.z, -rawGyroRotation.w);
            Vector3 currentGyroEuler = rawGyroRotation.eulerAngles;
            filteredGyroEuler = LowPassFilterVector3(currentGyroEuler, filteredGyroEuler);

            // Convert euler angles to radians
            float pitchRad = filteredGyroEuler.x * Mathf.Deg2Rad;
            float rollRad = filteredGyroEuler.z * Mathf.Deg2Rad;

            // Calculate movement based on device tilt
            float moveX = Mathf.Sin(rollRad) * movementScale;
            float moveZ = -Mathf.Sin(pitchRad) * movementScale;

            // Calculate target position with gyro offset
            Vector3 gyroOffset = new Vector3(moveX, 0, moveZ);
            Vector3 targetPosition = gyroOffset + (trackedPosition - originOffset);
            
            // Apply low-pass filter to position
            filteredPosition = LowPassFilterVector3(targetPosition, filteredPosition);
            modelInstance.transform.position = filteredPosition;

            // Filter and apply rotation
            Quaternion targetRotation = trackedRotation * Quaternion.Euler(0, filteredGyroEuler.y, 0);
            filteredRotation = LowPassFilterQuaternion(targetRotation, filteredRotation);
            modelInstance.transform.rotation = filteredRotation;

            // Make camera look at the object
            if (mainCamera != null)
            {
                mainCamera.transform.LookAt(filteredPosition);
            }
        }
    }

    void OnDestroy()
    {
        if (cameraTexture != null)
        {
            cameraTexture.Stop();
        }
    }

    public void RefreshOrigin()
    {
        if (modelInstance != null)
        {
            // Set the current tracked position as our new origin offset
            originOffset = trackedPosition;
            
            // Reset model to center relative to new origin
            modelInstance.transform.position = Vector3.zero;
            modelInstance.transform.rotation = Quaternion.identity;
            
            // Reset all filtering
            isFirstSample = true;
            filteredGyroEuler = Vector3.zero;
            filteredPosition = Vector3.zero;
            filteredRotation = Quaternion.identity;
            
            Debug.Log($"New origin set at tracked position: {originOffset}");
        }
    }
}