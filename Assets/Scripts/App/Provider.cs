using System;
using System.Collections.Generic;
using UnityEngine;

public class Provider
{
    private static Provider _instance;

    private readonly Dictionary<Type, object> services = new();

    private Provider()
    {
    }

    public static Provider Instance => _instance ?? (_instance = new Provider());

    public void RegisterService(Type type, object service)
    {
        if (!services.ContainsKey(type)) services[type] = service;
    }

    public T GetService<T>()
    {
        var type = typeof(T);
        if (services.ContainsKey(type)) return (T)services[type];
        Debug.LogError(typeof(T).Name + " service not found");

        return default;
    }
}