using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoaderWrapper : NetworkBehaviour
{
    public static SceneLoaderWrapper Instance { get; private set; }
    
    [SerializeField] private LoadingProgressManager m_LoadingProgressManager;
    [SerializeField] private ClientLoadingScreen m_ClientLoadingScreen;
    private bool IsNetworkSceneManagementEnabled => NetworkManager != null && NetworkManager.SceneManager != null && NetworkManager.NetworkConfig.EnableSceneManagement;
    
    private void Awake()
    {
        if (Instance != null)
            Destroy(gameObject);
        else
            Instance = this;

        DontDestroyOnLoad(this);
    }

    private void Start()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    
    public override void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        base.OnDestroy();
    }

    public override void OnNetworkDespawn()
    {
        if (NetworkManager != null && NetworkManager.SceneManager != null)
            NetworkManager.SceneManager.OnSceneEvent -= OnSceneEvent;
    }

    /// <summary>
    /// Initializes the callback on scene events. This needs to be called right after initializing NetworkManager
    /// (after StartHost, StartClient or StartServer)
    /// </summary>
    public void AddOnSceneEventCallback()
    {
        if (IsNetworkSceneManagementEnabled)
            NetworkManager.SceneManager.OnSceneEvent += OnSceneEvent;
    }

    /// <summary>
    /// Loads a scene asynchronously using the specified loadSceneMode, with NetworkSceneManager if on a listening
    /// server with SceneManagement enabled, or SceneManager otherwise. If a scene is loaded via SceneManager, this
    /// method also triggers the start of the loading screen.
    /// </summary>
    /// <param name="sceneName">Name or path of the Scene to load.</param>
    /// <param name="useNetworkSceneManager">If true, uses NetworkSceneManager, else uses SceneManager</param>
    /// <param name="loadSceneMode">If LoadSceneMode.Single then all current Scenes will be unloaded before loading.</param>
    public void LoadScene(string sceneName, bool useNetworkSceneManager, LoadSceneMode loadSceneMode = LoadSceneMode.Single)
    {
        if (useNetworkSceneManager)
        {
            if (!IsNetworkSceneManagementEnabled || NetworkManager.ShutdownInProgress || !NetworkManager.IsServer) return;
            NetworkManager.SceneManager.LoadScene(sceneName, loadSceneMode);
        }
        else
        {
            var loadOperation = SceneManager.LoadSceneAsync(sceneName, loadSceneMode);
            if (loadSceneMode != LoadSceneMode.Single) return;
            m_ClientLoadingScreen.StartLoadingScreen(sceneName);
            m_LoadingProgressManager.LocalLoadOperation = loadOperation;
        }
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
    {
        if (NetworkManager.ShutdownInProgress)
            m_ClientLoadingScreen.StopLoadingScreen();
    }

    private void OnSceneEvent(SceneEvent sceneEvent)
    {
        switch (sceneEvent.SceneEventType)
        {
            // Server told client to load a scene
            case SceneEventType.Load:
            {
                // Only executes on client
                if (NetworkManager.IsClient)
                {
                    // Only start a new loading screen if scene loaded in Single mode, else simply update
                    if (sceneEvent.LoadSceneMode == LoadSceneMode.Single)
                    {
                        m_ClientLoadingScreen.StartLoadingScreen(sceneEvent.SceneName);
                        m_LoadingProgressManager.LocalLoadOperation = sceneEvent.AsyncOperation;
                    }
                    else
                    {
                        m_ClientLoadingScreen.UpdateLoadingScreen(sceneEvent.SceneName);
                        m_LoadingProgressManager.LocalLoadOperation = sceneEvent.AsyncOperation;
                    }
                }
                break;
            }
            
            // Server told client that all clients finished loading a scene
            case SceneEventType.LoadEventCompleted:
            {
                // Only executes on client
                if (NetworkManager.IsClient)
                {
                    m_ClientLoadingScreen.StopLoadingScreen();
                    m_LoadingProgressManager.ResetLocalProgress();
                }

                break;
            }
            
            // Server told client to start synchronizing scenes
            case SceneEventType.Synchronize:
            {
                // Only executes on client that is not the host
                if (NetworkManager.IsClient && !NetworkManager.IsHost)
                {
                    // unload all currently loaded additive scenes so that if we connect to a server with the same
                    // main scene we properly load and synchronize all appropriate scenes without loading a scene
                    // that is already loaded.
                    UnloadAdditiveScenes();
                }
                break;
            }
            
            // Client told server that they finished synchronizing
            case SceneEventType.SynchronizeComplete:
            {
                // Only executes on server
                if (NetworkManager.IsServer)
                {
                    // Send client RPC to make sure the client stops the loading screen after the server handles what it needs to after
                    // the client finished synchronizing, for example character spawning done server side should still be hidden by loading screen.
                    StopLoadingScreenClientRpc(new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = new[] { sceneEvent.ClientId } } });
                }
                break;
            }
            
            case SceneEventType.Unload:
                break;
            
            case SceneEventType.ReSynchronize:
                break;
            
            case SceneEventType.UnloadEventCompleted:
                break;
            
            case SceneEventType.LoadComplete:
                break;
            
            case SceneEventType.UnloadComplete:
                break;
                
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void UnloadAdditiveScenes()
    {
        var activeScene = SceneManager.GetActiveScene();
        for (var i = 0; i < SceneManager.sceneCount; i++)
        {
            var scene = SceneManager.GetSceneAt(i);
            if (scene.isLoaded && scene != activeScene)
                SceneManager.UnloadSceneAsync(scene);
        }
    }

    [ClientRpc]
    private void StopLoadingScreenClientRpc(ClientRpcParams clientRpcParams = default)
    {
        m_ClientLoadingScreen.StopLoadingScreen();
    }
}