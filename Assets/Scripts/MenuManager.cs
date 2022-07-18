using UnityEngine;

public class MenuManager : MonoBehaviour
{
    public GameObject MenuCanvas;
    public GameObject JoinLobbyCanvas;
    public GameObject HostLobbyCanvas;
    public GameObject HostDisconnectedPopup;

    private void Start()
    {
        if (GameNetworkManager.Instance.HostDisconnected)
            HostDisconnectedPopup.SetActive(true);
    }

    public void OnHostSelected()
    {
        MenuCanvas.SetActive(false);
        HostLobbyCanvas.SetActive(true);
    }
    
    public void GoToJoinLobby()
    {
        MenuCanvas.SetActive(false);
        JoinLobbyCanvas.SetActive(true);
        JoinLobbyManager.Instance.RefreshLobbies();
    }
    
    public void GoToMainMenu()
    {
        MenuCanvas.SetActive(true);
        JoinLobbyCanvas.SetActive(false);
        HostLobbyCanvas.SetActive(false);
    }

    public void HostDisconnectedButton()
    {
        GameNetworkManager.Instance.HostDisconnected = false;
        HostDisconnectedPopup.SetActive(false);
    }

}
