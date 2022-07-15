using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
public class PersistentPlayer : NetworkBehaviour
{
    public NetworkNameState NetworkNameState => m_NetworkNameState;

    [SerializeField] private NetworkNameState m_NetworkNameState;
    [SerializeField] private PersistentPlayerRuntimeCollection m_PersistentPlayerRuntimeCollection;

    public override void OnNetworkSpawn()
    {
        gameObject.name = "PersistentPlayer" + OwnerClientId;
        m_PersistentPlayerRuntimeCollection.Add(this);
        if (IsServer)
        {
            var sessionPlayerData = SessionManager<SessionPlayerData>.Instance.GetPlayerDataFromClientId(OwnerClientId);
            if (sessionPlayerData.HasValue)
                m_NetworkNameState.Name.Value = sessionPlayerData.Value.PlayerName;
        }
        DontDestroyOnLoad(gameObject);
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        RemovePersistentPlayer();
    }

    public override void OnNetworkDespawn()
    {
        RemovePersistentPlayer();
    }

    void RemovePersistentPlayer()
    {
        m_PersistentPlayerRuntimeCollection.Remove(this);
        
        if (!IsServer) return;
        var sessionPlayerData = SessionManager<SessionPlayerData>.Instance.GetPlayerDataFromClientId(OwnerClientId);
        
        if (!sessionPlayerData.HasValue) return;
        var playerData = sessionPlayerData.Value;
        playerData.PlayerName = m_NetworkNameState.Name.Value;
        SessionManager<SessionPlayerData>.Instance.SetPlayerData(OwnerClientId, playerData);
    }
}