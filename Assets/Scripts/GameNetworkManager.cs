using System.Threading.Tasks;
using UnityEngine;
using Unity.Netcode;
using Steamworks;
using Steamworks.Data;
using Netcode.Transports.Facepunch;
using UnityEngine.SceneManagement;

public class GameNetworkManager : MonoBehaviour
{
    public static GameNetworkManager Instance { get; private set; }
    
    // Random hash to distinguish our lobbies from others with default steam app id (480)
    public const string APP_ID = "b4bcc8776e19"; 
    public HostGameManager.HostLobbyData LobbyData;
    public Lobby? CurrentLobby { get; private set; }

    private ulong m_ClientId;
    private PlayerSpawner m_Spawner;
    private FacepunchTransport m_Transport;

    private void Awake() => Instance = this;

    private void Start()
    {
        m_Transport = GetComponent<FacepunchTransport>();

        SteamMatchmaking.OnLobbyCreated += OnLobbyCreated;
        SteamMatchmaking.OnLobbyEntered += OnLobbyEntered;
        SteamFriends.OnGameLobbyJoinRequested += OnGameLobbyJoinRequested;

        SceneManager.LoadScene("MainMenuScene");
    }

    private void OnDestroy()
    {
        SteamMatchmaking.OnLobbyCreated -= OnLobbyCreated;
        SteamMatchmaking.OnLobbyEntered -= OnLobbyEntered;
        SteamFriends.OnGameLobbyJoinRequested -= OnGameLobbyJoinRequested;
        
        if (NetworkManager.Singleton == null) return;
        
        NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnectedCallback;
        NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnectCallback;
    }

    public async Task<Lobby?> StartHost(HostGameManager.HostLobbyData data, int lobbySize = 10)
    {
        LobbyData = data;
        NetworkManager.Singleton.StartHost();
        CurrentLobby = await SteamMatchmaking.CreateLobbyAsync(lobbySize);
        return CurrentLobby;
    }

    public void StartClient(SteamId id)
    {
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnectedCallback;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnectCallback;

        m_Transport.targetSteamId = id;
        
        if (NetworkManager.Singleton.StartClient())
            Debug.Log($"Client has joined targetId={id}.", this);
    }

    public async Task<Lobby[]> RequestLobbyList()
    {
        return await SteamMatchmaking.LobbyList.WithKeyValue("AppID", APP_ID).RequestAsync();
    }

    public void JoinLobby(Lobby lobby)
    {
        CurrentLobby = lobby;
        CurrentLobby?.Join();
    }

    private void OnApplicationQuit()
    {
        CurrentLobby?.Leave();
        if (NetworkManager.Singleton == null) return;
        NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnectedCallback;
        NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnectCallback;
        NetworkManager.Singleton.Shutdown();
    }

    #region Network Callbacks
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
    private void OnLobbyCreated(Result result, Lobby lobby)
    {
        if (result != Result.OK)
        {
            Debug.LogError($"Lobby could not be created!, {result}", this);
            return;
        }

        {   // Set lobby data here
            lobby.SetPublic();
            lobby.SetData("name", LobbyData.Name);
            lobby.SetData("AppID", APP_ID);
            lobby.SetJoinable(true);
        }
        Debug.Log("Lobby has been created!");
    }

    private void OnLobbyEntered(Lobby lobby)
    {
        Debug.Log($"Entered in lobby", this);
        SceneManager.LoadScene("LobbyScene");
        SceneLoaderWrapper.Instance.AddOnSceneEventCallback();
        if (NetworkManager.Singleton.IsHost)
        {
            Debug.Log($"You are the host!", this);
            return;
        }
        StartClient(lobby.Owner.Id);
    }

    private void OnGameLobbyJoinRequested(Lobby lobby, SteamId id)
    {
        CurrentLobby = lobby;
        CurrentLobby?.Join();
    }
    #endregion
}
