using UnityEngine;
using UnityEngine.UI;

public class ARUI : MonoBehaviour
{
    

    [SerializeField] private Button button;
    [SerializeField] private Toggle useNativeGPS;
    [SerializeField] private ARSample arSample;

    private void Awake()
    {
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(OnClick);
        
        useNativeGPS.SetIsOnWithoutNotify(false);
        useNativeGPS.onValueChanged.RemoveAllListeners();
        useNativeGPS.onValueChanged.AddListener(OnToggleNativeGPS);
    }

    private void OnToggleNativeGPS(bool arg0)
    {
        arSample.ToggleTrackingSource(arg0);
    }

    private void OnClick()
    {
        arSample.ResetOrigin();
    }
}
