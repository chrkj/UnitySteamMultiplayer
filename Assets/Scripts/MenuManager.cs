using UnityEngine;

public class MenuManager : MonoBehaviour
{
    public GameObject MenuCanvas;
    public GameObject JoinLobbyCanvas;
    public GameObject HostLobbyCanvas;

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

}
