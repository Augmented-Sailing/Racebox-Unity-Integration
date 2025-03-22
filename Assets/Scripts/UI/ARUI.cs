using System;
using UnityEngine;
using UnityEngine.UI;

public class ARUI : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private Toggle useNativeGPS;
    [SerializeField] private FloatController rotationControl;
    [SerializeField] private FloatController scaleControl;
    [SerializeField] private FloatController yPosControl;
    [SerializeField] private ARWrapper arWrapper;

    private void Start()
    {
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(OnClick);

        useNativeGPS.onValueChanged.RemoveAllListeners();
        useNativeGPS.onValueChanged.AddListener(OnToggleNativeGPS);

        rotationControl.onValueChanged.AddListener(arWrapper.SetRotation);

        scaleControl.onValueChanged.AddListener(arWrapper.SetScale);
        yPosControl.onValueChanged.AddListener(arWrapper.SetYPos);
    }

    private void OnEnable()
    {
        rotationControl.MinValue = 0;
        rotationControl.MaxValue = 360;
        rotationControl.SetValueWithoutNotify(arWrapper.GetRotation());

        scaleControl.MinValue = 0.1f;
        scaleControl.MaxValue = 10f;
        scaleControl.SetValueWithoutNotify(arWrapper.GetScale());

        yPosControl.MinValue = -10;
        yPosControl.MaxValue = 10;
        yPosControl.SetValueWithoutNotify(arWrapper.GetYPos());

        useNativeGPS.SetIsOnWithoutNotify(arWrapper.useNativeGPS);
    }

    private void OnToggleNativeGPS(bool useNative)
    {
        arWrapper.ToggleTrackingSource(useNative);
    }

    private void OnClick()
    {
        arWrapper.ResetOrigin();
    }
}