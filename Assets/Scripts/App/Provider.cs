using System;
using System.Collections.Generic;
using UnityEngine;

public class Provider
{
    private static Provider _instance;
    public static Provider Instance => _instance ?? (_instance = new Provider());

    private Dictionary<Type, object> services = new Dictionary<Type, object>();

    private Provider() { }

    public void RegisterService<T>(T service)
    {
        var type = typeof(T);
        if (!services.ContainsKey(type))
        {
            services[type] = service;
        }
    }

    public void RegisterService(Type type, object service)
    {
        if (!services.ContainsKey(type))
        {
            services[type] = service;
        }
    }

    public T GetService<T>()
    {
        var type = typeof(T);
        if (services.ContainsKey(type))
        {
            return (T)services[type];
        }
        Debug.LogError(typeof(T).Name + " service not found");

        return default(T);
    }
}