using System.Collections.Generic;
using System.Linq;
using Steamworks;
using Steamworks.Data;
using UnityEngine;
using Color = UnityEngine.Color;

public class LobbyManager : MonoBehaviour
{
    public Transform LayoutLobby;
    public GameObject LobbyAvatarPrefab;
    public UnityEngine.UI.Text LobbyName;
    public UnityEngine.UI.Button ReadyButton;
    
    private bool m_Ready = false;
    //private bool m_GameStarted = false;
    
    private Dictionary<ulong, LobbyAvatar> m_LobbyAvatars = new Dictionary<ulong, LobbyAvatar>();

    private void Start()
    {
        SteamMatchmaking.OnLobbyMemberDataChanged += OnLobbyMemberDataChanged;
        
        InitializeLobby();
    }

    private void OnDestroy()
    {
        SteamMatchmaking.OnLobbyMemberDataChanged -= OnLobbyMemberDataChanged;
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

    public void Ready()
    {
        Debug.Log("click");
        m_Ready = !m_Ready;
        GameNetworkManager.Instance.CurrentLobby.Value.SetData("Ready", m_Ready.ToString());
        ReadyButton.GetComponent<UnityEngine.UI.Image>().color = m_Ready ? new Color(0, 0.25f, 0) : new Color(0.25f, 0, 0);
        ReadyButton.GetComponentInChildren<UnityEngine.UI.Text>().text = m_Ready ? "Ready" : "Click to Ready up";
    }
    
    private void OnLobbyMemberDataChanged(Lobby lobby, Friend friend)
    {
        //m_LobbyAvatars[friend.Id].Refresh();
        Debug.Log($"Data changed for steamId={friend.Id}");
        //CheckForEveryoneReady();
    }
  
}
