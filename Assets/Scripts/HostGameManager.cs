using UnityEngine;
using UnityEngine.SceneManagement;

public class HostGameManager : MonoBehaviour
{
    public UnityEngine.UI.InputField GameName;
    public UnityEngine.UI.InputField GamePassword;

    public async void HostGame()
    {
        if (GameName.text.Length == 0) return;
        HostLobbyData data = new HostLobbyData
        {
            Name = GameName.text,
            Password = GamePassword.text
        };
        await GameNetworkManager.Instance.StartHost(data, 10);
        
    }
    
    public void GoBackToMainMenu()
    {
        SceneManager.LoadScene("MainMenuScene");
    }

    public class HostLobbyData
    {
        public string Name;
        public string Password;
    }
    
}
