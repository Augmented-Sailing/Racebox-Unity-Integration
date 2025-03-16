using UnityEngine;
using UnityEngine.UI.Extensions; // Required for UILineRenderer
using System.Collections;
using System.Collections.Generic;

public class GPSTracker : MonoBehaviour
{
    // Public references to UI elements
    public RectTransform chartPanel;    // The UI Panel representing the chart area
    public GameObject dotPrefab;        // Prefab for historical position dots
    public GameObject currentDot;       // GameObject for the current position marker
    public UILineRenderer line;         // UILineRenderer to draw the movement path

    // Public variables for configuration
    public float positionThreshold = 0.000001f; // Threshold for adding new positions
    public int smoothingWindowSize = 5; // Window size for the moving average filter
    public float fadeDuration = 5f; // Duration for the dots to fade out
  
    [Space]
    [SerializeField] public Transform modelInstance;
    [SerializeField] public Transform anchor;
    
    // Private lists to store history and UI dots
    private List<Vector2> history = new List<Vector2>(); // x = longitude, y = latitude
    private List<RectTransform> dots = new List<RectTransform>();
    private Queue<Vector2> smoothingQueue = new Queue<Vector2>();

    // Origin point
    private Vector2 origin;
    private Vector2 lastPoint;

    /// <summary>
    /// Updates the GPS data from ARSampleManager.
    /// </summary>
    public void UpdateGPSData(double latitude, double longitude, float accuracy)
    {
        Vector2 newPos = new Vector2((float)longitude, (float)latitude);
        Debug.Log($"Received GPS data: Latitude={latitude}, Longitude={longitude}, Accuracy={accuracy}");

        // Set origin if it's the first sample
        if (history.Count == 0)
        {
            origin = newPos;
            RefreshOrigin();
            newPos = Vector2.zero; // Set newPos to zero as it is the origin
            history.Add(newPos);
            lastPoint = newPos;
        }
        else
        {
            // Adjust new position relative to origin
            newPos -= origin;
            Debug.Log($"Adjusted position relative to origin: {newPos}");

            // Check if the new position is significantly different
            if (Vector2.Distance(newPos, lastPoint) > positionThreshold)
            {
                lastPoint = newPos;
                history.Add(newPos);
                Debug.Log($"New position added to history: {newPos}");

                // Instantiate a new dot for the historical position
                GameObject dot = Instantiate(dotPrefab, chartPanel);
                dot.SetActive(true);
                dots.Add(dot.GetComponent<RectTransform>());
                Debug.Log("New dot instantiated and added to chart.");

                // Start fading the dot
                StartCoroutine(FadeOutDot(dot.GetComponent<CanvasRenderer>()));
            }
            else
            {
                // Replace the last value in the history with the new position
                history[history.Count - 1] = newPos;
                Debug.Log($"Last position in history replaced with: {newPos}");
            }
        }

        // Update the chart with the new data
        UpdateChart();
    }

    private IEnumerator FadeOutDot(CanvasRenderer dotRenderer)
    {
        float elapsedTime = 0f;
        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsedTime / fadeDuration);
            dotRenderer.SetAlpha(alpha);
            yield return null;
        }
        dotRenderer.gameObject.SetActive(false);
    }

private void UpdateChart()
{
    if (history.Count == 0) return;

    // Calculate the range of longitude and latitude from history
    float minLong = history[0].x;
    float maxLong = history[0].x;
    float minLat = history[0].y;
    float maxLat = history[0].y;

    foreach (Vector2 pos in history)
    {
        minLong = Mathf.Min(minLong, pos.x);
        maxLong = Mathf.Max(maxLong, pos.x);
        minLat = Mathf.Min(minLat, pos.y);
        maxLat = Mathf.Max(maxLat, pos.y);
    }

    // Ensure a minimum range of 20 meters
    float minRangeMeters = 20f / 111320f; // Convert meters to degrees (approximation)
    if (maxLong - minLong < minRangeMeters)
    {
        float midLong = (minLong + maxLong) / 2;
        minLong = midLong - minRangeMeters / 2;
        maxLong = midLong + minRangeMeters / 2;
    }
    if (maxLat - minLat < minRangeMeters)
    {
        float midLat = (minLat + maxLat) / 2;
        minLat = midLat - minRangeMeters / 2;
        maxLat = midLat + minRangeMeters / 2;
    }

    // Calculate scaling factors to map GPS coordinates to chart dimensions
    float scaleX = chartPanel.rect.width / (maxLong - minLong);
    float scaleY = chartPanel.rect.height / (maxLat - minLat);

    // Calculate the center offset
    float centerX = chartPanel.rect.width / 2;
    float centerY = chartPanel.rect.height / 2;

    // Update positions of dots and collect points for the line renderer
    List<Vector2> points = new List<Vector2>();
    points.Add(new Vector2(0, 0));
    float range = Mathf.Max(maxLong - minLong, maxLat - minLat);
    float scale = Mathf.Clamp(range / minRangeMeters, 0.2f, 1);
    for (int i = 0; i < history.Count; i++)
    {
        float x = (history[i].x - minLong) * scaleX - centerX;
        float y = (history[i].y - minLat) * scaleY - centerY;

        Vector2 point = new Vector2(x, y);
        points.Add(point);

        RectTransform rect = default;
        if (i >= history.Count - 1)
        {
            rect = currentDot.GetComponent<RectTransform>();
        }
        else
        {
            rect = dots[i];
        }

        rect.anchoredPosition = point;
        
        // Scale the dot size based on the height/range
        rect.localScale = new Vector3(scale, scale, scale);
    }

    // Update the line renderer with the scaled points
    line.Points = points.ToArray();
    Debug.Log("Chart updated with new points.");
}

    /// <summary>
    /// Resets the origin and clears the history.
    /// </summary>
    public void RefreshOrigin()
    {
        // Clear history and destroy dots
        history.Clear();
        foreach (var dot in dots)
        {
            Destroy(dot.gameObject);
        }
        dots.Clear();

        // Reset the current position marker
        currentDot.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;

        // Clear the line renderer points
        line.Points = new Vector2[0];

        // Clear the smoothing queue
        smoothingQueue.Clear();

        // Log the reset action
        Debug.Log("Origin and history reset, chart cleared and centered.");

        modelInstance.position = anchor.position - Vector3.down * 5f;
    }
}