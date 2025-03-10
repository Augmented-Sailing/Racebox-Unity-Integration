using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class Startup
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void OnRuntimeMethodLoad()
    {
        StartLoadingScene();
    }

    private static void StartLoadingScene()
    {
        string sceneToLoad = "App"; // Replace with your scene name
        SceneManager.LoadScene(sceneToLoad, LoadSceneMode.Additive);
    }
}