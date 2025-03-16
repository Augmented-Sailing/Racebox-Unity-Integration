using UnityEngine;
using System.Collections;
using TMPro;

public class CameraGPSController1: MonoBehaviour
{
    // ========== ORIGEN ==========
    // (Se usará para restar lat/lon y así convertir en coordenadas relativas)
    public float originLatitude;
    public float originLongitude;
    public float originAltitude;

    // ========== POSICIÓN ACTUAL ==========
    [Header("GPS Actual")]
    public float lat;
    public float lon;

    [Header("Parámetros de Conversión")]
    public float metersPerLatDegree = 111320f;
    public float metersPerLonDegree = 111320f;

    [Header("Movimiento")]
    public float moveSmoothSpeed = 5f;  // Velocidad del Lerp de la cámara
    public bool useGPSAltitude = false; // Si true, se usa la altitud del GPS en 'y'
    public float fixedHeight = 5f;      // Si no usamos altitud GPS, la cámara estará a esta altura

    [Header("Textos en pantalla")]
    public TextMeshProUGUI GPSYN;       // Estado del GPS (inicializado, timeout, etc.)
    public TextMeshProUGUI latitud;     // Texto para mostrar el origen (lat) (opcional)
    public TextMeshProUGUI longitud;    // Texto para mostrar el origen (lon) (opcional)

    // Textos para mostrar la latitud/longitud actuales en tiempo real
    public TextMeshProUGUI latAct;
    public TextMeshProUGUI lonAct;

    // Flag para saber si el GPS está activo
    private bool gpsActive = false;

    // Variables internas para la posición actual
    public float currentLat = 0.0f;
    public float currentLon = 0.0f;
    private float currentAlt = 0.0f;

    private Vector3 lastPosition;
    private bool hasLastPos = false;

    IEnumerator Start()
    {
        // 1. Iniciamos GPS y esperamos
        yield return StartCoroutine(StartGPS());

        // 2. Si el GPS se inicia con éxito, definimos el origen
        if (gpsActive)
        {
            // Obtenemos lat/lon/alt actuales del jugador
            currentLat = Input.location.lastData.latitude;
            currentLon = Input.location.lastData.longitude;
            currentAlt = Input.location.lastData.altitude;

            // OPCIÓN A: Ajustar algún offset (por ejemplo -10m en lat).
            // OPCIÓN B: Simplemente, origen = la posición actual sin offset:
            originLatitude = currentLat;
            originLongitude = currentLon;
            originAltitude = currentAlt;

            Debug.Log($"Origen GPS definido a {originLatitude}, {originLongitude}");

            // Mostramos en los textos opcionales
            if (latitud != null) latitud.text = $"Origen Lat: {originLatitude:F6}";
            if (longitud != null) longitud.text = $"Origen Lon: {originLongitude:F6}";
        }
    }

    void Update()
    {
        // Si el GPS no está activo, no hacemos nada
        if (!gpsActive) return;

        // 1. Actualizamos la posición actual
        currentLat = Input.location.lastData.latitude;
        currentLon = Input.location.lastData.longitude;
        currentAlt = Input.location.lastData.altitude;

        // 2. Convertir a coordenadas en Unity
        Vector3 targetPos = GPSToWorldPosition(currentLat, currentLon, currentAlt);

        // 3. Movimiento suave de la cámara
        transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * moveSmoothSpeed);

        // 4. Mostramos la lat/lon actuales en pantalla
        if (latAct != null) latAct.text = $"Lat actual: {currentLat:F6}";
        if (lonAct != null) lonAct.text = $"Lon actual: {currentLon:F6}";
    }

    private IEnumerator StartGPS()
    {
        // Chequea si el usuario ha habilitado el GPS en ajustes
        if (!Input.location.isEnabledByUser)
        {
            Debug.LogWarning("El usuario no ha habilitado el GPS.");
            if (GPSYN != null) GPSYN.text = "El usuario no ha habilitado el GPS.";
            yield break;
        }

        // Inicia el servicio de localización
        // (precisión aproximada, distancia mínima entre actualizaciones)
        Input.location.Start(0.5f, 0.5f);

        // Esperamos un máximo de 20 segundos
        int maxWait = 20;
        while (Input.location.status == LocationServiceStatus.Initializing && maxWait > 0)
        {
            yield return new WaitForSeconds(1);
            maxWait--;
        }
        if (maxWait < 1)
        {
            Debug.LogWarning("GPS Timeout");
            if (GPSYN != null) GPSYN.text = "GPS Timeout";
            yield break;
        }

        // Si falla el GPS
        if (Input.location.status == LocationServiceStatus.Failed)
        {
            Debug.LogWarning("No se pudo determinar la localización.");
            yield break;
        }
        else
        {
            // Éxito
            gpsActive = true;
            Debug.Log("GPS inicializado correctamente.");
            if (GPSYN != null) GPSYN.text = "GPS inicializado";
        }
    }

    private Vector3 GPSToWorldPosition(float lat, float lon, float alt)
    {
        // 1. Diferencia en grados respecto al origen
        float latDiff = lat - originLatitude;
        float lonDiff = lon - originLongitude;

        // 2. Ajustar la conversión de longitud, multiplicando por cos(ORIGEN)
        float actualMetersPerLonDegree = metersPerLonDegree * Mathf.Cos(originLatitude * Mathf.Deg2Rad);

        // 3. Convertir la diferencia de grados a metros
        float xOffset = lonDiff * actualMetersPerLonDegree;
        float zOffset = latDiff * metersPerLatDegree;

        // 4. Definir Y
        float yPos = useGPSAltitude ? alt : fixedHeight;

        // 5. Retornamos la posición final
        return new Vector3(xOffset, yPos, zOffset);
    }
}
