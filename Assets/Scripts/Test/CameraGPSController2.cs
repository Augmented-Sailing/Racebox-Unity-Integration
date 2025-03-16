using UnityEngine;
using System.Collections;
using TMPro;

public class CameraGPSController2 : MonoBehaviour
{
    // ========== ORIGEN ==========
    public float originLatitude;
    public float originLongitude;
    public float originAltitude;

    // ========== POSICIÓN ACTUAL (manual o GPS) ==========
    [Header("GPS / Manual Actual")]
    [Tooltip("Si el GPS no está activo, se usarán estas coordenadas introducidas en el Inspector.")]
    public float lat;
    public float lon;

    [Header("Parámetros de Conversión")]
    public float metersPerLatDegree = 111320f;
    public float metersPerLonDegree = 111320f;

    [Header("Movimiento")]
    [Tooltip("Velocidad del Lerp de la cámara (posición).")]
    public float moveSmoothSpeed = 5f;

    [Tooltip("Velocidad con la que la cámara rota para apuntar a la nueva dirección.")]
    public float rotateSmoothSpeed = 5f;

    public bool useGPSAltitude = false;
    public float fixedHeight = 5f;

    [Header("Textos en pantalla")]
    public TextMeshProUGUI GPSYN;       // Estado del GPS (inicializado, timeout, etc.)
    public TextMeshProUGUI latitud;     // Texto para mostrar el origen (lat)
    public TextMeshProUGUI longitud;    // Texto para mostrar el origen (lon)
    public TextMeshProUGUI latAct;      // Texto lat actual
    public TextMeshProUGUI lonAct;      // Texto lon actual

    // Indica si el GPS está activo
    private bool gpsActive = false;

    // Posición actual (sea por GPS o manual)
    private float currentLat = 0.0f;
    private float currentLon = 0.0f;
    private float currentAlt = 0.0f;

    // Para calcular la rotación según el movimiento
    private Vector3 lastPos;
    private bool hasInitializedRotation = false;

    IEnumerator Start()
    {
        // Iniciamos el GPS
        yield return StartCoroutine(StartGPS());

        if (gpsActive)
        {
            // Modo GPS
            currentLat = Input.location.lastData.latitude;
            currentLon = Input.location.lastData.longitude;
            currentAlt = Input.location.lastData.altitude;

            originLatitude = currentLat;
            originLongitude = currentLon;
            originAltitude = currentAlt;

            if (latitud != null) latitud.text = $"Origen Lat: {originLatitude:F6}";
            if (longitud != null) longitud.text = $"Origen Lon: {originLongitude:F6}";
        }
        else
        {
            // Modo manual
            if (GPSYN) GPSYN.text = "GPS inactivo -> Manual";

            originLatitude = lat;
            originLongitude = lon;
            originAltitude = 0f;

            if (latitud != null) latitud.text = $"Origen Lat: {originLatitude:F6}";
            if (longitud != null) longitud.text = $"Origen Lon: {originLongitude:F6}";
        }

        // Posición inicial
        lastPos = transform.position;
    }

    void Update()
    {
        // 1. Leemos la posición actual (GPS o manual)
        if (gpsActive)
        {
            currentLat = Input.location.lastData.latitude;
            currentLon = Input.location.lastData.longitude;
            currentAlt = Input.location.lastData.altitude;

            // Mostramos en la UI
            if (latAct) latAct.text = $"Lat actual: {currentLat:F6}";
            if (lonAct) lonAct.text = $"Lon actual: {currentLon:F6}";
        }
        else
        {
            // Modo manual
            currentLat = lat;
            currentLon = lon;
            currentAlt = 0f;

            // Mostramos en la UI
            if (latAct) latAct.text = $"Lat manual: {currentLat:F6}";
            if (lonAct) lonAct.text = $"Lon manual: {currentLon:F6}";
        }

        // 2. Calculamos la posición objetivo en Unity
        Vector3 targetPos = GPSToWorldPosition(currentLat, currentLon, currentAlt);

        // Interpolamos la posición de la cámara
        Vector3 newPos = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * moveSmoothSpeed);

        // 3. Calculamos la dirección de movimiento para la rotación
        if (hasInitializedRotation)
        {
            Vector3 movementDir = newPos - lastPos;

            // Solo rotamos si nos hemos movido lo suficiente
            if (movementDir.sqrMagnitude > 0.01f)
            {
                float angleDeg = Mathf.Atan2(movementDir.x, movementDir.z) * Mathf.Rad2Deg;
                Quaternion finalRot = Quaternion.Euler(0f, 0f, 0f); //-----------------------> Quaternion.Euler(0f, angleDeg, 0f);

                transform.rotation = Quaternion.Slerp(transform.rotation, finalRot, Time.deltaTime * rotateSmoothSpeed);
            }
        }
        else
        {
            // Configuración inicial
            transform.eulerAngles = Vector3.zero; // Aseguramos rotación 0,0,0
            Debug.Log("[CameraGPSController2] Rotación inicial configurada a 0,0,0.");
            hasInitializedRotation = true;
        }

        // Asignamos la nueva posición
        transform.position = newPos;
        lastPos = newPos;
    }

    private IEnumerator StartGPS()
    {
        // Verificamos si el usuario activó el GPS
        if (!Input.location.isEnabledByUser)
        {
            Debug.LogWarning("[CameraGPSController2] GPS deshabilitado por el usuario.");
            if (GPSYN) GPSYN.text = "GPS deshabilitado";
            yield break;
        }

        Input.location.Start(0.5f, 0.5f);

        int maxWait = 20;
        while (Input.location.status == LocationServiceStatus.Initializing && maxWait > 0)
        {
            yield return new WaitForSeconds(1);
            maxWait--;
        }
        if (maxWait < 1)
        {
            Debug.LogWarning("[CameraGPSController2] GPS Timeout");
            if (GPSYN) GPSYN.text = "GPS Timeout";
            yield break;
        }

        if (Input.location.status == LocationServiceStatus.Failed)
        {
            Debug.LogWarning("[CameraGPSController2] No se pudo determinar la localización.");
            yield break;
        }
        else
        {
            // GPS activo
            gpsActive = true;
            Debug.Log("[CameraGPSController2] GPS inicializado correctamente.");
            if (GPSYN) GPSYN.text = "GPS inicializado";
        }
    }

    private Vector3 GPSToWorldPosition(float lat, float lon, float alt)
    {
        float latDiff = lat - originLatitude;
        float lonDiff = lon - originLongitude;

        float actualMetersPerLonDegree = metersPerLonDegree * Mathf.Cos(originLatitude * Mathf.Deg2Rad);
        float xOffset = lonDiff * actualMetersPerLonDegree;
        float zOffset = latDiff * metersPerLatDegree;

        float yPos = useGPSAltitude ? alt : fixedHeight;
        return new Vector3(xOffset, yPos, zOffset);
    }
}
