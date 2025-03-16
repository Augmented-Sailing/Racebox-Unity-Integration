using UnityEngine;

public static class Startup
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void OnRuntimeMethodLoad()
    {
        StartLoadingScene();
    }

    private static void StartLoadingScene()
    {
        Scenes.Instance.LoadAppScene();
    }
}