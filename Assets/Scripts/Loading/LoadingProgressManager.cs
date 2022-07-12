using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class LoadingProgressManager : NetworkBehaviour
{
    public AsyncOperation LocalLoadOperation
    {
        set
        {
            LocalProgress = 0;
            m_LocalLoadOperation = value;
        }
    } 
    
    public float LocalProgress
    {
        get => IsSpawned && ProgressTrackers.ContainsKey(NetworkManager.LocalClientId)
            ? ProgressTrackers[NetworkManager.LocalClientId].Progress.Value
            : m_LocalProgress;
        private set
        {
            if (IsSpawned && ProgressTrackers.ContainsKey(NetworkManager.LocalClientId))
                ProgressTrackers[NetworkManager.LocalClientId].Progress.Value = value;
            else
                m_LocalProgress = value;
        }
    }

    public event Action onTrackersUpdated;
    public Dictionary<ulong, NetworkTrackerLoadingProgress> ProgressTrackers { get; } = new Dictionary<ulong, NetworkTrackerLoadingProgress>();

    private float m_LocalProgress;
    private AsyncOperation m_LocalLoadOperation;
    [SerializeField] private GameObject m_ProgressTrackerPrefab;
    
    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;
        
        NetworkManager.OnClientConnectedCallback += AddTracker;
        NetworkManager.OnClientDisconnectCallback += RemoveTracker;
        AddTracker(NetworkManager.LocalClientId);
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer)
        {
            NetworkManager.OnClientConnectedCallback -= AddTracker;
            NetworkManager.OnClientDisconnectCallback -= RemoveTracker;
        }
        ProgressTrackers.Clear();
        onTrackersUpdated?.Invoke();
    }
    
    public void ResetLocalProgress()
    {
        LocalProgress = 0;
    }

    private void Update()
    {
        if (m_LocalLoadOperation != null)
            LocalProgress = m_LocalLoadOperation.isDone ? 1 : m_LocalLoadOperation.progress;
    }

    [ClientRpc]
    private void UpdateTrackersClientRpc()
    {
        if (!IsHost)
        {
            ProgressTrackers.Clear();
            foreach (var tracker in FindObjectsOfType<NetworkTrackerLoadingProgress>())
            {
                if (!tracker.IsSpawned) continue;
                ProgressTrackers[tracker.OwnerClientId] = tracker;
                if (tracker.OwnerClientId == NetworkManager.LocalClientId)
                    LocalProgress = Mathf.Max(m_LocalProgress, LocalProgress);
            }
        }
        onTrackersUpdated?.Invoke();
    }

    private void AddTracker(ulong clientId)
    {
        if (!IsServer) return;
        
        var tracker = Instantiate(m_ProgressTrackerPrefab);
        var networkObject = tracker.GetComponent<NetworkObject>();
        networkObject.SpawnWithOwnership(clientId);
        ProgressTrackers[clientId] = tracker.GetComponent<NetworkTrackerLoadingProgress>();
        UpdateTrackersClientRpc();
    }

    private void RemoveTracker(ulong clientId)
    {
        if (!IsServer || !ProgressTrackers.ContainsKey(clientId)) return;
        
        var tracker = ProgressTrackers[clientId];
        ProgressTrackers.Remove(clientId);
        tracker.NetworkObject.Despawn();
        UpdateTrackersClientRpc();
    }

}