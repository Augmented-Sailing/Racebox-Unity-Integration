using UnityEngine;
using UnityEngine.UI;

public class ARUI : MonoBehaviour
{
    

    [SerializeField] private Button button;
    [SerializeField] private Toggle useNativeGPS;

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
        ARSampleManager ARManager = Provider.Instance.GetService<ARSampleManager>();
        ARManager.ToggleNativeGPS(arg0);
    }

    private void OnClick()
    {
        ARSampleManager ARManager = Provider.Instance.GetService<ARSampleManager>();
        ARManager.CustomARModel.RefreshOrigin();
    }
}
