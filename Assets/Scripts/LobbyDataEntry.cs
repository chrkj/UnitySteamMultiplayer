using Steamworks.Data;
using UnityEngine;
using UnityEngine.UI;

public class LobbyDataEntry : MonoBehaviour
{
    public Lobby Lobby;
    public string LobbyName;
    public Text LobbyNameText;

    public void SetLobbyData()
    {
        LobbyNameText.text = LobbyName;
    }

    public void JoinLobby()
    {
        GameNetworkManager.Instance.JoinLobby(Lobby);
    }
}
