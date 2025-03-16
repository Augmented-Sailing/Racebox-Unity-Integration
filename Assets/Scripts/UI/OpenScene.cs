using UnityEngine;
using UnityEngine.UI;

public class OpenScene : MonoBehaviour
{
    

    [SerializeField] private Button button;
    [SerializeField] private string sceneName;
    
    private void Awake()
    {
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(OnClick);
    }

    private void OnClick()
    {
        Scenes.Instance.StartCoroutine(Scenes.Instance.SwitchActiveScene(sceneName));
    }
}
