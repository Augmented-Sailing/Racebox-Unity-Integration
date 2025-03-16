using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Scenes : MonoBehaviour
{
    public static Scenes Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<Scenes>();
                if (instance == null)
                {
                    instance = new GameObject("Scenes").AddComponent<Scenes>();
                    DontDestroyOnLoad(instance.gameObject);
                }
            }

            return instance;
        }
    }
    private static Scenes instance;

    private Scene AppScene;
    private Scene ActiveScene;

    public void LoadAppScene()
    {
        Debug.Log("[Scenes] Loading App Scene...");
        StartCoroutine(LoadAppSceneCoroutine());
    }

    public IEnumerator SwitchActiveScene(string name)
    {
        Debug.Log($"[Scenes] Switching active scene to: {name}");
        if (ActiveScene != default && ActiveScene.isLoaded)
        {
            Debug.Log($"[Scenes] Unloading current active scene: {ActiveScene.name}");
            yield return StartCoroutine(UnloadScene(ActiveScene.name));
            ActiveScene = default;
            Debug.Log($"[Scenes] Unloaded current active scene: {ActiveScene.name}");
        }

        Debug.Log($"[Scenes] Loading scene: {ActiveScene.name}");
        yield return StartCoroutine(LoadScene(name));
        ActiveScene = SceneManager.GetSceneByName(name);
        Debug.Log($"[Scenes] Active scene switched to: {ActiveScene.name}");
    }

    private IEnumerator LoadAppSceneCoroutine()
    {
        var sceneToLoad = "App"; // Replace with your scene name
        Debug.Log($"[Scenes] Loading App scene: {sceneToLoad}");
        if (AppScene == default || AppScene.isLoaded == false)
        {
            AppScene = SceneManager.GetSceneByName(sceneToLoad);
        }
        yield return StartCoroutine(LoadScene(sceneToLoad));
        AppScene = SceneManager.GetSceneByName(sceneToLoad);
        Debug.Log($"[Scenes] App scene loaded: {AppScene.name}");

        yield return StartCoroutine(SwitchActiveScene("GPS Sample"));
    }

    private IEnumerator UnloadScene(string name)
    {
        Debug.Log($"[Scenes] Unloading scene: {name}");
        yield return SceneManager.UnloadSceneAsync(name);
        Debug.Log($"[Scenes] Scene unloaded: {name}");
    }

    private IEnumerator LoadScene(string name)
    {
        Debug.Log($"[Scenes] Loading scene: {name}");
        yield return SceneManager.LoadSceneAsync(name, LoadSceneMode.Additive);
        Debug.Log($"[Scenes] Scene loaded: {name}");
    }
}