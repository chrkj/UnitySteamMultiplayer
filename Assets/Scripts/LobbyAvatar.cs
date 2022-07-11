using Steamworks;
using UnityEngine;
using UnityEngine.UI;

public class LobbyAvatar : Avatar
{
    public bool Ready;
    public Text ReadyText;
    public Image ImageReadyOutline;
    
    public void Refresh()
    {
        if (bool.TryParse(GameNetworkManager.Instance.CurrentLobby.Value.GetMemberData(new Friend(SteamID), "Ready"), out Ready))
        {
            if (Ready)
                ReadyText.text = "Ready";
            else
                ReadyText.text = "Not Ready";

            var color = Ready ? Color.green : Color.red;
            ReadyText.color = color;
            ImageReadyOutline.color = color;
        }
    }
}
