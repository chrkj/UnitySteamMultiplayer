using System.Threading.Tasks;
using UnityEngine;
using Unity.Netcode;
using Steamworks;
using Steamworks.Data;
using Netcode.Transports.Facepunch;

public class GameNetworkManager : MonoBehaviour
{
    public static GameNetworkManager Instance { get; private set; }

    public Lobby? CurrentLobby { get; private set; }

    private ulong m_ClientId;
    private const string APP_ID = "b4bcc8776e19";
    private PlayerSpawner m_Spawner;
    private FacepunchTransport m_Transport;

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
        m_Spawner = GameObject.Find("PlayerSpawner").GetComponent<PlayerSpawner>();

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
            Debug.Log($"Client has joined targetId={id}.", this);
    }

    public void Disconnect()
    {
        CurrentLobby?.Leave();
        if (NetworkManager.Singleton == null) return;
        NetworkManager.Singleton.Shutdown();
    }
    
    public async Task<Lobby[]> RequestLobbyList()
    {
        return await SteamMatchmaking.LobbyList.WithKeyValue("AppID", APP_ID).RequestAsync();
    }

    public async void Join()
    {
        Lobby[] lobbies = await RequestLobbyList();
        Debug.Log(lobbies[0].GetData("name"));
        var lobby = lobbies[0];
        CurrentLobby = lobby;
        CurrentLobby?.Join();
    }

    public void SpawnPlayer()
    {
        m_Spawner.SpawnPlayerPrefab_ServerRpc(m_ClientId);
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
        m_ClientId = clientId;
        Debug.Log($"Client connected clientId={clientId}");
    }

    private void OnClientDisconnectCallback(ulong clientId)
    {
        Debug.Log($"Client disconnected clientId={clientId}");
        m_ClientId = 0;
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
        lobby.SetData("name", "Test name");
        lobby.SetData("AppID", APP_ID);
        lobby.SetJoinable(true);

        Debug.Log("Lobby has been created!");
    }

    private void OnLobbyEntered(Lobby lobby)
    {
        Debug.Log($"Entered in lobby", this);
        if (NetworkManager.Singleton.IsHost)
        {
            Debug.Log($"You are the host!", this);
            return;
        }
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
        CurrentLobby = lobby;
        CurrentLobby?.Join();
    }

    #endregion
}
