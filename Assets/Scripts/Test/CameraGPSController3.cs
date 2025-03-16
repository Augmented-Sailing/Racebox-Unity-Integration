using UnityEngine;
using System.Collections;
using TMPro;

public class CameraGPSController3 : MonoBehaviour
{
    // ========== ORIGEN ==========
    public float originLatitude;
    public float originLongitude;
    public float originAltitude;

    // ========== POSICI?N ACTUAL (manual o GPS) ==========
    [Header("GPS / Manual Actual")]
    [Tooltip("Si el GPS no est? activo, se usar?n estas coordenadas introducidas en el Inspector.")]
    public float lat;
    public float lon;

    [Header("Par?metros de Conversi?n")]
    public float metersPerLatDegree = 111320f;
    public float metersPerLonDegree = 111320f;

    [Header("Movimiento")]
    [Tooltip("Velocidad del Lerp de la c?mara (posici?n).")]
    public float moveSmoothSpeed = 5f;

    [Tooltip("Velocidad con la que la c?mara rota para apuntar a la nueva direcci?n.")]
    public float rotateSmoothSpeed = 5f;

    public bool useGPSAltitude = false;
    public float fixedHeight = 5f;

    // --- NUEVO: Umbral en metros para ?ignorar? desplazamientos m?nimos ---
    [Tooltip("Ignora cualquier delta de posici?n menor que este valor (en metros).")]
    public float clampThreshold = 1.0f;

    // --- NUEVO: Permitir o no la rotaci?n de la c?mara ---
    [Header("Rotaci?n de la C?mara")]
    [Tooltip("Si est? activo, la c?mara rotar? hacia la direcci?n del movimiento.")]
    public bool allowCameraRotation = true;

    [Header("Textos en pantalla")]
    public TextMeshProUGUI GPSYN;       // Estado del GPS (inicializado, timeout, etc.)
    public TextMeshProUGUI latitud;     // Texto para mostrar el origen (lat)
    public TextMeshProUGUI longitud;    // Texto para mostrar el origen (lon)
    public TextMeshProUGUI latAct;      // Texto lat actual
    public TextMeshProUGUI lonAct;      // Texto lon actual

    // Indica si el GPS est? activo
    private bool gpsActive = false;

    // Posici?n actual (sea por GPS o manual)
    private float currentLat = 0.0f;
    private float currentLon = 0.0f;
    private float currentAlt = 0.0f;

    // Para calcular la rotaci?n seg?n el movimiento
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

        // Posici?n inicial
        lastPos = transform.position;
    }

    void Update()
    {
        // 1. Leemos la posici?n actual (GPS o manual)
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

        // 2. Calculamos la posici?n objetivo en Unity
        Vector3 targetPos = GPSToWorldPosition(currentLat, currentLon, currentAlt);

        // Interpolamos la posici?n de la c?mara
        Vector3 newPos = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * moveSmoothSpeed);

        // 3. Calculamos la direcci?n de movimiento para la rotaci?n
        if (hasInitializedRotation && allowCameraRotation) // <-- Aqu? revisamos si est? activo "allowCameraRotation"
        {
            Vector3 movementDir = newPos - lastPos;

            // Solo rotamos si nos hemos movido lo suficiente
            if (movementDir.sqrMagnitude > 0.01f)
            {
                float angleDeg = Mathf.Atan2(movementDir.x, movementDir.z) * Mathf.Rad2Deg;
                Quaternion finalRot = Quaternion.Euler(0f, angleDeg, 0f);

                transform.rotation = Quaternion.Slerp(transform.rotation, finalRot, Time.deltaTime * rotateSmoothSpeed);
            }
        }
        else if (!hasInitializedRotation)
        {
            // Configuraci?n inicial
            transform.eulerAngles = Vector3.zero; // Aseguramos rotaci?n 0,0,0
            Debug.Log("[CameraGPSController3] Rotaci?n inicial configurada a 0,0,0.");
            hasInitializedRotation = true;
        }

        // Asignamos la nueva posici?n
        transform.position = newPos;
        lastPos = newPos;
    }

    private IEnumerator StartGPS()
    {
        // Verificamos si el usuario activ? el GPS
        if (!Input.location.isEnabledByUser)
        {
            Debug.LogWarning("[CameraGPSController3] GPS deshabilitado por el usuario.");
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
            Debug.LogWarning("[CameraGPSController3] GPS Timeout");
            if (GPSYN) GPSYN.text = "GPS Timeout";
            yield break;
        }

        if (Input.location.status == LocationServiceStatus.Failed)
        {
            Debug.LogWarning("[CameraGPSController3] No se pudo determinar la localizaci?n.");
            yield break;
        }
        else
        {
            // GPS activo
            gpsActive = true;
            Debug.Log("[CameraGPSController3] GPS inicializado correctamente.");
            if (GPSYN) GPSYN.text = "GPS inicializado";
        }
    }

    // ========================================
    // M?TODO CON "CLAMP" DE DESPLAZAMIENTOS
    // ========================================
    private Vector3 GPSToWorldPosition(float lat, float lon, float alt)
    {
        // Diferencia en grados entre la posici?n actual y el origen
        float latDiff = lat - originLatitude-0.0001f;//////////////////--------------------->>>>float latDiff = lat - originLatitude;   
        float lonDiff = lon - originLongitude;

        // Ajuste de conversi?n en funci?n de la latitud
        float actualMetersPerLonDegree = metersPerLonDegree * Mathf.Cos(originLatitude * Mathf.Deg2Rad);

        // Desplazamiento en metros en X y Z
        float xOffset = lonDiff * actualMetersPerLonDegree; // Eje X
        float zOffset = latDiff * metersPerLatDegree;       // Eje Z

        // Si la distancia en XZ es menor que clampThreshold, forzamos a 0
        Vector2 offsetXZ = new Vector2(xOffset, zOffset);
        if (offsetXZ.sqrMagnitude < (clampThreshold * clampThreshold))
        {
            xOffset = 0f;
            zOffset = 0f;
        }

        // Altura: o la del GPS, o la fija
        float yPos = useGPSAltitude ? alt : fixedHeight;

        return new Vector3(xOffset, yPos, zOffset);
    }
}
