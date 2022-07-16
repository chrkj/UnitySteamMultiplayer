using System;
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
    public bool HostDisconnected;
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

        SceneManager.LoadScene("MainMenu");
    }

    private void OnDestroy()
    {
        SteamMatchmaking.OnLobbyCreated -= OnLobbyCreated;
        SteamMatchmaking.OnLobbyEntered -= OnLobbyEntered;
        SteamFriends.OnGameLobbyJoinRequested -= OnGameLobbyJoinRequested;
        
        if (NetworkManager.Singleton == null) return;
        
        NetworkManager.Singleton.ConnectionApprovalCallback -= ApprovalCheck; // Should unsubscribe when lobby is destroyed?
        NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnectedCallback;
        NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnectCallback;
    }

    public void UnsubscribeConnectionApprovalCallback()
    {
        NetworkManager.Singleton.ConnectionApprovalCallback -= ApprovalCheck;
    }

    public async Task StartHost(HostLobbyManager.HostLobbyData data, int lobbySize = 10)
    {
        NetworkManager.Singleton.ConnectionApprovalCallback += ApprovalCheck;

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
        m_Transport.targetSteamId = id;

        ApproveClient();
        if (NetworkManager.Singleton.StartClient())
            Debug.Log($"Client has joined targetId={id}.", this);
        SceneLoaderWrapper.Instance.AddOnSceneEventCallback();
    }

    private void ApproveClient()
    {
        // TODO: Future password input should be included in the payload
        var payload = JsonUtility.ToJson(new ConnectionPayload()
        {
            steamId = SteamClient.SteamId.ToString(),
            playerName = SteamClient.Name,
        });

        var payloadBytes = System.Text.Encoding.UTF8.GetBytes(payload);
        NetworkManager.Singleton.NetworkConfig.ConnectionData = payloadBytes;
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
        SessionManager<SessionPlayerData>.Instance.DisconnectClient(clientId);

        if (clientId == 0)
        {
            Debug.Log("Host left");
            HostDisconnected = true;
            CurrentLobby.Value.Leave();
            SceneManager.LoadScene("MainMenu");
        }
    }

    public void UnsubscribeConnectionCallbacks()
    {
        NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnectedCallback;
        NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnectCallback;
    }
    
    private void ApprovalCheck(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
    {
        var clientId = request.ClientNetworkId;
        var connectionData = request.Payload;
        
        // Approval check happens for Host too, but obviously we want it to be approved
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            SessionManager<SessionPlayerData>.Instance.SetupConnectingPlayerSessionData(clientId, SteamClient.SteamId, 
                new SessionPlayerData(clientId, SteamClient.Name, true));

            // Your approval logic determines the following values
            response.Approved = true;
            response.CreatePlayerObject = true;
        }
        else
        {
            var payload = System.Text.Encoding.UTF8.GetString(connectionData);
            var connectionPayload = JsonUtility.FromJson<ConnectionPayload>(payload);
            SessionManager<SessionPlayerData>.Instance.SetupConnectingPlayerSessionData(clientId, ulong.Parse(connectionPayload.steamId), 
                new SessionPlayerData(clientId, connectionPayload.playerName, true));

            response.Approved = true;
            response.CreatePlayerObject = true;
            response.Position = Vector3.zero;
            response.Rotation = Quaternion.identity;
        }
        
        // If additional approval steps are needed, set this to true until the additional steps are complete
        // once it transitions from true to false the connection approval response will be processed.
        response.Pending = false;
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
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnectedCallback;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnectCallback;

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

[Serializable]
public class ConnectionPayload
{
    public string steamId;
    public string playerName;
}
