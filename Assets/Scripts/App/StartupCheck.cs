using System.Collections;
using RaceboxIntegration.Managers;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StartupCheck : MonoBehaviour
{
    public static StartupCheck Instance;

    public string sceneA;
    public string sceneB;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        #if UNITY_EDITOR
        if (StartupCheckEditor.IsStartupCheckEnabled())
        #endif
        {
            CheckConditions();
        }
    }

    public void CheckConditions()
    {
        bool loadRaceboxConnectScreen = ShouldLoadRaceboxConnection();

        if (loadRaceboxConnectScreen)
        {
            UnloadSceneIfLoaded(sceneB);
            LoadSceneIfNotLoaded(sceneA);
        }
        else
        {
            UnloadSceneIfLoaded(sceneA);
            LoadSceneIfNotLoaded(sceneB);
        }
    }

    private bool ShouldLoadRaceboxConnection()
    {
        RaceboxManager raceboxManager = Provider.Instance.GetService<RaceboxManager>();
        if (raceboxManager == null) return true;

        if (raceboxManager.IsRaceboxConnected == false) return true;

        return false;
    }

    private void UnloadSceneIfLoaded(string sceneName)
    {
        if (SceneManager.GetSceneByName(sceneName).isLoaded)
        {
            StartCoroutine(UnloadSceneAsync(sceneName));
        }
    }

    private IEnumerator UnloadSceneAsync(string sceneName)
    {
        AsyncOperation asyncUnload = SceneManager.UnloadSceneAsync(sceneName);
        while (!asyncUnload.isDone)
        {
            yield return null;
        }
    }

    private void LoadSceneIfNotLoaded(string sceneName)
    {
        if (!SceneManager.GetSceneByName(sceneName).isLoaded)
        {
            SceneManager.LoadScene(sceneName, LoadSceneMode.Additive);
        }
    }
}