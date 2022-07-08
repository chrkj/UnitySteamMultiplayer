using System.Threading.Tasks;
using UnityEngine;
using Unity.Netcode;
using Steamworks;
using Steamworks.Data;
using Netcode.Transports.Facepunch;

public class GameNetworkManager : MonoBehaviour
{
    public static GameNetworkManager Instance { get; private set; } = null;

    public Lobby? CurrentLobby { get; private set; } = null;
    
    private Task<Lobby[]> m_LobbyList;
    private FacepunchTransport m_Transport = null;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            Destroy(this);
            return;
        }
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        m_Transport = GetComponent<FacepunchTransport>();

        SteamMatchmaking.OnLobbyInvite += OnLobbyInvite;
        SteamMatchmaking.OnLobbyCreated += OnLobbyCreated;
        SteamMatchmaking.OnLobbyEntered += OnLobbyEntered;
        SteamMatchmaking.OnLobbyGameCreated += OnLobbyGameCreated;
        SteamMatchmaking.OnLobbyMemberJoined += OnLobbyMemberJoined;
        SteamFriends.OnGameLobbyJoinRequested += OnGameLobbyJoinRequested;
    }

    private void OnDestroy()
    {
        SteamMatchmaking.OnLobbyInvite -= OnLobbyInvite;
        SteamMatchmaking.OnLobbyCreated -= OnLobbyCreated;
        SteamMatchmaking.OnLobbyEntered -= OnLobbyEntered;
        SteamMatchmaking.OnLobbyGameCreated -= OnLobbyGameCreated;
        SteamMatchmaking.OnLobbyMemberJoined -= OnLobbyMemberJoined;
        SteamFriends.OnGameLobbyJoinRequested -= OnGameLobbyJoinRequested;
        
        if (NetworkManager.Singleton == null) return;
        
        NetworkManager.Singleton.OnServerStarted -= OnServerStarted;
        NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnectedCallback;
        NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnectCallback;
    }

    public async void StartHost(int lobbySize = 100)
    {
        NetworkManager.Singleton.OnServerStarted += OnServerStarted;
        NetworkManager.Singleton.StartHost();

        CurrentLobby = await SteamMatchmaking.CreateLobbyAsync(lobbySize);
    }

    public void StartClient(SteamId id)
    {
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnectedCallback;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnectCallback;

        m_Transport.targetSteamId = id;
        
        if (NetworkManager.Singleton.StartClient())
            Debug.Log("Client has joined", this);
    }

    public void Disconnect()
    {
        CurrentLobby?.Leave();
        if (NetworkManager.Singleton == null) return;
        
        NetworkManager.Singleton.Shutdown();
    }
    
    public void RequestList()
    {
        m_LobbyList = SteamMatchmaking.LobbyList.WithKeyValue("name", "Temp Lobby name").RequestAsync();
    }

    public void Join()
    {
        var lobby = m_LobbyList.Result[0];
        CurrentLobby = lobby;
        CurrentLobby?.Join();
    }
    
    private void OnApplicationQuit()
    {
        Disconnect();
    }

    #region Network Callbacks

    private void OnServerStarted()
    {
        Debug.Log("Server has been started!", this);
    }

    private void OnClientConnectedCallback(ulong clientId)
    {
        Debug.Log($"Client connected: clientId={clientId}");
    }

    private void OnClientDisconnectCallback(ulong clientId)
    {
        Debug.Log($"Client disconnected: clientId={clientId}");
        NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnectedCallback;
        NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnectCallback;
    }

    #endregion

    #region Steam Callbacks

    private void OnLobbyInvite(Friend friend, Lobby lobby)
    {
        Debug.Log($"You got an invite from {friend.Name}", this);
    }

    private void OnLobbyCreated(Result result, Lobby lobby)
    {
        if (result != Result.OK)
        {
            Debug.LogError($"Lobby couldn't be created!, {result}", this);
            return;
        }

        lobby.SetPublic(); 
        lobby.SetData("name", "Temp lobby name");
        lobby.SetJoinable(true);

        Debug.Log("Lobby has been created!");
    }

    private void OnLobbyEntered(Lobby lobby)
    {
        Debug.Log($"You have entered in lobby, clientId={NetworkManager.Singleton.LocalClientId}", this);

        if (NetworkManager.Singleton.IsHost)
            return;

        StartClient(lobby.Owner.Id);
    }

    private void OnLobbyGameCreated(Lobby lobby, uint ip, ushort port, SteamId id)
    {
    }

    private void OnLobbyMemberJoined(Lobby lobby, Friend friend)
    {
    }

    private void OnGameLobbyJoinRequested(Lobby lobby, SteamId id)
    {
        StartClient(id);
    }

    #endregion
}
