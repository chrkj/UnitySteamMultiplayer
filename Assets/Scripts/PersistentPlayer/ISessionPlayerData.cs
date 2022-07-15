using System.Collections.Generic;
using UnityEngine;

public interface ISessionPlayerData
{
    bool IsConnected { get; set; }
    ulong ClientID { get; set; }
    void Reinitialize();
}

public class SessionManager<T> where T : struct, ISessionPlayerData
{
    SessionManager()
    {
        m_ClientData = new Dictionary<ulong, T>();
        m_ClientIDToSteamId = new Dictionary<ulong, ulong>();
    }

    public static SessionManager<T> Instance => s_Instance ??= new SessionManager<T>();

    static SessionManager<T> s_Instance;

    /// <summary>
    /// Maps a given client steam id to the data for a given client player.
    /// </summary>
    Dictionary<ulong, T> m_ClientData;

    /// <summary>
    /// Map to allow us to cheaply map from clientId to steamId.
    /// </summary>
    Dictionary<ulong, ulong> m_ClientIDToSteamId;

    bool m_HasSessionStarted;

    /// <summary>
    /// Handles client disconnect."
    /// </summary>
    public void DisconnectClient(ulong clientId)
    {
        if (m_HasSessionStarted)
        {
            // Mark client as disconnected, but keep their data so they can reconnect.
            if (m_ClientIDToSteamId.TryGetValue(clientId, out var steamId))
            {
                if (GetPlayerDataFromSteamId(steamId)?.ClientID == clientId)
                {
                    var clientData = m_ClientData[steamId];
                    clientData.IsConnected = false;
                    m_ClientData[steamId] = clientData;
                }
            }
        }
        else
        {
            // Session has not started, no need to keep their data
            if (m_ClientIDToSteamId.TryGetValue(clientId, out var steamId))
            {
                m_ClientIDToSteamId.Remove(clientId);
                if (GetPlayerDataFromSteamId(steamId)?.ClientID == clientId)
                    m_ClientData.Remove(steamId);
            }
        }
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="steamId">This is the steamId that is unique to this client and persists across multiple logins from the same client</param>
    /// <returns>True if a player with this ID is already connected.</returns>
    public bool IsDuplicateConnection(ulong steamId)
    {
        return m_ClientData.ContainsKey(steamId) && m_ClientData[steamId].IsConnected;
    }

    /// <summary>
    /// Adds a connecting player's session data if it is a new connection, or updates their session data in case of a reconnection.
    /// </summary>
    /// <param name="clientId">This is the clientId that Netcode assigned us on login. It does not persist across multiple logins from the same client. </param>
    /// <param name="steamId">This is the steamId that is unique to this client and persists across multiple logins from the same client</param>
    /// <param name="sessionPlayerData">The player's initial data</param>
    public void SetupConnectingPlayerSessionData(ulong clientId, ulong steamId, T sessionPlayerData)
    {
        var isReconnecting = false;

        // Test for duplicate connection
        if (IsDuplicateConnection(steamId))
        {
            Debug.LogError(
                $"Steam ID {steamId} already exists. This is a duplicate connection. Rejecting this session data.");
            return;
        }

        // If another client exists with the same steamId
        if (m_ClientData.ContainsKey(steamId))
        {
            if (!m_ClientData[steamId].IsConnected)
            {
                // If this connecting client has the same steam Id as a disconnected client, this is a reconnection.
                isReconnecting = true;
            }
        }

        // Reconnecting. Give data from old player to new player
        if (isReconnecting)
        {
            // Update player session data
            sessionPlayerData = m_ClientData[steamId];
            sessionPlayerData.ClientID = clientId;
            sessionPlayerData.IsConnected = true;
        }

        //Populate our dictionaries with the SessionPlayerData
        m_ClientIDToSteamId[clientId] = steamId;
        m_ClientData[steamId] = sessionPlayerData;
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="clientId"> id of the client whose data is requested</param>
    /// <returns>The Steam ID matching the given client ID (0 if none matches)</returns>
    public ulong GetSteamId(ulong clientId)
    {
        if (m_ClientIDToSteamId.TryGetValue(clientId, out ulong steamId))
            return steamId;

        Debug.Log($"No client steam ID found mapped to the given client ID: {clientId}");
        return 0;
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="clientId"> id of the client whose data is requested</param>
    /// <returns>Player data struct matching the given ID</returns>
    public T? GetPlayerDataFromClientId(ulong clientId)
    {
        //First see if we have a steamId matching the clientID given.
        var steamId = GetSteamId(clientId);
        if (steamId != 0)
            return GetPlayerDataFromSteamId(steamId);

        Debug.Log($"No client steam ID found mapped to the given client ID: {clientId}");
        return null;
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="steamId"> Steam ID of the client whose data is requested</param>
    /// <returns>Player data struct matching the given ID</returns>
    public T? GetPlayerDataFromSteamId(ulong steamId)
    {
        if (m_ClientData.TryGetValue(steamId, out T data))
            return data;

        Debug.Log($"No PlayerData of matching steam ID found: {steamId}");
        return null;
    }

    /// <summary>
    /// Updates player data
    /// </summary>
    /// <param name="clientId"> id of the client whose data will be updated </param>
    /// <param name="sessionPlayerData"> new data to overwrite the old </param>
    public void SetPlayerData(ulong clientId, T sessionPlayerData)
    {
        if (m_ClientIDToSteamId.TryGetValue(clientId, out ulong steamId))
            m_ClientData[steamId] = sessionPlayerData;
        else
            Debug.LogError($"No client steam ID found mapped to the given client ID: {clientId}");
    }

    /// <summary>
    /// Marks the current session as started, so from now on we keep the data of disconnected players.
    /// </summary>
    public void OnSessionStarted()
    {
        m_HasSessionStarted = true;
    }

    /// <summary>
    /// Reinitializes session data from connected players, and clears data from disconnected players, so that if they reconnect in the next game, they will be treated as new players
    /// </summary>
    public void OnSessionEnded()
    {
        ClearDisconnectedPlayersData();
        ReinitializePlayersData();
        m_HasSessionStarted = false;
    }

    /// <summary>
    /// Resets all our runtime state, so it is ready to be reinitialized when starting a new server
    /// </summary>
    public void OnServerEnded()
    {
        m_ClientData.Clear();
        m_ClientIDToSteamId.Clear();
        m_HasSessionStarted = false;
    }

    void ReinitializePlayersData()
    {
        foreach (var id in m_ClientIDToSteamId.Keys)
        {
            ulong steamId = m_ClientIDToSteamId[id];
            T sessionPlayerData = m_ClientData[steamId];
            sessionPlayerData.Reinitialize();
            m_ClientData[steamId] = sessionPlayerData;
        }
    }

    void ClearDisconnectedPlayersData()
    {
        List<ulong> idsToClear = new List<ulong>();
        foreach (var id in m_ClientIDToSteamId.Keys)
        {
            var data = GetPlayerDataFromClientId(id);
            if (data is { IsConnected: false })
                idsToClear.Add(id);
        }

        foreach (var id in idsToClear)
        {
            ulong steamId = m_ClientIDToSteamId[id];
            if (GetPlayerDataFromClientId(steamId)?.ClientID == id)
                m_ClientData.Remove(steamId);
            m_ClientIDToSteamId.Remove(id);
        }
    }
}