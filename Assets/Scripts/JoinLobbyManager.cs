using System.Collections.Generic;
using UnityEngine;
using Steamworks;
using Utility;

public class JoinLobbyManager : SingletonMono<JoinLobbyManager>
{
    public GameObject LobbyListContent;
    public GameObject LobbyDataItemPrefab;
    public List<GameObject> ListOfLobbies = new List<GameObject>();

    public async void RefreshLobbies()
    {
        foreach (var lobby in ListOfLobbies)
            Destroy(lobby);
        ListOfLobbies.Clear();
        
        var lobbies = await SteamMatchmaking.LobbyList.WithKeyValue("AppID", GameNetworkManager.APP_ID).RequestAsync();
        if (lobbies == null)
        {
            Debug.Log("No lobbies available.", this);
            return;
        }
        
        foreach (var lobby in lobbies)
        {
            var lobbyItem = Instantiate(LobbyDataItemPrefab, LobbyListContent.transform, true);
            var data = lobbyItem.GetComponent<LobbyDataEntry>();
            data.Lobby = lobby;
            data.LobbyName.text = lobby.GetData("name");
            lobbyItem.transform.localScale = Vector3.one;
            ListOfLobbies.Add(lobbyItem);
        }
    }
    
}
