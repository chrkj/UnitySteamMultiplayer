using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SceneLoading;
using Steamworks;
using Steamworks.Data;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Color = UnityEngine.Color;
using Debug = System.Diagnostics.Debug;
using Image = UnityEngine.UI.Image;

public class LobbyManager : NetworkBehaviour
{
    public Transform LayoutLobby;
    public GameObject ChatManagerObj;
    public GameObject LobbyAvatarPrefab;
    
    public Text LobbyName;
    public Button ReadyButton;
    public Button StartButton;
    public Button LeaveButton;
    public InputField ChatInputField;

    private bool m_Ready;
    private Lobby m_Lobby;
    private bool m_Gamestarted;
    private ChatManager m_ChatManager;
    private readonly Dictionary<ulong, LobbyAvatar> m_LobbyAvatars = new Dictionary<ulong, LobbyAvatar>();

    private void Start()
    {
        m_ChatManager = ChatManagerObj.GetComponent<ChatManager>();
        
        Debug.Assert(GameNetworkManager.Instance.CurrentLobby != null, $"GameNetworkManager.Instance.CurrentLobby is null in {this}");
        m_Lobby = GameNetworkManager.Instance.CurrentLobby.Value;
        
        SteamMatchmaking.OnChatMessage += OnChatMessage;
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
        SteamMatchmaking.OnChatMessage -= OnChatMessage;
        SteamMatchmaking.OnLobbyMemberLeave -= OnLobbyMemberLeave;
        SteamMatchmaking.OnLobbyMemberJoined -= OnLobbyMemberJoined;
        SteamMatchmaking.OnLobbyMemberDataChanged -= OnLobbyMemberDataChanged;
    }

    public async void StartGame()
    {
        m_Gamestarted = true;
        StartButton.interactable = false;
        m_Lobby.SetMemberData("Start", "true");
        
        m_Lobby.SendChatString("Game starting...");
        for (var i = 5; i > 0; i--)
        {
            m_Lobby.SendChatString($"{i}...");
            await Task.Delay(1000);
        }
        
        SceneLoaderWrapper.Instance.LoadScene("InGameScene", useNetworkSceneManager: true);
    }

    public void Ready()
    {
        m_Ready = !m_Ready;
        m_Lobby.SetMemberData("Ready", m_Ready.ToString());
        ReadyButton.GetComponent<Image>().color = m_Ready ? new Color(0, 0.25f, 0) : new Color(0.25f, 0, 0);
        ReadyButton.GetComponentInChildren<Text>().text = m_Ready ? "Ready" : "Click to Ready up";
    }
    
    public void Disconnect()
    {
        m_Lobby.Leave();
        NetworkManager.Singleton.Shutdown();
        GameNetworkManager.Instance.CurrentLobby = null;
        SceneManager.LoadScene("MainMenuScene");
    }

    public void SendLobbyMessage()
    {
        m_Lobby.SendChatString(ChatInputField.text);
        ChatInputField.text = "";
    }

    private void RefreshLobby()
    {
        foreach (var lobbyAvatar in m_LobbyAvatars.Values)
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
        var tmp = Instantiate(LobbyAvatarPrefab, LayoutLobby.transform, false).GetComponent<LobbyAvatar>();
        tmp.SteamID = steamId;
        tmp.gameObject.name = new Friend(steamId).Name;
        return tmp;
    }
    
    private void CheckForEveryoneReady()
    {
        if (m_Gamestarted || m_Lobby.MemberCount <= 0 || !NetworkManager.Singleton.IsHost) return;
        
        StartButton.interactable = false;
        foreach (var lobbyAvatar in m_LobbyAvatars.Values)
        {
            if (lobbyAvatar.Ready) continue;
            return;
        }
        StartButton.interactable = true;
    }

    private void OnLobbyMemberDataChanged(Lobby lobby, Friend friend)
    {
        m_LobbyAvatars[friend.Id].Refresh();
        if (m_Lobby.GetMemberData(friend, "Start") == "true")
        {
            ReadyButton.interactable = false;
            LeaveButton.interactable = false;
        }
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

    private void OnChatMessage(Lobby lobby, Friend friend, string message)
    {
        m_ChatManager.SendMessageToChat($"[{friend.Name}] {message}");
    }

}
