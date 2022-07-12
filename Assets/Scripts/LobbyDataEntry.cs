using Steamworks.Data;
using UnityEngine;
using UnityEngine.UI;

public class LobbyDataEntry : MonoBehaviour
{
    public Lobby Lobby;
    public Text LobbyName;

    public void JoinLobby()
    {
        Debug.Log($"Joining {Lobby.Owner.Name}'s lobby");
        GameNetworkManager.Instance.JoinLobby(Lobby);
    }
}
