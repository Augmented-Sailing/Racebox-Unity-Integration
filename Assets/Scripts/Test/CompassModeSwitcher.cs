using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CompassModeSwitcher : MonoBehaviour
{
    public Toggle modeToggle; // Referencia al Toggle en la UI
    public CameraAndCompassDirection cameraCompassScript; // Script basado en la rotaci?n de la c?mara
    public HybridCompass hybridCompassScript; // Script h?brido basado en velocidad
    public TextMeshProUGUI compassText,gpsText; // Texto para mostrar direcci?n h?brida   

    private void Start()
    {
        if (modeToggle != null)
        {
            modeToggle.onValueChanged.AddListener(ToggleCompassMode);
        }

        // Asegurarse de que el modo inicial es el correcto
        ToggleCompassMode(modeToggle.isOn);
        compassText.text = "";    gpsText.text = "";  
    }

    private void ToggleCompassMode(bool useHybrid)
    {
        if (cameraCompassScript != null) cameraCompassScript.enabled = !useHybrid;
        if (hybridCompassScript != null) hybridCompassScript.enabled = useHybrid;
    }
}