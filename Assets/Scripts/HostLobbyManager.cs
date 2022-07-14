using UnityEngine;

public class HostLobbyManager : MonoBehaviour
{
    public UnityEngine.UI.InputField GameName;
    public UnityEngine.UI.InputField GamePassword;

    public async void HostLobby()
    {
        if (GameName.text.Length == 0) return;
        HostLobbyData data = new HostLobbyData
        {
            Name = GameName.text,
            Password = GamePassword.text
        };
        await GameNetworkManager.Instance.StartHost(data, 10);
    }
    
    public class HostLobbyData
    {
        public string Name;
        public string Password;
    }
    
}
