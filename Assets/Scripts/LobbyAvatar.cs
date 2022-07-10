using Steamworks;
using UnityEngine;
using UnityEngine.UI;

public class LobbyAvatar : Avatar
{
    public bool Ready;
    public Image ImageReadyOutline;
    
    public void Refresh ()
    {
        if (bool.TryParse(GameNetworkManager.Instance.CurrentLobby.Value.GetMemberData(new Friend(SteamID), "Ready"), out Ready))
        {
            ImageReadyOutline.color = Ready ? Color.green : Color.red;
        }
    }
}
