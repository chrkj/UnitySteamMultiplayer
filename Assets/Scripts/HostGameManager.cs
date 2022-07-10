using UnityEngine;
using UnityEngine.SceneManagement;

public class HostGameManager : MonoBehaviour
{
    public UnityEngine.UI.InputField GameName;
    public UnityEngine.UI.InputField GamePassword;

    public async void HostGame()
    {
        if (GameName.text.Length == 0) return;
        HostLobbyData data = new HostLobbyData();
        data.Name = GameName.text;
        data.Password = GamePassword.text; 
        await GameNetworkManager.Instance.StartHost(data);
        SceneManager.LoadScene("LobbyScene");
    }

    public class HostLobbyData
    {
        public string Name;
        public string Password;
    }

}
