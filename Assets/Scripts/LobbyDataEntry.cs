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
        Debug.Log($"Joining {Lobby.Owner.Name}'s lobby");
        GameNetworkManager.Instance.JoinLobby(Lobby);
    }
}
