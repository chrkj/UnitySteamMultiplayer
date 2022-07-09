using UnityEngine;
using Unity.Netcode;

public class PlayerSpawner : NetworkBehaviour 
{
    [SerializeField] private GameObject m_PlayerPrefab;
 
    // Server owns this object but client can request a spawn
    [ServerRpc(RequireOwnership = false)]
    public void SpawnPlayerPrefab_ServerRpc(ulong clientId)
    {
        var go = Instantiate(m_PlayerPrefab);
        var no = go.GetComponent<NetworkObject>();
        no.SpawnAsPlayerObject(clientId, true);
    }
}