using System.Collections.Generic;
using System.Linq;
using Steamworks;
using UnityEditor.PackageManager;
using UnityEngine;

public class LobbyManager : MonoBehaviour
{
    
    public Transform LayoutLobby;
    public GameObject LobbyAvatarPrefab;
    public UnityEngine.UI.Text LobbyName;
    //public UnityEngine.UI.Button ReadyButton;
    //
    private bool m_Ready = false;
    //private bool m_GameStarted = false;
    
    private Dictionary<ulong, LobbyAvatar> m_LobbyAvatars = new Dictionary<ulong, LobbyAvatar>();

    private void Start()
    {
        InitializeLobby();
    }
    
    private void InitializeLobby()
    {
        // Destroy the old lobby
        foreach (LobbyAvatar lobbyAvatar in m_LobbyAvatars.Values)
        {
            Destroy(lobbyAvatar.gameObject);
        }

        m_LobbyAvatars.Clear();

        var lobbyMembers = GameNetworkManager.Instance.CurrentLobby.Value.Members.ToArray();

        // Spawn all members of the new lobby
        foreach (var lobbyMember in lobbyMembers)
        {
            m_LobbyAvatars[lobbyMember.Id] = InstantiateLobbyAvatar(lobbyMember.Id);
            m_LobbyAvatars[lobbyMember.Id].Refresh();
        }

        LobbyName.text = GameNetworkManager.Instance.LobbyData.Name;
        GameNetworkManager.Instance.CurrentLobby.Value.SetData("Ready", m_Ready.ToString());
    }
    
    private LobbyAvatar InstantiateLobbyAvatar(ulong steamId)
    {
        LobbyAvatar tmp = Instantiate(LobbyAvatarPrefab, LayoutLobby, false).GetComponent<LobbyAvatar>();
        tmp.gameObject.name = new Friend(steamId).Name;
        tmp.SteamID = steamId;
        return tmp;
    }
  
}
