using System.Threading.Tasks;
using UnityEngine;
using Unity.Netcode;
using Steamworks;
using Steamworks.Data;
using Netcode.Transports.Facepunch;
using SceneLoading;
using UnityEngine.SceneManagement;
using Utility;

public class GameNetworkManager : PersistentSingletonMonoBehaviour<GameNetworkManager>
{
    public Lobby? CurrentLobby;
    public static string APP_ID { get => m_APP_ID; }
    
    // Random hash to distinguish our lobbies from others with default steam app id (480)
    private const string m_APP_ID = "b4bcc8776e19";
    private PlayerSpawner m_Spawner;
    private FacepunchTransport m_Transport;
    private HostLobbyManager.HostLobbyData m_LobbyData;

    private void Start()
    {
        m_Transport = GetComponent<FacepunchTransport>();
        
        SteamMatchmaking.OnLobbyCreated += OnLobbyCreated;
        SteamMatchmaking.OnLobbyEntered += OnLobbyEntered;
        SteamFriends.OnGameLobbyJoinRequested += OnGameLobbyJoinRequested;

        SceneManager.LoadScene("Menu");
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

    public async Task StartHost(HostLobbyManager.HostLobbyData data, int lobbySize = 10)
    {
        m_LobbyData = data;
        NetworkManager.Singleton.StartHost();
        SceneLoaderWrapper.Instance.AddOnSceneEventCallback();
        CurrentLobby = await SteamMatchmaking.CreateLobbyAsync(lobbySize);
    }
    
    public void JoinLobby(Lobby lobby)
    {
        CurrentLobby = lobby;
        CurrentLobby?.Join();
    }

    private void StartClient(SteamId id)
    {
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnectedCallback;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnectCallback;

        m_Transport.targetSteamId = id;
        
        SceneLoaderWrapper.Instance.AddOnSceneEventCallback();
        if (NetworkManager.Singleton.StartClient())
            Debug.Log($"Client has joined targetId={id}.", this);
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
        Debug.Log($"Client connected clientId={clientId}");
    }

    private void OnClientDisconnectCallback(ulong clientId)
    {
        Debug.Log($"Client disconnected clientId={clientId}");
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
            lobby.SetData("name", m_LobbyData.Name);
            lobby.SetData("AppID", APP_ID);
            lobby.SetJoinable(true);
        }
        Debug.Log("Lobby has been created!");
    }

    private void OnLobbyEntered(Lobby lobby)
    {
        Debug.Log($"Entered in lobby", this);
        SceneManager.LoadScene("Lobby");
        
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
