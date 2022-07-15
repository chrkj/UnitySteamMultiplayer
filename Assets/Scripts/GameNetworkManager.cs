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

    public async Task StartHost(HostLobbyManager.HostLobbyData data, int lobbySize = 10)
    {
        m_LobbyData = data;
        NetworkManager.Singleton.ConnectionApprovalCallback += ApprovalCheck;
        NetworkManager.Singleton.StartHost();
        CurrentLobby = await SteamMatchmaking.CreateLobbyAsync(lobbySize);
        SceneLoaderWrapper.Instance.AddOnSceneEventCallback();
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
        NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnectedCallback;
        NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnectCallback;
    }
    
    private void ApprovalCheck(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
    {
        // The client identifier to be authenticated
        var clientId = request.ClientNetworkId;

        // Additional connection data defined by user code
        var connectionData = request.Payload;

        // The prefab hash value of the NetworkPrefab, if null the default NetworkManager player prefab is used
        response.PlayerPrefabHash = null;

        // Position to spawn the player object (if null it uses default of Vector3.zero)
        response.Position = Vector3.zero;

        // Rotation to spawn the player object (if null it uses the default of Quaternion.identity)
        response.Rotation = Quaternion.identity;

        // Approval check happens for Host too, but obviously we want it to be approved
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            // TODO: currentHitPoints Hardcoded atm
            SessionManager<SessionPlayerData>.Instance.SetupConnectingPlayerSessionData(clientId, SteamClient.SteamId.ToString(),
                new SessionPlayerData(clientId, SteamClient.Name, 100, true));

            // Your approval logic determines the following values
            response.Approved = true;
            response.CreatePlayerObject = true;
        }
        else
        {
            // TODO: currentHitPoints Hardcoded atm
            var payload = System.Text.Encoding.UTF8.GetString(connectionData);
            var connectionPayload = JsonUtility.FromJson<ConnectionPayload>(payload); // https://docs.unity3d.com/2020.2/Documentation/Manual/JSONSerialization.html
            SessionManager<SessionPlayerData>.Instance.SetupConnectingPlayerSessionData(clientId, connectionPayload.steamId,
                new SessionPlayerData(clientId, connectionPayload.playerName, 0, true));

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
