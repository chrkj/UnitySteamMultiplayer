using System.Collections.Generic;
using System.Linq;
using Steamworks;
using Steamworks.Data;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using Color = UnityEngine.Color;

public class LobbyManager : NetworkBehaviour
{
    public Transform LayoutLobby;
    public GameObject ChatManagerObj;
    public GameObject LobbyAvatarPrefab;
    public UnityEngine.UI.Text LobbyName;
    public UnityEngine.UI.Button ReadyButton;
    public UnityEngine.UI.Button StartButton;

    private bool m_Ready;
    private Lobby m_Lobby;
    private ChatManager m_ChatManager;
    
    private Dictionary<ulong, LobbyAvatar> m_LobbyAvatars = new Dictionary<ulong, LobbyAvatar>();

    private void Start()
    {
        m_ChatManager = ChatManagerObj.GetComponent<ChatManager>();
        m_Lobby = GameNetworkManager.Instance.CurrentLobby.Value;
        SteamMatchmaking.OnLobbyMemberLeave += OnLobbyMemberLeave;
        SteamMatchmaking.OnLobbyMemberJoined += OnLobbyMemberJoined;
        SteamMatchmaking.OnLobbyMemberDataChanged += OnLobbyMemberDataChanged;

        if (NetworkManager.IsHost)
        {
            StartButton.gameObject.SetActive(true);
            StartButton.interactable = false;
        }
        
        RefreshLobby();
    }

    public override void OnDestroy()
    {
        SteamMatchmaking.OnLobbyMemberDataChanged -= OnLobbyMemberDataChanged;
    }

    public void StartGame()
    {
        NetworkManager.SceneManager.LoadScene("InGameScene", LoadSceneMode.Single);
    }
    
    public void Ready()
    {
        m_Ready = !m_Ready;
        m_Lobby.SetMemberData("Ready", m_Ready.ToString());
        ReadyButton.GetComponent<UnityEngine.UI.Image>().color = m_Ready ? new Color(0, 0.25f, 0) : new Color(0.25f, 0, 0);
        ReadyButton.GetComponentInChildren<UnityEngine.UI.Text>().text = m_Ready ? "Ready" : "Click to Ready up";
    }
    
    public void Disconnect()
    {
        m_Lobby.Leave();
        SceneManager.LoadScene("JoinGameScene");
    }

    private void RefreshLobby()
    {
        foreach (LobbyAvatar lobbyAvatar in m_LobbyAvatars.Values)
            Destroy(lobbyAvatar.gameObject);
        m_LobbyAvatars.Clear();

        var lobbyMembers = m_Lobby.Members.ToArray();
        foreach (var lobbyMember in lobbyMembers)
        {
            m_LobbyAvatars[lobbyMember.Id] = InstantiateLobbyAvatar(lobbyMember.Id);
            m_LobbyAvatars[lobbyMember.Id].Refresh();
        }

        LobbyName.text = m_Lobby.GetData("name");
        m_Lobby.SetData("Ready", m_Ready.ToString());
    }
    
    private LobbyAvatar InstantiateLobbyAvatar(ulong steamId)
    {
        LobbyAvatar tmp = Instantiate(LobbyAvatarPrefab, LayoutLobby, false).GetComponent<LobbyAvatar>();
        tmp.SteamID = steamId;
        tmp.gameObject.name = new Friend(steamId).Name;

        var tc = tmp.transform;
        tc.SetParent(LayoutLobby.transform);
        tc.localScale = Vector3.one;
        return tmp;
    }
    
    private void CheckForEveryoneReady()
    {
        // Only the lobby owner checks if everyone is ready and then sends a message to everyone to start the game
        if (m_Lobby.MemberCount > 0)
        {
            if (NetworkManager.Singleton.IsHost)
            {
                bool everyoneReady = true;
                StartButton.interactable = false;
                foreach (var lobbyAvatar in m_LobbyAvatars.Values)
                {
                    if (!lobbyAvatar.Ready)
                    {
                        everyoneReady = false;
                        break;
                    }
                }

                if (everyoneReady)
                {
                    StartButton.interactable = true;
                }
            }
        }
    }

    private void OnLobbyMemberDataChanged(Lobby lobby, Friend friend)
    {
        m_LobbyAvatars[friend.Id].Refresh();
        CheckForEveryoneReady();
    }

    private void OnLobbyMemberJoined(Lobby lobby, Friend friend)
    {
        m_ChatManager.SendMessageToChat($"{friend.Name} joined the lobby.");
        RefreshLobby();
    }
  
    private void OnLobbyMemberLeave(Lobby lobby, Friend friend)
    {
        m_ChatManager.SendMessageToChat($"{friend.Name} left the lobby.");
        RefreshLobby();
    }
    
}
