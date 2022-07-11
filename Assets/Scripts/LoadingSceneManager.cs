using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class LoadingSceneManager : NetworkBehaviour
{

    public Text LoadingText;
    public Slider LoadingBar;
    
    private AsyncOperation m_LoadingOperation;
    
    private void Start()
    {
        m_LoadingOperation = SceneManager.LoadSceneAsync(LoadingData.SceneToLoad);
        m_LoadingOperation.allowSceneActivation = false;
        //m_LoadingOperation = NetworkManager.SceneManager.LoadScene(LoadingData.SceneToLoad, LoadSceneMode.Single);
        DelayForMili();
    }

    private void Update()
    {
        float progressValue = Mathf.Clamp01(m_LoadingOperation.progress / 1f);
        LoadingText.text = progressValue.ToString();
        LoadingBar.value = m_LoadingOperation.progress;
        Debug.Log(Mathf.Round(progressValue * 100) + "%");
    }

    private async void DelayForMili()
    {
        await Task.Delay(5000);
        m_LoadingOperation.allowSceneActivation = true;
    }
    
}
