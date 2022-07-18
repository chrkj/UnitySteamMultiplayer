using UnityEngine;

public interface ISessionPlayerData
{
    public ulong ClientID { get; set; }
    public bool IsConnected { get; set; }
    public void Reinitialize();
}

public struct SessionPlayerData : ISessionPlayerData
{
    public string PlayerName;
    public bool HasCharacterSpawned;

    public SessionPlayerData(ulong clientID, string name, bool isConnected, bool hasCharacterSpawned = false)
    {
        ClientID = clientID;
        PlayerName = name;
        IsConnected = isConnected;
        HasCharacterSpawned = hasCharacterSpawned;
    }

    public bool IsConnected { get; set; }
    public ulong ClientID { get; set; }
    
    public void Reinitialize()
    {
        HasCharacterSpawned = false;
    }
    
}