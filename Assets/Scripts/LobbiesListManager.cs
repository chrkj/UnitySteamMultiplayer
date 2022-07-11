using System.Collections.Generic;
using UnityEngine;
using Steamworks;
using UnityEngine.SceneManagement;

public class LobbiesListManager : MonoBehaviour
{
    public static LobbiesListManager Instance;
    
    public GameObject LobbyListContent;
    public GameObject LobbyDataItemPrefab;

    public List<GameObject> ListOfLobbies = new List<GameObject>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    private void Start()
    {
        RefreshLobbies();
    }

    public void DestroyLobbies()
    {
        foreach (var lobby in ListOfLobbies)
            Destroy(lobby);
        ListOfLobbies.Clear();
    }

    public async void DisplayLobbies()
    {
        //var lobbies = await SteamMatchmaking.LobbyList.RequestAsync();
        var lobbies = await SteamMatchmaking.LobbyList.WithKeyValue("AppID", GameNetworkManager.APP_ID).RequestAsync();
        for (int i = 0; i < lobbies.Length; i++)
        {
            var lobby = lobbies[i];
            var lobbyItem = Instantiate(LobbyDataItemPrefab);
            var data = lobbyItem.GetComponent<LobbyDataEntry>();
            data.Lobby = lobby;
            data.LobbyName = lobby.GetData("name");
            data.SetLobbyData();
            lobbyItem.transform.SetParent(LobbyListContent.transform);
            lobbyItem.transform.localScale = Vector3.one;
            ListOfLobbies.Add(lobbyItem);
        }
    }

    public void RefreshLobbies()
    {
        DestroyLobbies();
        DisplayLobbies();
    }

    public void GoBackToMainMenu()
    {
        SceneManager.LoadScene("MainMenuScene");
    }
}
