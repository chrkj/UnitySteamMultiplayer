using UnityEngine;
using Unity.Netcode;

public class PlayerSpawner : NetworkBehaviour 
{
    [SerializeField] private GameObject m_PlayerPrefab;

    private void Start()
    {
        print("spawning");
        SpawnPlayerPrefab_ServerRpc(NetworkManager.LocalClientId);
    }

    // Server owns this object but client can request a spawn
    [ServerRpc(RequireOwnership = false)]
    private void SpawnPlayerPrefab_ServerRpc(ulong clientId)
    {
        var go = Instantiate(m_PlayerPrefab);
        var no = go.GetComponent<NetworkObject>();
        no.SpawnAsPlayerObject(clientId, true);
    }
    
}