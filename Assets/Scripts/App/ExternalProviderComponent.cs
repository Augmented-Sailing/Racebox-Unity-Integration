using System;
using UnityEngine;

public class ExternalProviderComponent : MonoBehaviour
{
    [SerializeField] private MonoBehaviour externalManager;

    void Awake()
    {
        if (externalManager != null)
        {
            Type managerType = externalManager.GetType();
            Provider.Instance.RegisterService(managerType, externalManager);
        }
        else
        {
            Debug.LogError("External manager is not assigned.");
        }
    }
}