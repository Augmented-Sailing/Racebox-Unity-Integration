using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class OpenARSampleButton : MonoBehaviour
{
    [SerializeField] private Button button;

    private void Awake()
    {
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(OnClick);
    }

    private void OnClick()
    {
        StartupCheck.Instance.CheckConditions();
    }
}
