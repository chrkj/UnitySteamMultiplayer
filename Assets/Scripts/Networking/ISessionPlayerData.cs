using System.Collections.Generic;
using UnityEngine;

public interface ISessionPlayerData
{
    bool IsConnected { get; set; }
    ulong ClientID { get; set; }
    void Reinitialize();
}

/// <summary>
/// This class uses a unique player ID to bind a player to a session. Once that player connects to a host, the host
/// associates the current ClientID to the player's unique ID. If the player disconnects and reconnects to the same
/// host, the session is preserved.
/// </summary>
/// <remarks>
/// Using a client-generated player ID and sending it directly could be problematic, as a malicious user could
/// intercept it and reuse it to impersonate the original user. We are currently investigating this to offer a
/// solution that handles security better.
/// </remarks>
/// <typeparam name="T"></typeparam>
public class SessionManager<T> where T : struct, ISessionPlayerData
{
    SessionManager()
    {
        m_ClientData = new Dictionary<string, T>();
        m_ClientIDToSteamId = new Dictionary<ulong, string>();
    }

    public static SessionManager<T> Instance => s_Instance ??= new SessionManager<T>();

    static SessionManager<T> s_Instance;

    /// <summary>
    /// Maps a given client player id to the data for a given client player.
    /// </summary>
    Dictionary<string, T> m_ClientData;

    /// <summary>
    /// Map to allow us to cheaply map from player id to player data.
    /// </summary>
    Dictionary<ulong, string> m_ClientIDToSteamId;

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
                if (GetPlayerData(steamId)?.ClientID == clientId)
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
                if (GetPlayerData(steamId)?.ClientID == clientId)
                {
                    m_ClientData.Remove(steamId);
                }
            }
        }
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="steamId">This is the steamId that is unique to this client and persists across multiple logins from the same client</param>
    /// <returns>True if a player with this ID is already connected.</returns>
    public bool IsDuplicateConnection(string steamId)
    {
        return m_ClientData.ContainsKey(steamId) && m_ClientData[steamId].IsConnected;
    }

    /// <summary>
    /// Adds a connecting player's session data if it is a new connection, or updates their session data in case of a reconnection.
    /// </summary>
    /// <param name="clientId">This is the clientId that Netcode assigned us on login. It does not persist across multiple logins from the same client. </param>
    /// <param name="steamId">This is the steamId that is unique to this client and persists across multiple logins from the same client</param>
    /// <param name="sessionPlayerData">The player's initial data</param>
    public void SetupConnectingPlayerSessionData(ulong clientId, string steamId, T sessionPlayerData)
    {
        var isReconnecting = false;

        // Test for duplicate connection
        if (IsDuplicateConnection(steamId))
        {
            Debug.LogError(
                $"Player ID {steamId} already exists. This is a duplicate connection. Rejecting this session data.");
            return;
        }

        // If another client exists with the same steamId
        if (m_ClientData.ContainsKey(steamId))
        {
            if (!m_ClientData[steamId].IsConnected)
            {
                // If this connecting client has the same player Id as a disconnected client, this is a reconnection.
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
    /// <returns>The Player ID matching the given client ID</returns>
    public string GetSteamId(ulong clientId)
    {
        if (m_ClientIDToSteamId.TryGetValue(clientId, out string steamId))
        {
            return steamId;
        }

        Debug.Log($"No client player ID found mapped to the given client ID: {clientId}");
        return null;
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="clientId"> id of the client whose data is requested</param>
    /// <returns>Player data struct matching the given ID</returns>
    public T? GetPlayerData(ulong clientId)
    {
        //First see if we have a steamId matching the clientID given.
        var steamId = GetSteamId(clientId);
        if (steamId != null)
        {
            return GetPlayerData(steamId);
        }

        Debug.Log($"No client player ID found mapped to the given client ID: {clientId}");
        return null;
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="steamId"> Steam ID of the client whose data is requested</param>
    /// <returns>Player data struct matching the given ID</returns>
    public T? GetPlayerData(string steamId)
    {
        if (m_ClientData.TryGetValue(steamId, out T data))
        {
            return data;
        }

        Debug.Log($"No PlayerData of matching player ID found: {steamId}");
        return null;
    }

    /// <summary>
    /// Updates player data
    /// </summary>
    /// <param name="clientId"> id of the client whose data will be updated </param>
    /// <param name="sessionPlayerData"> new data to overwrite the old </param>
    public void SetPlayerData(ulong clientId, T sessionPlayerData)
    {
        if (m_ClientIDToSteamId.TryGetValue(clientId, out string steamId))
        {
            m_ClientData[steamId] = sessionPlayerData;
        }
        else
        {
            Debug.LogError($"No client player ID found mapped to the given client ID: {clientId}");
        }
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
            string steamId = m_ClientIDToSteamId[id];
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
            var data = GetPlayerData(id);
            if (data is { IsConnected: false })
            {
                idsToClear.Add(id);
            }
        }

        foreach (var id in idsToClear)
        {
            string steamId = m_ClientIDToSteamId[id];
            if (GetPlayerData(steamId)?.ClientID == id)
            {
                m_ClientData.Remove(steamId);
            }

            m_ClientIDToSteamId.Remove(id);
        }
    }
}